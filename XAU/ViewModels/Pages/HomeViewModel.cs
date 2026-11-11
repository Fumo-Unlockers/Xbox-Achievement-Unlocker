using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using XAU.Util.Etw;
using XAU.Util.Logging;
using XAU.Util.Diagnostics;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Memory;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Collections.ObjectModel;
using System.IO.Compression;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using XboxAuthNet.OAuth;
using XboxAuthNet.OAuth.CodeFlow;
using XboxAuthNet.XboxLive;
using XboxAuthNet.XboxLive.Requests;
using XboxAuthNet.XboxLive.Responses;

namespace XAU.ViewModels.Pages
{
    public partial class ImageItem : ObservableObject
    {
        [ObservableProperty]
        private string _imageUrl;
    }

    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        public static string ToolVersion = "26.11.11";
        public static string EventsVersion = "1.0";

        //attach vars
        [ObservableProperty] private string _attached = "Not Attached";
        [ObservableProperty] private Brush _attachedColor = new SolidColorBrush(Colors.Red);
        [ObservableProperty] private string _loggedIn = "Not Logged In";
        [ObservableProperty] private Brush _loggedInColor = new SolidColorBrush(Colors.Red);

        //profile vars
        [ObservableProperty] private string? _gamerPic = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string? _gamerTag = "Gamertag: Unknown   ";
        [ObservableProperty] private string? _xuid = "XUID: Unknown";
        [ObservableProperty] private string? _gamerScore = "Gamerscore: Unknown";
        [ObservableProperty] private string? _profileRep = "Reputation: Unknown";
        [ObservableProperty] private string? _accountTier = "Tier: Unknown";
        [ObservableProperty] private string? _currentlyPlaying = "Currently Playing: Unknown";
        [ObservableProperty] private string? _activeDevice = "Active Device: Unknown";
        [ObservableProperty] private string? _isVerified = "Verified: Unknown";
        [ObservableProperty] private string? _location = "Location: Unknown";
        [ObservableProperty] private string? _tenure = "Tenure: Unknown";
        [ObservableProperty] private string? _following = "Following: Unknown";
        [ObservableProperty] private string? _followers = "Followers: Unknown";
        [ObservableProperty] private string? _gamepass = "Gamepass: Unknown";
        [ObservableProperty] private string? _bio = "Bio: Unknown";
        [ObservableProperty] private string _loginText = "Login";
        [ObservableProperty] public static bool _isLoggedIn = false;
        [ObservableProperty] public static bool _updateAvaliable = false;
        [ObservableProperty] private ObservableCollection<ImageItem> _watermarks = new ObservableCollection<ImageItem>();

        private readonly Lazy<XboxRestAPI> _xboxRestAPI;
        private readonly Lazy<GithubRestApi> _gitHubRestAPI = new Lazy<GithubRestApi>();

        public static int SpoofingStatus = 0; //0 = NotSpoofing, 1 = Spoofing, 2 = AutoSpoofing
        public static string SpoofedTitleID = "0";
        public static string AutoSpoofedTitleID = "0";

        //SnackBar
        public HomeViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            // Singleton in DI (App.xaml.cs) -- publish the one instance so background consumers
            // (heartbeat loops, HTTP-server routes) can reach instance methods like auth recovery.
            Instance = this;
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;

            // Assume XAUTH and System Language are set by the time this is actually instantiated
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        private const string XAuthScanPattern = "58 42 4C 33 2E 30 20 78 3D";

        // Newer Xbox app builds store the token as UTF-16 ("XBL3.0 x=" with
        // interleaved null bytes). Read with Encoding.Unicode, not UTF-8.
        private const string XAuthScanPatternUtf16 = "58 00 42 00 4C 00 33 00 2E 00 30 00 20 00 78 00 3D 00";

        [RelayCommand]
        private void RefreshProfile()
        {
            GrabProfile(force: true);
        }

        [RelayCommand]
        private void OpenXboxStatusPage()
        {
            try
            {
                var p = new Process();
                p.StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = "https://support.xbox.com/en-US/xbox-live-status"
                };
                p.Start();
            }
            catch (Exception ex)
            {
                EventsLog($"[XBLSTATUS] could not open status page: {ex.GetType().Name}: {ex.Message}");
            }
        }

        Mem m = new Mem();
        public BackgroundWorker XauthWorker = new BackgroundWorker();
        public BackgroundWorker EventsTokenWorker = new BackgroundWorker();
        bool IsAttached = false;
        bool GrabbedProfile = false;
        bool _grabProfileInFlight = false;
        public static bool XAUTHTested = false;
        public static string XAUTH = "";
        public static string XUIDOnly;
        public static bool InitComplete = false;

        // The single DI-registered HomeViewModel instance (see ctor). Static state like XAUTH lives
        // on this class; instance entry points (TryRecoverAuthAsync) are reached through this.
        public static HomeViewModel? Instance;

        // Cadence/anti-thrash state for the memory-scan token grabber. While we already hold a
        // (possibly expired) token we don't re-scan the whole user address space on every ~1s poll
        // tick -- only often enough to notice the Xbox app refreshing to a NEW token. See
        // ShouldScanForXauth / ShouldAdoptScannedToken.
        // How often the logged-in probe re-validates a HELD token. Without this an OAuth/SISU XBL3.0
        // token that expires hours into a session is never detected: TestXAUTH used to run only while
        // signed OUT, so the first sign of expiry was every other API call 401-ing at once.
        private static readonly TimeSpan LoggedInProbeInterval = TimeSpan.FromMinutes(15);

        private static readonly TimeSpan XauthScanInterval = TimeSpan.FromSeconds(5);
        private static DateTime _lastXauthScanUtc = DateTime.MinValue;
        private bool _xauthScanInFlight = false;

        // Re-test cadence, deliberately separate from adoption. Because we only adopt a CHANGED token
        // (ShouldAdoptScannedToken), a token whose first TestXAUTH transiently failed/401'd would never be
        // re-tested if the test were gated purely off XAUTHTested -- the old spammy code only self-healed
        // by re-adopting the same token every tick. So give the test its own bounded re-arm clock plus an
        // in-flight lock: at most one attempt per XauthScanInterval, no overlap, and it keeps retrying the
        // token we hold so a token that merely wasn't ready yet (fresh launch) still logs in.
        private static DateTime _lastXauthTestUtc = DateTime.MinValue;
        private bool _xauthTestInFlight = false;
        private readonly object _authStateLock = new object();
        private int _authGeneration;
        private CancellationTokenSource _eventsCaptureCts = new CancellationTokenSource();

        private bool IsCurrentAuthGeneration(int generation) =>
            Volatile.Read(ref _authGeneration) == generation;

        // Tunable, persisted (XAUSettings.XauthScanReadLength): how many bytes ReadString() copies at each
        // AoB hit while locating the sign-in token. ReadString is zero-terminated (it splits on the first
        // NUL), so a generous value is essentially free; the ONE real hazard is a value that is too SMALL,
        // which truncates a valid token so TestXAUTH then sees a malformed token and gets a 400 (not a 401)
        // -- exactly the "could my token just be invalid?" false positive. The floor sits far above the
        // ~2.6KB token actually observed, so a genuine token can never be cut. See NormalizeScanReadLength.
        public const int MinScanReadLength = 4096;
        public const int MaxScanReadLength = 262144;
        public const int DefaultScanReadLength = 16384;
        public static int ScanReadLength = DefaultScanReadLength;

        public static bool ShouldAcceptScanReadLength(int length)
            => length >= MinScanReadLength && length <= MaxScanReadLength;

        public static int NormalizeScanReadLength(int length)
        {
            if (length < MinScanReadLength)
                return MinScanReadLength;
            if (length > MaxScanReadLength)
                return MaxScanReadLength;
            return length;
        }

        private bool _isInitialized = false;
        private bool _isInitializing = false;
        string SettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
        string EventsMetaFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "Events", "meta.json");
        string AuthFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "auth.json");
        public CodeFlowAuthenticator oauth;
        public XboxAuthClient xboxAuthClient;
        public XboxSignedClient xboxSignedClient;

        public async void OnNavigatedTo()
        {
            // InitializeViewModel awaits network calls (update checks) before it flips
            // _isInitialized, so a quick navigate-away-and-back could re-enter it and call
            // XauthWorker.RunWorkerAsync() a second time -> "BackgroundWorker is currently
            // busy". Gate on an in-progress flag as well, not just the completed flag.
            if (!ShouldBeginInitialization(_isInitialized, _isInitializing))
                return;

            _isInitializing = true;
            try
            {
                await InitializeViewModel();
            }
            finally
            {
                _isInitializing = false;
            }
        }
        public void OnNavigatedFrom() { }

        /// <summary>
        /// True only when InitializeViewModel should run: it has neither completed nor is
        /// already in progress. Prevents re-entrant navigation from double-running init
        /// (which used to double-start the BackgroundWorkers and crash).
        /// </summary>
        public static bool ShouldBeginInitialization(bool initialized, bool initializing)
            => !initialized && !initializing;

        /// <summary>
        /// True when a BackgroundWorker may be (re)started. Starting an already-busy worker
        /// throws InvalidOperationException; this guard (plus the catch in StartWorker) keeps
        /// the "currently busy" crash from reaching the unhandled-exception dialog.
        /// </summary>
        public static bool ShouldStartWorker(bool isBusy) => !isBusy;

        private void StartWorker(BackgroundWorker worker)
        {
            if (!ShouldStartWorker(worker.IsBusy))
                return;
            try
            {
                worker.RunWorkerAsync();
            }
            catch (InvalidOperationException)
            {
                // Worker was started on another thread between the IsBusy check and the call.
            }
        }

        #region Update
        private async Task CheckForToolUpdates()
        {
            if (ToolVersion == "EmptyDevToolVersion")
                return;

            if (ToolVersion.Contains("DEV"))
            {
                var jsonResponse = await _gitHubRestAPI.Value.GetDevToolVersionAsync();

                if (("DEV-" + jsonResponse.LatestBuildVersion.ToString()) != ToolVersion)
                {
                    var result = await _contentDialogService.ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = $"Version {jsonResponse.LatestBuildVersion.ToString()} available to download",
                            Content = "Would you like to update to this version?",
                            PrimaryButtonText = "Update",
                            CloseButtonText = "Cancel"
                        }
                    );
                    if (result == ContentDialogResult.Primary)
                    {
                        _snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                        string sourceFile = jsonResponse.DownloadURL.ToString();
                        string destFile = @"XAU-new.exe";
                        var fileDownloader = new FileDownloader();
                        await fileDownloader.DownloadFileAsync(new Uri(sourceFile).ToString(), destFile, UpdateTool);
                    }
                }
            }
            else
            {
                var jsonResponse = await _gitHubRestAPI.Value.GetReleaseVersionAsync();

                if (jsonResponse.Count == 0)
                    return;

                if (jsonResponse[0].tag_name.ToString() != ToolVersion)
                {
                    var result = await _contentDialogService.ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = $"Version {jsonResponse[0].tag_name.ToString()} available to download",
                            Content = "Would you like to update to this version?",
                            PrimaryButtonText = "Update",
                            CloseButtonText = "Cancel"
                        }
                    );
                    if (result == ContentDialogResult.Primary)
                    {
                        _snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                        string sourceFile = jsonResponse[0].assets[0].browser_download_url.ToString();
                        string destFile = @"XAU-new.exe";
                        var fileDownloader = new FileDownloader();
                        await fileDownloader.DownloadFileAsync(sourceFile, destFile, UpdateTool);
                    }
                }
            }

        }
        private async void CheckForEventUpdates()
        {
            try
            {
                if (EventsVersion == "EmptyDevEventsVersion")
                    return;
                var response = await _gitHubRestAPI.Value.CheckForEventUpdatesAsync();
                var EventsTimestamp = 0;
                if (File.Exists(EventsMetaFilePath))
                {
                    var metaJson = File.ReadAllText(EventsMetaFilePath);
                    var meta = JsonConvert.DeserializeObject<EventsUpdateResponse>(metaJson);
                    EventsTimestamp = meta.Timestamp;
                }

                if (response.Timestamp > EventsTimestamp && response.DataVersion == EventsVersion)
                {
                    _snackbarService.Show("Downloading Events Update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                    UpdateEvents();
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Events Update Check Failed", $"Could not check for event updates: {ex.Message}",
                    ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }
        }

        private void UpdateTool(object sender, AsyncCompletedEventArgs e)
        {
            var path = Environment.ProcessPath.ToString();
            string[] splitpath = path.Split("\\");
            using (StreamWriter writer = new StreamWriter("XAU-Updater.bat"))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("timeout 1 > nul");
                writer.WriteLine("del \"" + Environment.ProcessPath + "\" ");
                writer.WriteLine("del \"" + splitpath[splitpath.Count() - 1] + "\" ");
                writer.WriteLine("ren XAU-new.exe \"" + splitpath[splitpath.Count() - 1] + "\" ");
                writer.WriteLine("start \"\" " + "\"" + splitpath[splitpath.Count() - 1] + "\"");
                writer.WriteLine("goto 2 > nul & del \"%~f0\"");
            }
            Process proc = new Process();
            proc.StartInfo.FileName = "XAU-Updater.bat";
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.Start();
            Environment.Exit(0);
        }

        private async void UpdateEvents()
        {
            string XAUPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
            string backupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XAU", "Events", "Backup");
            Directory.CreateDirectory(backupFolderPath);
            string eventsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XAU", "Events");
            string[] eventFiles = Directory.GetFiles(eventsFolderPath);
            string[] backupFiles = Directory.GetFiles(backupFolderPath);

            foreach (string file in backupFiles)
            {
                File.Delete(file);
            }
            foreach (string eventFile in eventFiles)
            {
                string fileName = Path.GetFileName(eventFile);
                string destinationPath = Path.Combine(backupFolderPath, fileName);
                File.Move(eventFile, destinationPath, true);
            }

            string zipFilePath = Path.Combine(XAUPath, "Events.zip");
            string extractPath = XAUPath;

            using (var client = new FileDownloader())
            {
                await client.DownloadFileAsync(EventsUrls.Zip, zipFilePath);
            }
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            File.Delete(zipFilePath);
            //download and place meta.json in the events folder
            string MetaFilePath = Path.Combine(eventsFolderPath, "meta.json");
            using (var client = new FileDownloader())
            {
                await client.DownloadFileAsync(EventsUrls.MetaUrl, MetaFilePath);
            }
            _snackbarService.Show("Events Update Complete", "Events have been updated to the latest version.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
        }

        private async void CheckForXboxGamesDatabaseUpdate()
        {
            try
            {
                var fileInfo = await _gitHubRestAPI.Value.GetXboxGamesDatabaseInfoAsync();
                if (fileInfo == null)
                {
                    _snackbarService.Show("Error", "Could not check for database updates.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                string titleSearchPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "TitleSearch");
                string shaFilePath = Path.Combine(titleSearchPath, "xbox_games_sha.txt");
                string dbFilePath = Path.Combine(titleSearchPath, "xbox_games.db");

                Directory.CreateDirectory(titleSearchPath);

                string currentSha = string.Empty;
                try
                {
                    if (File.Exists(shaFilePath))
                    {
                        currentSha = (await File.ReadAllTextAsync(shaFilePath)).Trim();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(currentSha) || !currentSha.Equals(fileInfo.Sha, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("Database Update", "New Xbox games database available. Downloading...", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);

                    using var client = new HttpClient();
                    var response = await client.GetAsync(fileInfo.DownloadUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsByteArrayAsync();

                    await File.WriteAllBytesAsync(dbFilePath, content);

                    try
                    {
                        await File.WriteAllTextAsync(shaFilePath, fileInfo.Sha);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to store database SHA: {ex.Message}");
                    }

                    _snackbarService.Show("Success", "Xbox games database updated successfully!", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                }
                else
                {
                    Console.WriteLine("Xbox games database is up to date.");
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", $"Database update check failed: {ex.Message}", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        #endregion

        private async Task InitializeViewModel()
        {
            try
            {
                await CheckForToolUpdates();
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Update Check Failed", $"Could not check for updates: {ex.Message}",
                    ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }
            XauthWorker.DoWork += XauthWorker_DoWork;
            XauthWorker.ProgressChanged += XauthWorker_ProgressChanged;
            XauthWorker.RunWorkerCompleted += XauthWorker_RunWorkerCompleted;
            XauthWorker.WorkerReportsProgress = true;
            StartWorker(XauthWorker);
            EventsTokenWorker.DoWork += EventsTokenWorker_DoWork;
            if (!File.Exists(SettingsFilePath))
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "XAU")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"));
                }

                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "XAU\\Events")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU\\Events"));
                }
                var defaultSettings = new XAUSettings
                {
                    SettingsVersion = "2",
                    ToolVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    UnlockAllEnabled = false,
                    AutoSpooferEnabled = false,
                    AutoLaunchXboxAppEnabled = false,
                    FakeSignatureEnabled = true,
                    RegionOverride = false,
                    UseAcrylic = false,
                    PrivacyMode = false,
                    OAuthLogin = false
                };
                string defaultSettingsJson = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
                using (var file = new StreamWriter(SettingsFilePath))
                {
                    file.Write(defaultSettingsJson);
                }
                if (Settings.OAuthLogin)
                {
                    _ = OAuthLogin();
                }

            }
            CheckForEventUpdates();
            CheckForXboxGamesDatabaseUpdate();
            LoadSettings();
            if (Settings.OAuthLogin)
                _ = OAuthLogin();
            _isInitialized = true;
            if (Settings.AutoLaunchXboxAppEnabled && XboxAppPid() == 0)
            {
                var p = new Process();
                var startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = @"shell:appsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"
                };

                if (Settings.LaunchHidden)
                {
                    startInfo.WindowStyle = ProcessWindowStyle.Hidden;
                }
                p.StartInfo = startInfo;
                p.Start();
            }
        }

        #region Xauth
        // The updated Xbox app (2612.x) no longer runs XboxPcApp.exe; its
        // full-trust host process XboxPcAppFT.exe holds the token in memory.
        private bool OpenXboxAppProcess()
        {
            if (m.OpenProcess(ProcessNames.XboxPcApp))
                return true;
            return m.OpenProcess(ProcessNames.XboxPcAppFt);
        }

        private int XboxAppPid()
        {
            int pid = m.GetProcIdFromName(ProcessNames.XboxPcApp);
            if (pid == 0)
                pid = m.GetProcIdFromName(ProcessNames.XboxPcAppFt);
            return pid;
        }

        public void XauthWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // OAuth mode: the token comes from SISU, not the Xbox app's memory, so there is nothing to
            // attach/scan. Stay ALIVE (idle tick) so ProgressChanged keeps firing: it drives the periodic
            // logged-in probe that detects an expired XBL3.0 token and triggers silent recovery. The old
            // `while (!Settings.OAuthLogin)` exit made this worker die forever on OAuth login, which is
            // why a token expiring hours later was never detected or refreshed.
            if (Settings.OAuthLogin)
            {
                Thread.Sleep(15000);
                XauthWorker.ReportProgress(0);
                return;
            }
            while (!Settings.OAuthLogin)
            {
                if (!OpenXboxAppProcess())
                {
                    IsAttached = false;
                    Thread.Sleep(1000);
                }
                else
                {
                    IsAttached = true;
                }
                Thread.Sleep(1000);
                XauthWorker.ReportProgress(0);
            }
            Thread.Sleep(5000);
        }
        public void XauthWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (IsAttached || XAUTH.Length > 0)
            {
                Attached = $"Attached to xbox app ({XboxAppPid().ToString()})";
                AttachedColor = new SolidColorBrush(Colors.Green);
                if (IsLoggedIn)
                {
                    if (!GrabbedProfile)
                        GrabProfile();
                    LoggedIn = "Logged In";
                    LoggedInColor = new SolidColorBrush(Colors.Green);

                    // Periodically re-validate the token even while logged in, so an expiry mid-session
                    // (typical after hours of spoofing) is caught here and recovered, instead of surfacing
                    // as random 401s from every consumer at once.
                    if (XAUTH.Length > 0 &&
                        ShouldTestXauth(_xauthTestInFlight, _lastXauthTestUtc, DateTime.UtcNow, LoggedInProbeInterval))
                    {
                        _xauthTestInFlight = true;
                        _lastXauthTestUtc = DateTime.UtcNow;
                        TestXAUTH();
                    }
                }
                else
                {
                    if (!SettingsViewModel.ManualXauth && !Settings.OAuthLogin)
                    {
                        GetXAUTH();
                        SettingsViewModel.ManualXauth = false;
                    }
                    LoggedIn = "Not Logged In";
                    LoggedInColor = new SolidColorBrush(Colors.Red);

                    // Test on its own bounded cadence rather than keying off XAUTHTested. Keep retrying the
                    // token we hold so a transient 401 / not-yet-ready token still logs the user in, instead
                    // of one failed attempt latching us out until a genuinely different token shows up.
                    if (XAUTH.Length > 0 &&
                        ShouldTestXauth(_xauthTestInFlight, _lastXauthTestUtc, DateTime.UtcNow, XauthScanInterval))
                    {
                        _xauthTestInFlight = true;
                        _lastXauthTestUtc = DateTime.UtcNow;
                        TestXAUTH();
                    }
                }
            }
            if (XboxAppPid() == 0)
            {
                Attached = "Not Attached";
                AttachedColor = new SolidColorBrush(Colors.Red);
            }
        }
        public void XauthWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StartWorker(XauthWorker);
        }
        /// <summary>
        /// True when the ~1s XauthWorker tick may start a fresh AoBScan. Once we hold a token we
        /// only re-scan on XauthScanInterval so we can notice the Xbox app refreshing to a new
        /// token, instead of hammering the whole address space (and the Xbox profile API, via the
        /// re-armed TestXAUTH) every tick. `inFlight` stops overlapping scans; the `DateTime.MinValue`
        /// fast-path makes the very first scan immediate.
        /// </summary>
        public static bool ShouldScanForXauth(DateTime lastScanUtc, DateTime nowUtc, bool inFlight)
        {
            if (inFlight)
                return false;
            if (lastScanUtc == DateTime.MinValue)
                return true;
            return (nowUtc - lastScanUtc) >= XauthScanInterval;
        }

        /// <summary>
        /// True when a scanned token should be adopted. Keeps the original frequency>3 confidence
        /// bar, but additionally requires the string to DIFFER from the token we already hold:
        /// re-adopting an identical token would reset XAUTHTested=false and re-arm TestXAUTH, so an
        /// expired token would be re-tested once per poll tick forever. Ignoring unchanged tokens
        /// lets that dead token settle until the Xbox app produces a genuinely new one.
        /// </summary>
        public static bool ShouldAdoptScannedToken(string scanned, string current, int frequency,
            int distinctNonEmptyCandidates = -1, bool heldTokenRejected = false)
        {
            if (string.IsNullOrEmpty(scanned))
                return false;
            // Standard confidence bar. Exceptions, only while the session is unproven:
            // 1. Nothing held + a lone candidate: the Xbox app sometimes exposes just 1-3 copies
            //    (log 2026-09-30 17:11: bestFreq=3, need >3) and the old rule blocked login forever.
            // 2. Held token was rejected (401) while signed out + few distinct candidates: several
            //    plausible tokens/truncations compete at freq 1 (log 2026-09-30 17:41: valid=3,
            //    bestFreq=1). Frequency cannot pick a winner; the profile API referees, so rotate.
            if (frequency <= 3)
            {
                bool rotate = heldTokenRejected && distinctNonEmptyCandidates is >= 2 and <= 8;
                bool lone = current.Length == 0 && distinctNonEmptyCandidates == 1;
                if (!rotate && !lone)
                    return false;
            }
            return scanned != current;
        }

        /// <summary>
        /// Picks the most-frequent NON-empty scanned token (tie broken by the longer string, a better
        /// bet against a real token than a truncated read). Empties -- what ReadString returns "" for when
        /// an address is unreadable or below 0x10000 -- are ignored so they can never outrank a genuine
        /// token. That was the bug behind "can't log in": the old single top-1 pick let the most-common
        /// empty string (a very high duplicate count) defeat ShouldAdoptScannedToken's empty-check and
        /// block adoption outright. Returns ("", 0) when every candidate is empty. Pure; unit-testable.
        /// </summary>
        public static string SelectBestScannedToken(IEnumerable<KeyValuePair<string, int>> frequency, out int bestFrequency)
        {
            bestFrequency = 0;
            string best = "";
            if (frequency == null)
                return best;

            foreach (var pair in frequency)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                    continue;
                if (pair.Value > bestFrequency
                    || (pair.Value == bestFrequency && pair.Key.Length > best.Length))
                {
                    best = pair.Key;
                    bestFrequency = pair.Value;
                }
            }
            return best;
        }

        /// <summary>Number of DISTINCT non-empty token strings in the scan, for the lone-candidate rule.</summary>
        public static int CountDistinctNonEmptyTokens(IEnumerable<KeyValuePair<string, int>> frequency)
        {
            if (frequency == null)
                return 0;
            int distinct = 0;
            foreach (var pair in frequency)
                if (!string.IsNullOrWhiteSpace(pair.Key))
                    distinct++;
            return distinct;
        }

        /// <summary>
        /// Structural check for a scanned XBL3.0 token. The AoB pattern only proves the prefix is
        /// present; a long read from an unterminated buffer returns the prefix followed by
        /// arbitrary memory, which frequency counting treats as a distinct garbage candidate
        /// (log 2026-09-30 17:36: distinct=11, bestFreq=1, bestLen=16384). A real token is
        /// "XBL3.0 x={digits};{token body}" with no whitespace, in a plausible size range.
        /// </summary>
        private static readonly System.Text.RegularExpressions.Regex XblTokenShape =
            new(@"^XBL3\.0 x=\d{6,20};[A-Za-z0-9+/_=\-\.]{200,8000}$",
                System.Text.RegularExpressions.RegexOptions.Compiled);

        public static bool IsPlausibleXblToken(string? candidate) =>
            !string.IsNullOrWhiteSpace(candidate) && XblTokenShape.IsMatch(candidate);

        /// <summary>Keeps only candidates that parse as XBL3.0 tokens, dropping prefixed garbage.</summary>
        public static Dictionary<string, int> FilterPlausibleTokens(IEnumerable<KeyValuePair<string, int>> frequency)
        {
            var filtered = new Dictionary<string, int>();
            if (frequency == null)
                return filtered;
            foreach (var pair in frequency)
                if (IsPlausibleXblToken(pair.Key))
                    filtered[pair.Key] = pair.Value;
            return filtered;
        }

        /// <summary>
        /// True when a TestXAUTH attempt may run: never overlapping (inFlight) and at most one per
        /// <paramref name="interval"/>. Deliberately does NOT consult XAUTHTested -- a token that 401'd once
        /// must still be retried, because it can become valid (Xbox app still authenticating after launch)
        /// or the failure may have been transient. Keying the retry off XAUTHTested as a permanent latch was
        /// what caused "not logging in on a fresh launch."
        /// </summary>
        public static bool ShouldTestXauth(bool inFlight, DateTime lastTestUtc, DateTime nowUtc, TimeSpan interval)
        {
            if (inFlight)
                return false;
            if (lastTestUtc == DateTime.MinValue)
                return true;
            return (nowUtc - lastTestUtc) >= interval;
        }

        private async void GetXAUTH()
        {
            if (!ShouldScanForXauth(_lastXauthScanUtc, DateTime.UtcNow, _xauthScanInFlight))
                return;

            int generation = Volatile.Read(ref _authGeneration);
            _xauthScanInFlight = true;
            try
            {
                var swScan = System.Diagnostics.Stopwatch.StartNew();
                // Scan both ASCII and UTF-16 layouts; the Xbox app changed the
                // on-memory token encoding between versions.
                var asciiCandidates = await CollectXauthCandidates(XAuthScanPattern, Encoding.UTF8);
                var utf16Candidates = await CollectXauthCandidates(XAuthScanPatternUtf16, Encoding.Unicode);
                var XauthStrings = asciiCandidates.Concat(utf16Candidates).ToArray();
                if (!IsCurrentAuthGeneration(generation))
                    return;
                long scanMs = swScan.ElapsedMilliseconds;
                // The expensive AoBScan has now run, so start backing off from here regardless of
                // whether this particular scan yielded a usable token.
                _lastXauthScanUtc = DateTime.UtcNow;

                long readMs = swScan.ElapsedMilliseconds - scanMs;

                Dictionary<string, int> frequency = new Dictionary<string, int>();
                foreach (string str in XauthStrings)
                {
                    if (!frequency.ContainsKey(str))
                    {
                        frequency[str] = 1;
                    }
                    else
                    {
                        frequency[str]++;
                    }
                }

                if (XauthStrings.Length == 0)
                {
                    DiagLog.Write(
                        $"[XAUTHDBG] scan found no XBL3.0 token candidates in Xbox app memory (signed in there yet?) " +
                        $"[scan={scanMs}ms read={readMs}ms]");
                    return;
                }

                // Highest count over ALL keys (including the empty string unreadable addresses read back as).
                // Kept only so the "below confidence" diagnostic keeps its original, comparable meaning.
                int topFreqAll = 0;
                foreach (var pair in frequency)
                {
                    if (pair.Value > topFreqAll)
                        topFreqAll = pair.Value;
                }

                // Selection runs over STRUCTURALLY VALID candidates only: the AoB pattern matches
                // the prefix inside larger unterminated buffers, so raw reads are often prefix +
                // memory garbage (log 2026-09-30 17:36: distinct=11, bestFreq=1, bestLen=16384).
                // Empties and garbage never outrank a genuine token this way.
                Dictionary<string, int> validFrequency = FilterPlausibleTokens(frequency);

                // Adopt the most-frequent valid candidate; a lone valid candidate is accepted
                // below the confidence bar only when we hold nothing (ShouldAdoptScannedToken).
                // Rotation while SIGNED OUT (not just after a rejection): at fresh startup several
                // plausible candidates compete at freq 1 (log 2026-09-30 18:03: valid=4, bestFreq=1,
                // currentXAUThLen=0) and nothing is ever adopted to reject — the gate must be open
                // from the first scan. The profile API referees; once logged in the bar resumes.
                bool rotating = !IsLoggedIn;
                Dictionary<string, int> selectableFrequency = rotating && XAUTH.Length > 0
                    ? validFrequency.Where(kvp => kvp.Key != XAUTH)
                        .ToDictionary(kvp => kvp.Key, kvp => kvp.Value)
                    : validFrequency;

                string best = SelectBestScannedToken(selectableFrequency, out int bestFreq);
                int distinctNonEmpty = CountDistinctNonEmptyTokens(validFrequency);
                bool adoptedNew;
                lock (_authStateLock)
                {
                    if (!IsCurrentAuthGeneration(generation))
                        return;
                    adoptedNew = ShouldAdoptScannedToken(best, XAUTH, bestFreq, distinctNonEmpty,
                        heldTokenRejected: rotating);
                    if (adoptedNew)
                    {
                        XAUTH = best;
                        XAUTHTested = false;
                    }
                }

                DiagLog.Write(
                    $"[XAUTHDBG] scan: candidates={XauthStrings.Length}, distinct={frequency.Count}, " +
                    $"valid={validFrequency.Count}, topFreq={topFreqAll} (need >3, lone-valid bypasses), " +
                    $"bestFreq={bestFreq}, bestLen={best.Length}, " +
                    $"currentXAUThLen={XAUTH.Length}, adoptedNew={adoptedNew} [scan={scanMs}ms read={readMs}ms]" +
                    (best.Length == 0
                        ? " (no structurally valid token in memory -- Xbox app signed out or token not fully written yet?)"
                        : ""));
            }
            catch (Exception ex)
            {
                // Failed scans need the same backoff as successful scans, unless this session was
                // invalidated by a cache clear that deliberately reset the clock for an immediate retry.
                if (IsCurrentAuthGeneration(generation))
                    _lastXauthScanUtc = DateTime.UtcNow;
                DiagLog.Write($"[XAUTHDBG] GetXAUTH scan failed: {ex.GetType().Name}; retrying on next cadence.");
            }
            finally
            {
                _xauthScanInFlight = false;
            }
        }

        private async Task<string[]> CollectXauthCandidates(string scanPattern, System.Text.Encoding encoding)
        {
            IEnumerable<long> scanResults = await m.AoBScan(scanPattern, true);
            var candidates = new List<string>();
            foreach (var address in scanResults)
            {
                string token = m.ReadString(address.ToString("X"), length: ScanReadLength, stringEncoding: encoding);
                if (!string.IsNullOrEmpty(token) && token.StartsWith("XBL3.0 x="))
                    candidates.Add(token);
            }
            return candidates.ToArray();
        }
        private async void TestXAUTH()
        {
            int generation = Volatile.Read(ref _authGeneration);
            string testedToken = XAUTH;
            DiagLog.Write($"[XAUTHDBG] TestXAUTH: verifying token (len={testedToken.Length}) via GetBasicProfileAsync...");
            try
            {
                var response = await _xboxRestAPI.Value.GetBasicProfileAsync();
                var user = response?.ProfileUsers?.FirstOrDefault();
                if (user == null || string.IsNullOrWhiteSpace(user.Id))
                    return;
                lock (_authStateLock)
                {
                    if (!IsCurrentAuthGeneration(generation) || XAUTH != testedToken)
                        return;
                    if (Settings.PrivacyMode)
                    {
                        GamerTag = "Gamertag: Hidden";
                        Xuid = "XUID: Hidden";
                    }
                    else
                    {
                        GamerTag = $"Gamertag: {user.Settings?.FirstOrDefault()?.Value}";
                        Xuid = $"XUID: {user.Id}";
                    }
                    XUIDOnly = user.Id;
                    IsLoggedIn = true;
                    XAUTHTested = true;
                    InitComplete = true;
                }
                StartWorker(EventsTokenWorker);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    lock (_authStateLock)
                    {
                        if (!IsCurrentAuthGeneration(generation) || XAUTH != testedToken)
                            return;
                        IsLoggedIn = false;
                        XAUTHTested = true;
                    }
                    DiagLog.Write("[XAUTHDBG] TestXAUTH: 401 Unauthorized -- token held is stale/expired; attempting silent re-acquisition...");
                    await TryRecoverAuthAsync();
                }
                else
                {
                    // Previously this non-401 case fell through silently and left the app stuck signed-out
                    // with no clue; surface the real status code. NOTE: HttpRequestException.StatusCode is
                    // NULLABLE -- it is null for connection-level failures (no HTTP response at all:
                    // socket reset, DNS failure, etc.). Casting it non-conditionally threw
                    // InvalidOperationException from inside this catch and crashed the app
                    // (async void + unhandled = crash dialog after ~9h of uptime).
                    DiagLog.Write($"[XAUTHDBG] TestXAUTH: HttpRequestException {(int?)ex.StatusCode ?? 0} {ex.StatusCode?.ToString() ?? "<no HTTP status - connection-level failure>"} (not 401) -- staying signed-out, will retry.");
                }
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[XAUTHDBG] TestXAUTH: threw {ex.GetType().Name}: {ex.Message} (swallowed; will retry on next cadence).");
            }
            finally
            {
                // Release the re-test lock so the next bounded attempt can run.
                _xauthTestInFlight = false;
            }
        }
        #endregion

        #region AuthRecovery
        // Central 401 recovery. Before this existed, an XBL3.0 token expiring mid-session (typical
        // after 24h of spoofing) left the app signed out forever: OAuth mode never re-ran SISU with
        // the saved refresh token, and memory-scan mode only recovered by luck when the Xbox app
        // happened to hold a fresh token. Every 401 consumer now funnels through here.
        private int _authRecoveryInFlight = 0;
        private DateTime _lastAuthRecoveryUtc = DateTime.MinValue;
        private static readonly TimeSpan AuthRecoveryCooldown = TimeSpan.FromMinutes(2);

        // A heartbeat 401 can arrive before the periodic profile probe marks the held token
        // invalid. Memory-scan replacements must be profile-tested on the short signed-out
        // cadence before a waiting spoofer resumes.
        internal void MarkSessionRejected()
        {
            lock (_authStateLock)
            {
                // An earlier profile probe can still be in flight for the rejected token.
                // Discard its result instead of letting it mark that token logged in again.
                Interlocked.Increment(ref _authGeneration);
                IsLoggedIn = false;
                XAUTHTested = false;
                _lastXauthTestUtc = DateTime.MinValue;
            }
        }

        /// <summary>
        /// Attempts silent token re-acquisition after a 401. Returns true only after SISU has issued
        /// a new XAUTH (OAuth path); endpoint-specific authorization is still checked separately.
        /// In memory-scan mode there is nothing to await -- we force
        /// an immediate rescan and let the usual scan/adopt/test loop take over, so returns false and
        /// the caller stays signed-out until that loop succeeds.
        /// Fire-and-forget callers use StartAuthRecovery().
        /// </summary>
        public async Task<bool> TryRecoverAuthAsync()
        {
            if (Interlocked.CompareExchange(ref _authRecoveryInFlight, 1, 0) != 0)
                return false;
            try
            {
                var now = DateTime.UtcNow;
                if (_lastAuthRecoveryUtc != DateTime.MinValue && now - _lastAuthRecoveryUtc < AuthRecoveryCooldown)
                    return false;
                _lastAuthRecoveryUtc = now;

                ShowSnackbar("Refreshing authentication...",
                    "Your session token was rejected -- XAU is re-authenticating in the background.",
                    ControlAppearance.Info, SymbolRegular.ArrowClockwise24);

                if (Settings.OAuthLogin)
                    return await TryRecoverOAuthAsync();

                // Memory-scan mode: force a scan and profile-test its replacement before
                // another heartbeat can reuse a token merely found in process memory.
                DiagLog.Write("[AUTHRECOVERY] token rejected (401); forcing immediate XAUTH memory re-scan");
                MarkSessionRejected();
                SettingsViewModel.ManualXauth = false;
                _lastXauthScanUtc = DateTime.MinValue;
                GetXAUTH();
                return false;
            }
            finally
            {
                Interlocked.Exchange(ref _authRecoveryInFlight, 0);
            }
        }

        private async Task<bool> TryRecoverOAuthAsync()
        {
            int generation = Volatile.Read(ref _authGeneration);
            EnsureOAuthInitialized();

            MicrosoftOAuthResponse? saved = null;
            try
            {
                if (File.Exists(AuthFilePath))
                    saved = readSession();
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[AUTHRECOVERY] could not read saved session: {ex.GetType().Name}: {ex.Message}");
            }

            if (!IsCurrentAuthGeneration(generation))
                return false;
            if (saved == null || !saved.Validate() || string.IsNullOrEmpty(saved.RefreshToken))
            {
                DiagLog.Write("[AUTHRECOVERY] no usable saved OAuth session -- user must log in again");
                IsLoggedIn = false;
                return false;
            }

            try
            {
                var fresh = await oauth.AuthenticateSilently(saved.RefreshToken!);
                if (!IsCurrentAuthGeneration(generation))
                    return false;
                // Do not report an Xbox token until the SISU exchange finishes.
                bool recovered = await CompleteLoginAsync(fresh, generation);
                if (recovered && IsCurrentAuthGeneration(generation))
                    ShowSnackbar("Authentication refreshed", "A new Xbox session token was acquired.",
                        ControlAppearance.Success, SymbolRegular.Checkmark24);
                return recovered;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[AUTHRECOVERY] silent OAuth refresh failed: {ex.GetType().Name}");
                if (IsCurrentAuthGeneration(generation))
                {
                    IsLoggedIn = false;
                    ShowSnackbar("Re-authentication failed", "Please log in again from the Home page.",
                        ControlAppearance.Danger, SymbolRegular.ErrorCircle24);
                }
                return false;
            }
        }

        /// <summary>
        /// Builds the OAuth/SISU clients on demand. OAuthLogin() and silent recovery share this so
        /// recovery works even if the interactive login flow never ran in this process.
        /// </summary>
        private void EnsureOAuthInitialized()
        {
            if (oauth != null)
                return;
            var OAuthhttpClient = new HttpClient();
            var apiClient = new CodeFlowLiveApiClient(XboxGameTitles.XboxAppPC, XboxAuthConstants.XboxScope, OAuthhttpClient);
            xboxAuthClient = new XboxAuthClient(OAuthhttpClient);
            xboxSignedClient = new XboxSignedClient(OAuthhttpClient);
            oauth = new CodeFlowBuilder(apiClient)
                .WithUIParent(this)
                .Build();
        }

        /// <summary>
        /// Thread-safe snackbar for background callers (auth recovery fires from heartbeat loops and
        /// the HTTP server, not just the UI thread). Wpf.Ui's SnackbarService is UI-bound, so marshal
        /// through the dispatcher when we're not already on it.
        /// </summary>
        private void ShowSnackbar(string title, string message, ControlAppearance appearance, SymbolRegular symbol)
        {
            void Show() => _snackbarService.Show(title, message, appearance, new SymbolIcon(symbol), _snackbarDuration);
            // Fully qualified: this project uses both WPF and WinForms, so the bare "Application"
            // name is ambiguous (CS0104).
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                Show();
            else
                dispatcher.Invoke(Show);
        }

        /// <summary>Fire-and-forget entry point for background consumers (heartbeat loops, API routes).</summary>
        public void StartAuthRecovery()
        {
            _ = TryRecoverAuthAsync();
        }

        #endregion

        #region EventsToken

        private static readonly TimeSpan EventsTokenCheckInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan EventsTokenMaxAge = TimeSpan.FromHours(23);

        private static DateTime _eventsTokenObtainedAt = DateTime.MinValue;
        private static string _eventsUserHash = null;

        internal static bool TryApplyCapturedEventsToken(string? captured, ref string? current,
            ref DateTime obtainedAtUtc, DateTime nowUtc)
        {
            if (string.IsNullOrWhiteSpace(captured))
                return false;
            current = captured;
            obtainedAtUtc = nowUtc;
            return true;
        }

        private static readonly string EventsLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "events_debug.log");

        public static void EventsLog(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EventsLogPath)!);
                File.AppendAllText(EventsLogPath, line + Environment.NewLine);
            }
            catch { }
        }


        public static readonly object SettingsWriteLock = new object();

        public bool PersistEventsToken()
        {
            try
            {
                lock (SettingsWriteLock)
                {
                    Settings.CachedEventsToken = AchievementsViewModel.EventsToken;
                    Settings.EventsTokenObtainedAt = _eventsTokenObtainedAt;
                    Settings.EventsUserHash = _eventsUserHash;
                    File.WriteAllText(SettingsFilePath, JsonConvert.SerializeObject(Settings));
                }
                return true;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[SETTINGS] failed to persist settings: {ex.GetType().Name}");
                return false;
            }
        }

        public void EventsTokenWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            try
            {
                EventsTokenWorkerLoop();
            }
            catch (Exception ex)
            {
                EventsLog($"Worker crashed: {ex.Message}");
            }
        }

        private void EventsTokenWorkerLoop()
        {
            EventsLog("Worker started");
            // Wait for login before scanning
            while (!IsLoggedIn)
            {
                Thread.Sleep(2000);
            }
            EventsLog("Logged in, entering refresh loop");

            // If a token already exists (e.g. from OAuth or cache), mark it as fresh
            if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken) && _eventsTokenObtainedAt == DateTime.MinValue)
                _eventsTokenObtainedAt = DateTime.UtcNow;

            while (true)
            {
                if (!Settings.AutoGrabEventsToken || !IsLoggedIn)
                {
                    Thread.Sleep(5000);
                    continue;
                }

                var currentToken = AchievementsViewModel.EventsToken;
                bool isEmpty = string.IsNullOrEmpty(currentToken);
                bool isValid = !isEmpty && IsEventsTokenValid();

                // A cached/captured token is bound to the Xbox account that produced it (the user hash
                // inside "x:XBL3.0 x={hash};..."). If the signed-in account changed since capture, the
                // token is useless even though it is still structurally "valid" -- discard it so the
                // loop re-captures for the CURRENT account instead of sending another account's ticket.
                if (!isEmpty && isValid &&
                    !string.IsNullOrEmpty(_eventsUserHash) &&
                    !string.IsNullOrEmpty(HomeViewModel.XUIDOnly) &&
                    _eventsUserHash != HomeViewModel.XUIDOnly)
                {
                    EventsLog("Cached events token belongs to a different account -- discarding and re-grabbing.");
                    AchievementsViewModel.EventsToken = null;
                    _eventsTokenObtainedAt = DateTime.MinValue;
                    PersistEventsToken();
                    continue;
                }

                var tokenAge = DateTime.UtcNow - _eventsTokenObtainedAt;
                bool isExpired = !isEmpty && isValid && tokenAge > EventsTokenMaxAge;

                EventsLog($"Check: empty={isEmpty}, valid={isValid}, age={tokenAge.TotalMinutes:F0}m, expired={isExpired}");

                if (isEmpty || !isValid || isExpired)
                {
                    if (isExpired)
                        EventsLog($"Token expired (age: {tokenAge.TotalMinutes:F0}m > {EventsTokenMaxAge.TotalMinutes:F0}m), refreshing...");
                    else
                        EventsLog("Token missing/invalid, refreshing...");

                    // Keep capturing until we get a token or settings change. Take the shared ETW gate
                    // (with a timeout so we can't wedge behind a long manual grab) so the auto worker and
                    // the manual button never drive the process-wide trace / Solitaire at the same time.
                    while (Settings.AutoGrabEventsToken && IsLoggedIn)
                    {
                        if (!_etwGate.Wait(TimeSpan.FromSeconds(2)))
                        {
                            EventsLog("ETW busy (manual grab in progress?), deferring capture...");
                            Thread.Sleep(1000);
                            continue;
                        }
                        try
                        {
                            // Drive the FULL capture flow (launch Solitaire if needed -> capture its
                            // telemetry burst -> extract -> close it). The old code called
                            // EtwTokenCapture.Capture(20) directly, which only listens to network
                            // traffic: with no game emitting telemetry it captured silence forever and
                            // the auto refresh never recovered an expired token. This is the exact
                            // flow the manual button uses, and it is safe under _etwGate (single
                            // consumer by construction).
                            int generation = Volatile.Read(ref _authGeneration);
                            using var capture = CancellationTokenSource.CreateLinkedTokenSource(_eventsCaptureCts.Token);
                            capture.CancelAfter(TimeSpan.FromMinutes(2));
                            string? captured = GrabEventsTokenFromSolitaire(capture.Token);
                            lock (_authStateLock)
                            {
                                if (!IsCurrentAuthGeneration(generation))
                                    continue;
                                if (TryApplyCapturedEventsToken(captured, ref AchievementsViewModel.EventsToken,
                                        ref _eventsTokenObtainedAt, DateTime.UtcNow))
                                {
                                    _eventsUserHash = XUIDOnly;
                                    PersistEventsToken();
                                    EventsLog("Auto capture success (Solitaire telemetry burst).");
                                    break;
                                }
                            }
                        }
                        finally
                        {
                            _etwGate.Release();
                        }
                        EventsLog("Auto capture found no token, retrying in 30s...");
                        Thread.Sleep(30000);
                    }
                }

                EventsLog($"Sleeping {EventsTokenCheckInterval.TotalMinutes:F0}m...");
                Thread.Sleep(EventsTokenCheckInterval);
            }
        }

        /// <summary>
        /// Launches Solitaire (if needed), then continuously captures ETW network traffic
        /// and scans for the events token until one is found or Solitaire exits.
        /// </summary>
        private string? GrabEventsTokenFromSolitaire(CancellationToken cancellationToken)
        {
            bool alreadyRunning = Process.GetProcessesByName(ProcessNames.Solitaire).Length > 0;
            bool launchedByUs = false;
            string? method = null;
            bool traceRunning = false;
            EventsLog($"Solitaire already running: {alreadyRunning}");
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                // Start tracing before launching Solitaire so its initial telemetry is captured.
                EtwTokenCapture.Cleanup();
                method = EtwTokenCapture.Start();
                if (method == null)
                {
                    EventsLog("Failed to start ETW trace (not running as admin?)");
                    return null;
                }
                traceRunning = true;
                if (!alreadyRunning)
                {
                    var p = new Process
                    {
                        StartInfo = new ProcessStartInfo
                        {
                            UseShellExecute = true,
                            FileName = AppLaunchUris.Solitaire
                        }
                    };
                    p.Start();
                    launchedByUs = true;
                    for (int i = 0; i < 15 && Process.GetProcessesByName(ProcessNames.Solitaire).Length == 0; i++)
                        if (cancellationToken.WaitHandle.WaitOne(1000))
                            return null;
                    if (Process.GetProcessesByName(ProcessNames.Solitaire).Length == 0)
                    {
                        EventsLog("Solitaire never appeared after 15s");
                        return null;
                    }
                }

                EventsLog("Waiting 25s for Xbox Live init + telemetry events...");
                if (cancellationToken.WaitHandle.WaitOne(25000))
                    return null;
                EtwTokenCapture.Stop(method);
                traceRunning = false;
                if (cancellationToken.WaitHandle.WaitOne(2000))
                    return null;
                string? captured = EtwTokenCapture.ExtractTokens();
                EtwTokenCapture.CleanupFiles();
                if (!string.IsNullOrWhiteSpace(captured))
                    return captured;

                int attempt = 0;
                while (!cancellationToken.IsCancellationRequested &&
                       Process.GetProcessesByName(ProcessNames.Solitaire).Length > 0)
                {
                    attempt++;
                    EventsLog($"Capture attempt {attempt}...");
                    captured = EtwTokenCapture.Capture(20);
                    if (!string.IsNullOrWhiteSpace(captured))
                        return captured;
                    if (cancellationToken.WaitHandle.WaitOne(3000))
                        break;
                }
                return null;
            }
            catch (OperationCanceledException)
            {
                return null;
            }
            catch (Exception ex)
            {
                EventsLog($"ETW capture failed: {ex.GetType().Name}");
                return null;
            }
            finally
            {
                if (traceRunning && method != null)
                    EtwTokenCapture.Stop(method);
                EtwTokenCapture.CleanupFiles();
                if (launchedByUs)
                {
                    try
                    {
                        foreach (var proc in Process.GetProcessesByName(ProcessNames.Solitaire))
                            proc.Kill();
                    }
                    catch { }
                }
            }
        }

        /// <summary>
        /// Manually triggers a scan (from the "Manually Refresh Token" button).
        /// Works regardless of the auto-grab setting.
        /// </summary>
        // Serialises every ETW/Solitaire consumer (the manual grab AND the auto-grab worker).
        // GrabEventsTokenFromSolitaire drives process-wide ETW sessions, launches/kills Solitaire and
        // shares eventsTokenFound/EventsToken, so two of them at once stomp each other: one tears down
        // the trace another is extracting, or one nulls EventsToken right after the other set it.
        // That is why a burst of clicks (or manual+auto overlap) made "refresh" appear to do nothing.
        private readonly SemaphoreSlim _etwGate = new SemaphoreSlim(1, 1);

        // Backed by a volatile field: the single-flight check+set happens on the UI thread (button
        // clicks) and the clear happens on the background Task, polled by the DispatcherTimer.
        private volatile bool _manualScanRunning;
        public bool ManualScanRunning
        {
            get => _manualScanRunning;
            private set => _manualScanRunning = value;
        }

        /// <summary>
        /// Single-flight gate for the manual "Manually Refresh Token" button: a scan may start only when
        /// none is already running. Repeated clicks -- or the 3s UI poll re-enabling the button while a
        /// long Solitaire/ETW capture is still in progress -- are ignored rather than piling up
        /// concurrent grabs that cancel each other out.
        /// </summary>
        public static bool ShouldStartManualScan(bool alreadyRunning) => !alreadyRunning;

        public void ScanForEventsTokenManual()
        {
            // Synchronous single-flight on the UI thread: set true before Task.Run so an immediate
            // re-entrant click is refused, and reset in the Task's finally (exactly one owner).
            if (!ShouldStartManualScan(ManualScanRunning))
                return;
            ManualScanRunning = true;

            int generation = Volatile.Read(ref _authGeneration);
            CancellationToken sessionToken = _eventsCaptureCts.Token;
            System.Threading.Tasks.Task.Run(() =>
            {
                bool entered = false;
                try
                {
                    // Manual and automatic captures share the same process-wide ETW trace.
                    _etwGate.Wait(sessionToken);
                    entered = true;
                    lock (_authStateLock)
                    {
                        if (!IsCurrentAuthGeneration(generation))
                            return;
                        AchievementsViewModel.EventsToken = null;
                        _eventsTokenObtainedAt = DateTime.MinValue;
                    }

                    using var capture = CancellationTokenSource.CreateLinkedTokenSource(sessionToken);
                    capture.CancelAfter(TimeSpan.FromMinutes(2));
                    string? captured = GrabEventsTokenFromSolitaire(capture.Token);
                    lock (_authStateLock)
                    {
                        if (!IsCurrentAuthGeneration(generation))
                            return;
                        if (TryApplyCapturedEventsToken(captured, ref AchievementsViewModel.EventsToken,
                                ref _eventsTokenObtainedAt, DateTime.UtcNow))
                        {
                            _eventsUserHash = XUIDOnly;
                            PersistEventsToken();
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // Clear Auth Cache or logout invalidated this capture.
                }
                catch (Exception ex)
                {
                    EventsLog($"Manual capture failed: {ex.GetType().Name}");
                }
                finally
                {
                    if (entered)
                        _etwGate.Release();
                    ManualScanRunning = false;
                }
            });
        }

        /// <summary>
        /// Returns whether the current events token looks structurally valid.
        /// </summary>
        public static bool IsEventsTokenValid()
        {
            var token = AchievementsViewModel.EventsToken;
            return !string.IsNullOrWhiteSpace(token)
                && token.StartsWith("x:XBL3.0 x=")
                && token.Length > 30;
        }

        /// <summary>
        /// Returns whether the current events token has exceeded its max age.
        /// </summary>
        public static bool IsEventsTokenExpired()
        {
            if (_eventsTokenObtainedAt == DateTime.MinValue)
                return false; // no timestamp means we can't determine expiry
            return (DateTime.UtcNow - _eventsTokenObtainedAt) > EventsTokenMaxAge;
        }

        /// <summary>
        /// The UTC time the current events token was obtained.
        /// </summary>
        public static DateTime EventsTokenObtainedAtUtc => _eventsTokenObtainedAt;

        /// <summary>
        /// The UTC time the current events token is expected to expire.
        /// </summary>
        public static DateTime? EventsTokenExpiresAtUtc =>
            _eventsTokenObtainedAt == DateTime.MinValue
                ? null
                : _eventsTokenObtainedAt + EventsTokenMaxAge;
        #endregion

        #region OAuthLogin

        // Guards OAuthLogin() against re-entrancy: it fires from InitializeViewModel at startup AND
        // from the Login button, and two concurrent runs would race on the shared oauth clients and
        // silently double the restore/interactive attempts.
        private int _oauthLoginInFlight = 0;

        // Silent session-restore retry cadence (startup auto-login).
        private const int SilentRestoreAttempts = 3;
        private static readonly TimeSpan SilentRestoreRetryDelay = TimeSpan.FromSeconds(5);

        [RelayCommand]
        private async Task OAuthLogin()
        {
            if (Interlocked.CompareExchange(ref _oauthLoginInFlight, 1, 0) != 0)
                return;
            int generation = Volatile.Read(ref _authGeneration);
            try
            {
                EnsureOAuthInitialized();
                if (LoginText == "Logout")
                {
                    // Clear locally first even when provider signout fails or is slow.
                    bool cleared = ClearAuthCache();
                    try { await oauth.Signout(); }
                    catch (Exception ex) { DiagLog.Write($"[OAUTH] provider signout failed: {ex.GetType().Name}"); }
                    if (!cleared)
                        ShowSnackbar("Logout incomplete", "Saved authentication data could not all be cleared.",
                            ControlAppearance.Caution, SymbolRegular.Warning24);
                    return;
                }

                MicrosoftOAuthResponse? response = await TryRestoreSessionAsync(generation);
                if (response == null && IsCurrentAuthGeneration(generation))
                    await TryInteractiveLoginAsync(generation);
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[OAUTH] login failed: {ex.GetType().Name}");
                if (IsCurrentAuthGeneration(generation))
                    ShowSnackbar("Authentication failed", "Please retry Xbox sign-in.",
                        ControlAppearance.Danger, SymbolRegular.ErrorCircle24);
            }
            finally
            {
                Interlocked.Exchange(ref _oauthLoginInFlight, 0);
            }
        }

        private bool DeleteAuthFile()
        {
            try
            {
                File.Delete(AuthFilePath);
                return true;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[AUTH] saved session could not be deleted: {ex.GetType().Name}");
                return false;
            }
        }

        /// <summary>
        /// Clears all cached authentication state (auth.json, XAUTH token, events token).
        /// The user will need to log in again after calling this.
        /// Does NOT require a reboot.
        /// </summary>
        public bool ClearAuthCache()
        {
            lock (_authStateLock)
            {
                // Discard results of token requests and scans started by the old session.
                Interlocked.Increment(ref _authGeneration);
                _eventsCaptureCts.Cancel();
                _eventsCaptureCts = new CancellationTokenSource();
                bool deleted = DeleteAuthFile();

                XAUTH = "";
                XAUTHTested = false;
                InitComplete = false;
                _lastXauthScanUtc = DateTime.MinValue;
                _lastXauthTestUtc = DateTime.MinValue;
                SettingsViewModel.ManualXauth = false;

                IsLoggedIn = false;
                ClearProfileState();
                LoginText = "Login";
                Settings.OAuthLogin = false;
                bool persisted = PersistEventsToken();
                return deleted && persisted;
            }
        }

        private async Task<bool> CompleteLoginAsync(MicrosoftOAuthResponse response, int generation,
            string? successMessage = null)
        {
            try
            {
                bool published = await AwaitAndPublishAuthAsync(GenerateXboxTokenAsync(response), generation);
                if (!published)
                    return false;

                bool sessionSaved = true;
                bool settingsSaved;
                lock (_authStateLock)
                {
                    if (!IsCurrentAuthGeneration(generation))
                        return false;
                    try
                    {
                        writeSession(response);
                    }
                    catch (Exception ex)
                    {
                        sessionSaved = false;
                        DiagLog.Write($"[OAUTH] session established but saved session failed: {ex.GetType().Name}");
                    }
                    Settings.OAuthLogin = true;
                    settingsSaved = PersistEventsToken();
                }
                void FinishUi()
                {
                    if (!IsCurrentAuthGeneration(generation))
                        return;
                    LoginText = "Logout";
                    XauthWorker_ProgressChanged(null!, null!);
                    if (IsLoggedIn && !GrabbedProfile)
                        GrabProfile(force: true);
                }
                var dispatcher = System.Windows.Application.Current?.Dispatcher;
                if (dispatcher == null || dispatcher.CheckAccess())
                    FinishUi();
                else
                    dispatcher.Invoke(FinishUi);
                if (!IsCurrentAuthGeneration(generation))
                    return false;
                if (!sessionSaved || !settingsSaved)
                    ShowSnackbar("Session active, but not saved", "Sign-in worked, but the session or settings could not be saved for restart.",
                        ControlAppearance.Caution, SymbolRegular.Warning24);
                else if (!string.IsNullOrEmpty(successMessage))
                    ShowSnackbar("Success", successMessage, ControlAppearance.Success, SymbolRegular.Checkmark24);
                StartWorker(EventsTokenWorker);
                return true;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[OAUTH] device token/SISU or session persistence failed: {ex.GetType().Name}");
                if (IsCurrentAuthGeneration(generation))
                    ShowSnackbar("Authentication failed", "Could not complete Xbox sign-in. Please retry.",
                        ControlAppearance.Danger, SymbolRegular.ErrorCircle24);
                return false;
            }
        }

        private async Task<(string, string, string?)> GenerateXboxTokenAsync(MicrosoftOAuthResponse response)
        {
            var deviceToken = await xboxSignedClient.RequestDeviceToken(XboxDeviceTypes.Win32, "0.0.0");
            var sisuResult = await xboxSignedClient.SisuAuth(new XboxSisuAuthRequest
            {
                AccessToken = response.AccessToken,
                ClientId = XboxGameTitles.XboxAppPC,
                DeviceToken = deviceToken.Token,
                RelyingParty = XboxAuthConstants.XboxLiveRelyingParty,
            });
            var authorization = sisuResult?.AuthorizationToken;
            var claims = authorization?.XuiClaims;
            if (claims == null || string.IsNullOrWhiteSpace(claims.UserHash) ||
                string.IsNullOrWhiteSpace(claims.XboxUserId) || string.IsNullOrWhiteSpace(authorization?.Token))
                throw new InvalidOperationException("SISU returned incomplete Xbox authorization claims.");
            string auth = $"XBL3.0 x={claims.UserHash};{authorization.Token}";
            return (auth, claims.XboxUserId, claims.Gamertag);
        }

        // Separate the asynchronous SISU result from UI/state publication. A cache clear or logout
        // increments the generation, so a late result from an earlier session cannot log in again.
        internal async Task<bool> AwaitAndPublishAuthAsync(
            Task<(string Xauth, string Xuid, string? Gamertag)> pending, int generation)
        {
            var (xauth, xuid, gamertag) = await pending;
            bool published = false;
            void Publish()
            {
                lock (_authStateLock)
                {
                    if (!IsCurrentAuthGeneration(generation) ||
                        string.IsNullOrWhiteSpace(xauth) || string.IsNullOrWhiteSpace(xuid))
                        return;
                    XAUTH = xauth;
                    XUIDOnly = xuid;
                    IsLoggedIn = true;
                    XAUTHTested = true;
                    InitComplete = true;
                    GamerTag = Settings.PrivacyMode ? "Gamertag: Hidden" : $"Gamertag: {gamertag ?? "Unknown"}";
                    Xuid = Settings.PrivacyMode ? "XUID: Hidden" : $"XUID: {xuid}";
                    published = true;
                }
            }
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                Publish();
            else
                dispatcher.Invoke(Publish);
            return published;
        }

        // A successful OAuth refresh and a successful Xbox SISU exchange are different results.
        // If SISU fails, keep the restored OAuth response so startup does not open an
        // interactive login prompt for a still-valid saved Microsoft session.
        internal static async Task<MicrosoftOAuthResponse> RestoreOAuthSessionAsync(
            Func<Task<MicrosoftOAuthResponse>> restore,
            Func<MicrosoftOAuthResponse, Task<bool>> establishXbox)
        {
            var response = await restore();
            await establishXbox(response);
            return response;
        }

        private async Task<MicrosoftOAuthResponse?> TryRestoreSessionAsync(int generation)
        {
            if (!File.Exists(AuthFilePath))
                return null;

            MicrosoftOAuthResponse? response;
            try
            {
                response = readSession();
            }
            catch
            {
                DeleteAuthFile();
                _snackbarService.Show("Session invalid", "Saved session could not be read. Please log in again.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return null;
            }

            if (response == null || !response.Validate() || string.IsNullOrEmpty(response.RefreshToken))
            {
                DeleteAuthFile();
                if (response != null && !response.Validate())
                    _snackbarService.Show("Session expired", "Your saved session has expired. Please log in again.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return null;
            }

            // Silent restore can transiently fail right after app start (Wi-Fi/stack not up yet,
            // MS auth hiccup). Retry a few times before falling back to the interactive flow --
            // one unlucky request used to kill auto-login for the whole session.
            Exception? lastError = null;
            for (int attempt = 1; attempt <= SilentRestoreAttempts; attempt++)
            {
                if (!IsCurrentAuthGeneration(generation))
                    return null;
                try
                {
                    string refreshToken = response.RefreshToken!;
                    response = await RestoreOAuthSessionAsync(
                        () => oauth.AuthenticateSilently(refreshToken),
                        fresh => IsCurrentAuthGeneration(generation)
                            ? CompleteLoginAsync(fresh, generation, "Logged in with previous session")
                            : Task.FromResult(false));
                    return IsCurrentAuthGeneration(generation) ? response : null;
                }
                catch (Exception ex)
                {
                    lastError = ex;
                    DiagLog.Write($"[OAUTH] silent restore attempt {attempt}/{SilentRestoreAttempts} failed: {ex.GetType().Name}: {ex.Message}");
                    if (attempt < SilentRestoreAttempts)
                        await Task.Delay(SilentRestoreRetryDelay);
                }
            }
            if (IsCurrentAuthGeneration(generation))
                ShowSnackbar("Session invalid", $"Could not restore the saved session ({lastError?.GetType().Name}). Please log in again.",
                    ControlAppearance.Danger, SymbolRegular.ErrorCircle24);
            return null;
        }

        private async Task<MicrosoftOAuthResponse?> TryInteractiveLoginAsync(int generation)
        {
            try
            {
                var response = await oauth.AuthenticateInteractively();
                if (!IsCurrentAuthGeneration(generation))
                    return null;
                return await CompleteLoginAsync(response, generation, "Logged in") ? response : null;
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[OAUTH] interactive login failed/cancelled: {ex.GetType().Name}");
                if (IsCurrentAuthGeneration(generation))
                {
                    ShowSnackbar("Authentication failed", "Please retry Xbox sign-in.",
                        ControlAppearance.Danger, SymbolRegular.ErrorCircle24);
                    Settings.OAuthLogin = false;
                    PersistEventsToken();
                }
                return null;
            }
        }

        private void ClearProfileState()
        {
            IsLoggedIn = false;
            XAUTHTested = false;
            GrabbedProfile = false;
            XAUTH = "";
            XUIDOnly = "";
            GamerTag = "Gamertag: Unknown   ";
            Xuid = "XUID: Unknown";
            GamerPic = "pack://application:,,,/Assets/cirno.png";
            GamerScore = "Gamerscore: Unknown";
            ProfileRep = "Reputation: Unknown";
            AccountTier = "Tier: Unknown";
            CurrentlyPlaying = "Currently Playing: Unknown";
            ActiveDevice = "Active Device: Unknown";
            IsVerified = "Verified: Unknown";
            Location = "Location: Unknown";
            Tenure = "Tenure: Unknown";
            Following = "Following: Unknown";
            Followers = "Followers: Unknown";
            Gamepass = "Gamepass: Unknown";
            Bio = "Bio: Unknown";
            Watermarks.Clear();
            AchievementsViewModel.EventsToken = null;
            _eventsTokenObtainedAt = DateTime.MinValue;
            _eventsUserHash = null;
            XauthWorker_ProgressChanged(null, null);
        }

        private static readonly byte[] AuthFileMagic = Encoding.ASCII.GetBytes("XAU1");
        private static readonly byte[] AuthDpapiEntropy = Encoding.UTF8.GetBytes("XAU-Auth-v1");

        private MicrosoftOAuthResponse readSession()
        {
            var raw = File.ReadAllBytes(AuthFilePath);
            string json;
            if (raw.Length >= AuthFileMagic.Length && raw.AsSpan(0, AuthFileMagic.Length).SequenceEqual(AuthFileMagic))
            {
                var encrypted = raw.AsSpan(AuthFileMagic.Length).ToArray();
                var plain = ProtectedData.Unprotect(encrypted, AuthDpapiEntropy, DataProtectionScope.CurrentUser);
                json = Encoding.UTF8.GetString(plain);
            }
            else
            {
                json = Encoding.UTF8.GetString(raw);
            }
            var response = JsonConvert.DeserializeObject<MicrosoftOAuthResponse>(json);
            return response;
        }

        private void writeSession(MicrosoftOAuthResponse response)
        {
            var json = JsonConvert.SerializeObject(response);
            var plain = Encoding.UTF8.GetBytes(json);
            var encrypted = ProtectedData.Protect(plain, AuthDpapiEntropy, DataProtectionScope.CurrentUser);
            using (var fs = new FileStream(AuthFilePath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                fs.Write(AuthFileMagic, 0, AuthFileMagic.Length);
                fs.Write(encrypted, 0, encrypted.Length);
            }
        }

        #endregion
        #region Profile
        /// <summary>
        /// Decides whether a profile fetch is allowed to start. Prevents the ~1s XauthWorker
        /// progress ticks (and the login/refresh paths) from re-invoking the async-void
        /// GrabProfile while an earlier fetch is still awaiting its network calls — which used
        /// to stack a burst of "Profile information grabbed" snackbars.
        /// - Automatic callers (worker ticks / login): require a logged-in user that has not
        ///   already been grabbed, and require no fetch currently running.
        /// - Manual caller (Refresh Profile button, force=true): allow a re-grab even when
        ///   already grabbed, but still do not stack while a fetch is in-flight.
        /// </summary>
        public static bool ShouldStartProfileGrab(bool isLoggedIn, bool alreadyGrabbed, bool inFlight, bool force = false)
        {
            if (inFlight)
                return false;
            if (!isLoggedIn)
                return false;
            return force || !alreadyGrabbed;
        }

        private async void GrabProfile(bool force = false)
        {
            // The guard runs synchronously before the first await, so the ~1s progress ticks
            // delivered on the dispatcher can never slip a second concurrent fetch in while the
            // first is still pending. The in-flight flag is cleared in the finally block below.
            if (!ShouldStartProfileGrab(IsLoggedIn, GrabbedProfile, _grabProfileInFlight, force))
                return;

            _grabProfileInFlight = true;
            try
            {
                var profileResponse = await _xboxRestAPI.Value.GetProfileAsync(XUIDOnly);

                if (profileResponse?.People?.Any() != true)
                {
                    _snackbarService.Show("Error", "Failed to grab profile information.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                var person = profileResponse.People.FirstOrDefault();
                if (Settings.PrivacyMode)
                {
                    // Display hidden profile details for privacy mode
                    GamerTag = "Gamertag: Hidden";
                    Xuid = "XUID: Hidden";
                    GamerPic = "pack://application:,,,/Assets/cirno.png";
                    GamerScore = "Gamerscore: Hidden";
                    ProfileRep = "Reputation: Hidden";
                    AccountTier = "Tier: Hidden";
                    CurrentlyPlaying = "Currently Playing: Hidden";
                    ActiveDevice = "Active Device: Hidden";
                    IsVerified = "Verified: Hidden";
                    Location = "Location: Hidden";
                    Tenure = "Tenure: Hidden";
                    Following = "Following: Hidden";
                    Followers = "Followers: Hidden";
                    Gamepass = "Gamepass: Hidden";
                    Bio = "Bio: Hidden";
                }
                else
                {
                    // Populate user profile details
                    GamerTag = $"Gamertag: {person?.Gamertag ?? "Unknown"}";
                    Xuid = $"XUID: {person?.Xuid ?? "Unknown"}";
                    GamerPic = (person?.DisplayPicRaw?.Replace("&mode=Padding", "")) ?? "pack://application:,,,/Assets/default.png";
                    GamerScore = $"Gamerscore: {person?.GamerScore ?? "Unknown"}";
                    ProfileRep = $"Reputation: {person?.XboxOneRep ?? "Unknown"}";
                    AccountTier = $"Tier: {person?.Detail?.AccountTier ?? "Unknown"}";

                    // Currently playing information
                    var presence = person?.PresenceDetails?.FirstOrDefault();
                    if (presence?.TitleId == null)
                    {
                        CurrentlyPlaying = "Currently Playing: Unknown (No Presence)";
                    }
                    else
                    {
                        var gameTitle = await _xboxRestAPI.Value.GetGameTitleAsync(XUIDOnly, presence.TitleId);
                        CurrentlyPlaying = gameTitle?.Titles?.FirstOrDefault()?.Name ?? $"Currently Playing: Unknown ({presence.TitleId})";
                    }

                    // Retrieve Gamepass Membership Information
                    try
                    {
                        var gpuResponse = await _xboxRestAPI.Value.GetGamepassMembershipAsync(XUIDOnly);
                        Gamepass = $"Gamepass: {gpuResponse?.GamepassMembership ?? gpuResponse?.Data?.GamepassMembership ?? "Unknown"}";
                    }
                    catch
                    {
                        Gamepass = "Gamepass: Unknown";
                    }

                    // Active Device Information
                    ActiveDevice = $"Active Device: {presence?.Device ?? "Unknown"}";

                    // Detailed profile information
                    if (person?.Detail != null)
                    {
                        IsVerified = $"Verified: {person.Detail.IsVerified}";
                        Location = $"Location: {person.Detail.Location ?? "Unknown"}";
                        Tenure = $"Tenure: {person.Detail.Tenure ?? "Unknown"}";
                        Following = $"Following: {person.Detail.FollowingCount}";
                        Followers = $"Followers: {person.Detail.FollowerCount}";
                        Bio = $"Bio: {person.Detail.Bio ?? "No Bio"}";

                        // Handle Watermarks
                        Watermarks.Clear();

                        // Parse Tenure Badge
                        if (int.TryParse(person.Detail.Tenure, out int tenureInt))
                        {
                            string tenureBadge = tenureInt.ToString("D2");
                            Watermarks.Add(new ImageItem { ImageUrl = $@"{BasicXboxAPIUris.WatermarksUrl}tenure/{tenureBadge}.png" });
                        }
                        else
                        {
                            Console.WriteLine("The tenure string is not a valid integer.");
                        }

                        // Add Launch Watermarks
                        if (person.Detail.Watermarks != null)
                        {
                            foreach (var watermark in person.Detail.Watermarks)
                            {
                                Watermarks.Add(new ImageItem { ImageUrl = $@"{BasicXboxAPIUris.WatermarksUrl}launch/{watermark.ToLower()}.png" });
                            }
                        }
                    }
                }

                GrabbedProfile = true;
                _snackbarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsLoggedIn = false;
                XAUTHTested = true;
                _snackbarService.Show("401 Unauthorized", "Session expired -- attempting to re-authenticate...", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                await TryRecoverAuthAsync();
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", "Failed to grab profile information. " + ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
            finally
            {
                _grabProfileInFlight = false;
            }
        }
        #endregion

        #region Settings

        public static XAUSettings Settings = new();

        private void LoadSettings()
        {
            // A corrupt/truncated settings.json (or a lock from a parallel write) used to crash
            // startup right here. Degrade to defaults instead; the user can still save over it.
            string? settingsJson;
            XAUSettings? settings;
            try
            {
                settingsJson = File.ReadAllText(SettingsFilePath);
                settings = JsonConvert.DeserializeObject<XAUSettings>(settingsJson);
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[SETTINGS] failed to read/deserialise {SettingsFilePath}: {ex.GetType().Name}: {ex.Message} -- continuing with in-memory defaults.");
                _snackbarService.Show(
                    "Settings load failed",
                    "settings.json could not be read; defaults are in use. Saving settings will overwrite the bad file.",
                    ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
                return;
            }
            if (settings == null)
            {
                _snackbarService.Show(
                    "Error",
                    "Couldn't load settings.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24)
                );
                return;
            }

            Settings.SettingsVersion = settings.SettingsVersion;
            Settings.ToolVersion = settings.ToolVersion;
            Settings.UnlockAllEnabled = settings.UnlockAllEnabled;
            Settings.AutoSpooferEnabled = settings.AutoSpooferEnabled;
            Settings.AutoLaunchXboxAppEnabled = settings.AutoLaunchXboxAppEnabled;
            Settings.LaunchHidden = settings.LaunchHidden;
            Settings.FakeSignatureEnabled = settings.FakeSignatureEnabled;
            Settings.RegionOverride = settings.RegionOverride;
            Settings.UseAcrylic = settings.UseAcrylic;
            Settings.PrivacyMode = settings.PrivacyMode;
            Settings.OAuthLogin = settings.OAuthLogin;
            Settings.EnableDiagnosticsLog = settings.EnableDiagnosticsLog;
            DiagLog.Enabled = Settings.EnableDiagnosticsLog;
            Settings.XauthScanReadLength = settings.XauthScanReadLength;
            ScanReadLength = NormalizeScanReadLength(Settings.XauthScanReadLength);
            Settings.AutoGrabEventsToken = settings.AutoGrabEventsToken;
            Settings.CachedEventsToken = settings.CachedEventsToken;
            Settings.EventsTokenObtainedAt = settings.EventsTokenObtainedAt;
            Settings.EventsUserHash = settings.EventsUserHash;
            _eventsUserHash = settings.EventsUserHash;

            // Restore cached events token if it's still fresh
            if (!string.IsNullOrEmpty(settings.CachedEventsToken) && settings.EventsTokenObtainedAt.HasValue)
            {
                var age = DateTime.UtcNow - settings.EventsTokenObtainedAt.Value;
                if (age < EventsTokenMaxAge)
                {
                    AchievementsViewModel.EventsToken = settings.CachedEventsToken;
                    _eventsTokenObtainedAt = settings.EventsTokenObtainedAt.Value;
                    EventsLog($"Restored cached events token (age: {age.TotalHours:F1}h)");
                }
                else
                {
                    EventsLog($"Cached events token expired (age: {age.TotalHours:F1}h), will re-grab");
                }
            }
        }

        #endregion
    }
}
