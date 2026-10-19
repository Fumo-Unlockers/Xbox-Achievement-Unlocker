using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using XAU.Util.Etw;
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
        public static string ToolVersion = "EmptyDevToolVersion";
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

        private Lazy<XboxRestAPI> _xboxRestAPI;
        private readonly Lazy<GithubRestApi> _gitHubRestAPI = new Lazy<GithubRestApi>();

        public static int SpoofingStatus = 0; //0 = NotSpoofing, 1 = Spoofing, 2 = AutoSpoofing
        public static string SpoofedTitleID = "0";
        public static string AutoSpoofedTitleID = "0";

        //SnackBar
        public HomeViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;

            // Assume XAUTH and System Language are set by the time this is actually instantiated
            ResetXboxRestApiClient();
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        private const string XAuthScanPattern = "58 42 4C 33 2E 30 20 78 3D";
        private const int MinimumXauthCandidateLength = 31;
        private static readonly TimeSpan RejectedXauthCandidateQuarantine = TimeSpan.FromSeconds(60);
        private int _xauthScanInFlight;
        private int _xauthValidationInFlight;
        private int _xauthNoStableCandidateFailures;
        private int _xauthValidationFailures;
        private DateTime _nextXauthScanUtc = DateTime.MinValue;
        private DateTime _nextXauthValidationRetryUtc = DateTime.MinValue;
        private string _lastAcceptedXauthCandidateHash = "";
        private string _rejectedXauthCandidateHash = "";
        private DateTime _rejectedXauthCandidateUntilUtc = DateTime.MinValue;
        private int _lastSelectedXboxPid;
        private int _attachedXboxPid;

        private sealed class XauthCandidateRank
        {
            public string Candidate { get; set; } = "";
            public int Frequency { get; set; }
            public int AddressCount { get; set; }
            public string Hash { get; set; } = "";
            public bool IsQuarantined { get; set; }
            public int Score { get; set; }
        }

        [RelayCommand]
        private void RefreshProfile()
        {
            GrabProfile();
        }

        Mem m = new Mem();
        public BackgroundWorker XauthWorker = new BackgroundWorker();
        public BackgroundWorker EventsTokenWorker = new BackgroundWorker();
        bool IsAttached = false;
        bool GrabbedProfile = false;
        bool eventsTokenFound = false;
        public static bool XAUTHTested = false;
        public static string XAUTH = "";
        public static string XUIDOnly;
        public static bool InitComplete = false;
        private bool _isInitialized = false;
        string SettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
        string EventsMetaFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "Events", "meta.json");
        string AuthFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "auth.json");
        public CodeFlowAuthenticator oauth;
        public XboxAuthClient xboxAuthClient;
        public XboxSignedClient xboxSignedClient;

        public void ResetXboxRestApiClient()
        {
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
        }

        private void ResetXauthScanBackoff()
        {
            if (_xauthNoStableCandidateFailures == 0 && _nextXauthScanUtc == DateTime.MinValue)
                return;

            _xauthNoStableCandidateFailures = 0;
            _nextXauthScanUtc = DateTime.MinValue;
        }

        private void TrackNoStableXauthScan()
        {
            _xauthNoStableCandidateFailures++;

            var delay = _xauthNoStableCandidateFailures switch
            {
                < 3 => TimeSpan.Zero,
                3 => TimeSpan.FromSeconds(5),
                4 => TimeSpan.FromSeconds(10),
                5 => TimeSpan.FromSeconds(20),
                _ => TimeSpan.FromSeconds(30)
            };

            if (delay > TimeSpan.Zero)
                _nextXauthScanUtc = DateTime.UtcNow + delay;
        }

        private bool IsXauthScanBackoffActive()
        {
            return _nextXauthScanUtc > DateTime.UtcNow;
        }

        private void ResetXauthValidationFailureBackoff()
        {
            if (_xauthValidationFailures == 0 && _nextXauthValidationRetryUtc == DateTime.MinValue)
                return;

            _xauthValidationFailures = 0;
            _nextXauthValidationRetryUtc = DateTime.MinValue;
        }

        private void TrackXauthValidationFailureBackoff()
        {
            _xauthValidationFailures++;

            var delay = _xauthValidationFailures switch
            {
                <= 2 => TimeSpan.FromSeconds(2),
                3 => TimeSpan.FromSeconds(10),
                4 => TimeSpan.FromSeconds(30),
                _ => TimeSpan.FromSeconds(60)
            };

            _nextXauthValidationRetryUtc = DateTime.UtcNow + delay;
        }

        private bool IsXauthValidationFailureBackoffActive()
        {
            return _nextXauthValidationRetryUtc > DateTime.UtcNow;
        }

        private static bool IsValidXauthCandidate(string candidate)
        {
            return !string.IsNullOrWhiteSpace(candidate) && candidate.Length >= MinimumXauthCandidateLength;
        }

        private static string HashXauthCandidate(string candidate)
        {
            return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(candidate)));
        }

        private void ResetRejectedXauthCandidateQuarantine()
        {
            if (string.IsNullOrEmpty(_rejectedXauthCandidateHash) && _rejectedXauthCandidateUntilUtc == DateTime.MinValue)
                return;

            _rejectedXauthCandidateHash = "";
            _rejectedXauthCandidateUntilUtc = DateTime.MinValue;
        }

        private void ResetXauthFailureState()
        {
            ResetXauthValidationFailureBackoff();
            ResetRejectedXauthCandidateQuarantine();
        }

        private void ResetXauthScanState()
        {
            ResetXauthScanBackoff();
            ResetXauthFailureState();
        }

        private void QuarantineRejectedXauthCandidate(string candidate)
        {
            if (string.IsNullOrWhiteSpace(candidate))
                return;

            var candidateHash = HashXauthCandidate(candidate);
            _rejectedXauthCandidateHash = candidateHash;
            _rejectedXauthCandidateUntilUtc = DateTime.UtcNow + RejectedXauthCandidateQuarantine;
        }

        private static int GetMaxRepeatedCharRun(string value)
        {
            if (string.IsNullOrEmpty(value))
                return 0;

            var maxRun = 1;
            var currentRun = 1;
            for (var i = 1; i < value.Length; i++)
            {
                if (value[i] == value[i - 1])
                {
                    currentRun++;
                    if (currentRun > maxRun)
                        maxRun = currentRun;
                }
                else
                {
                    currentRun = 1;
                }
            }

            return maxRun;
        }

        private XauthCandidateRank BuildXauthCandidateRank(string candidate, int frequency, int addressCount, int medianLength)
        {
            const string xauthPrefix = "XBL3.0 x=";
            var candidateHash = HashXauthCandidate(candidate);
            var isRecentlyRejected = !string.IsNullOrEmpty(_rejectedXauthCandidateHash)
                && string.Equals(candidateHash, _rejectedXauthCandidateHash, StringComparison.Ordinal);
            var isQuarantined = isRecentlyRejected && _rejectedXauthCandidateUntilUtc > DateTime.UtcNow;
            var prefixIndex = candidate.IndexOf(xauthPrefix, StringComparison.Ordinal);
            var semicolonIndex = candidate.IndexOf(';');
            var tokenLength = candidate.Length;
            var score = 0;

            score += prefixIndex == 0 ? 80 : -80;
            score += candidate.Count(ch => ch == ';') == 1 ? 40 : -60;
            score += semicolonIndex >= xauthPrefix.Length && semicolonIndex <= 128 ? 30 : -30;
            score += candidate.IndexOfAny(new[] { '\r', '\n' }) < 0 ? 30 : -80;
            score += tokenLength is >= 200 and <= 6000 ? 20 : -40;

            if (medianLength > 0)
            {
                var lengthDelta = Math.Abs(tokenLength - medianLength);
                score -= Math.Min(40, lengthDelta / 100);
            }

            score += addressCount * 3;
            score += frequency * 2;

            if (isRecentlyRejected)
                score -= 120;

            if (isQuarantined)
                score -= 100000;

            if (prefixIndex < 0 || prefixIndex != candidate.LastIndexOf(xauthPrefix, StringComparison.Ordinal))
                score -= 80;

            if (GetMaxRepeatedCharRun(candidate) >= 128)
                score -= 60;

            return new XauthCandidateRank
            {
                Candidate = candidate,
                Frequency = frequency,
                AddressCount = addressCount,
                Hash = candidateHash,
                IsQuarantined = isQuarantined,
                Score = score
            };
        }

        private static int GetMedianLength(IEnumerable<string> candidates)
        {
            var lengths = candidates.Select(candidate => candidate.Length).OrderBy(length => length).ToList();
            if (lengths.Count == 0)
                return 0;

            var middle = lengths.Count / 2;
            return lengths.Count % 2 == 1
                ? lengths[middle]
                : (lengths[middle - 1] + lengths[middle]) / 2;
        }

        private void ResetAttachStateAfterFailedValidation()
        {
            TrackXauthValidationFailureBackoff();
            IsLoggedIn = false;
            XAUTH = "";
            XUIDOnly = "";
            XAUTHTested = false;
            InitComplete = false;
            GrabbedProfile = false;
            LoggedIn = "Not Logged In";
            LoggedInColor = new SolidColorBrush(Colors.Red);
            ResetXboxRestApiClient();
            ResetXauthScanBackoff();
        }

        private void ClearAttachAfterProcessFailure()
        {
            IsAttached = false;
            _attachedXboxPid = 0;
            ResetXauthFailureState();
        }

        public async void OnNavigatedTo()
        {
            if (!_isInitialized)
                await InitializeViewModel();
        }
        public void OnNavigatedFrom() { }

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
            await CheckForToolUpdates();
            XauthWorker.DoWork += XauthWorker_DoWork;
            XauthWorker.ProgressChanged += XauthWorker_ProgressChanged;
            XauthWorker.RunWorkerCompleted += XauthWorker_RunWorkerCompleted;
            XauthWorker.WorkerReportsProgress = true;
            XauthWorker.RunWorkerAsync();
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
                    OAuthLogin();
                }

            }
            CheckForEventUpdates();
            CheckForXboxGamesDatabaseUpdate();
            LoadSettings();
            if (Settings.OAuthLogin)
                OAuthLogin();
            _isInitialized = true;
            if (Settings.AutoLaunchXboxAppEnabled && Process.GetProcessesByName(ProcessNames.XboxPcApp).Length == 0)
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
        public void XauthWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (!Settings.OAuthLogin)
            {
                int xboxAppPid = 0;
                try
                {
                    xboxAppPid = m.GetProcIdFromName(ProcessNames.XboxPcApp);
                    if (xboxAppPid != _lastSelectedXboxPid)
                    {
                        _attachedXboxPid = 0;
                        ResetXauthScanState();
                        _lastSelectedXboxPid = xboxAppPid;
                    }
                }
                catch (Exception)
                {
                    ClearAttachAfterProcessFailure();
                    Thread.Sleep(1000);
                    XauthWorker.ReportProgress(0);
                    continue;
                }

                if (xboxAppPid == 0)
                {
                    ClearAttachAfterProcessFailure();
                    Thread.Sleep(1000);
                }
                else if (IsAttached && _attachedXboxPid == xboxAppPid)
                {
                    // Already attached to this PID; keep the existing handle and avoid re-emitting attach diagnostics.
                }
                else if (!m.OpenProcess(xboxAppPid, out _))
                {
                    ClearAttachAfterProcessFailure();
                    Thread.Sleep(1000);
                }
                else
                {
                    _attachedXboxPid = xboxAppPid;
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
                Attached = $"Attached to xbox app ({m.GetProcIdFromName(ProcessNames.XboxPcApp).ToString()})";
                AttachedColor = new SolidColorBrush(Colors.Green);
                if (IsLoggedIn)
                {
                    if (!GrabbedProfile)
                        GrabProfile();
                    LoggedIn = "Logged In";
                    LoggedInColor = new SolidColorBrush(Colors.Green);
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
                    if (!XAUTHTested && XAUTH.Length > 0)
                    {
                        TestXAUTH();
                    }
                }
            }
            if (m.GetProcIdFromName(ProcessNames.XboxPcApp) == 0)
            {
                Attached = "Not Attached";
                AttachedColor = new SolidColorBrush(Colors.Red);
            }
        }
        public void XauthWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!XauthWorker.IsBusy)
                XauthWorker.RunWorkerAsync();
        }

        private bool ShouldSkipXauthScan()
        {
            return IsLoggedIn
                || InitComplete
                || Interlocked.CompareExchange(ref _xauthValidationInFlight, 0, 0) != 0
                || (!string.IsNullOrEmpty(XAUTH) && !XAUTHTested)
                || IsXauthValidationFailureBackoffActive()
                || IsXauthScanBackoffActive();
        }

        private bool TryBeginXauthScan()
        {
            if (ShouldSkipXauthScan())
                return false;

            if (Interlocked.CompareExchange(ref _xauthScanInFlight, 1, 0) != 0)
                return false;

            if (!ShouldSkipXauthScan())
                return true;

            Interlocked.Exchange(ref _xauthScanInFlight, 0);
            return false;
        }

        private async void GetXAUTH()
        {
            if (!TryBeginXauthScan())
                return;

            try
            {
                var xauthScanAddresses = (await m.AoBScan(XAuthScanPattern, true)).ToList();
                Dictionary<string, int> frequency = new Dictionary<string, int>();
                Dictionary<string, HashSet<long>> candidateAddresses = new Dictionary<string, HashSet<long>>();

                foreach (var address in xauthScanAddresses)
                {
                    var candidate = m.ReadString(address.ToString("X"), length: 10000) ?? string.Empty;

                    frequency.TryGetValue(candidate, out var count);
                    frequency[candidate] = count + 1;

                    if (!candidateAddresses.TryGetValue(candidate, out var addresses))
                        candidateAddresses[candidate] = addresses = new HashSet<long>();

                    addresses.Add(address);
                }

                if (xauthScanAddresses.Count == 0)
                {
                    TrackNoStableXauthScan();
                    return;
                }

                var stableCandidates = frequency
                    .Where(pair => pair.Value > 3)
                    .ToList();

                if (stableCandidates.Count == 0)
                {
                    TrackNoStableXauthScan();
                    return;
                }

                var validStableCandidates = stableCandidates
                    .Select(pair => new KeyValuePair<string, int>(pair.Key ?? string.Empty, pair.Value))
                    .Where(pair => IsValidXauthCandidate(pair.Key))
                    .ToList();

                if (validStableCandidates.Count == 0)
                    return;

                var medianLength = GetMedianLength(validStableCandidates.Select(pair => pair.Key));
                var selectedCandidate = validStableCandidates
                    .Select(pair =>
                    {
                        var candidateAddressCount = candidateAddresses.TryGetValue(pair.Key, out var addresses)
                            ? addresses.Count
                            : 0;
                        return BuildXauthCandidateRank(pair.Key, pair.Value, candidateAddressCount, medianLength);
                    })
                    .OrderBy(candidate => candidate.IsQuarantined ? 1 : 0)
                    .ThenByDescending(candidate => candidate.Score)
                    .ThenByDescending(candidate => candidate.AddressCount)
                    .ThenByDescending(candidate => candidate.Frequency)
                    .First();

                if (selectedCandidate.IsQuarantined)
                    return;

                if (!string.Equals(selectedCandidate.Hash, _lastAcceptedXauthCandidateHash, StringComparison.Ordinal))
                    ResetXauthFailureState();

                _lastAcceptedXauthCandidateHash = selectedCandidate.Hash;
                XAUTH = selectedCandidate.Candidate;
                XAUTHTested = false;
                ResetXauthScanBackoff();
                ResetXboxRestApiClient();
            }
            catch
            {
            }
            finally
            {
                Interlocked.Exchange(ref _xauthScanInFlight, 0);
            }
        }
        private async void TestXAUTH()
        {
            if (Interlocked.Exchange(ref _xauthValidationInFlight, 1) == 1)
                return;

            var candidateUnderValidation = XAUTH;
            try
            {
                if (string.IsNullOrWhiteSpace(candidateUnderValidation))
                    return;

                var response = await _xboxRestAPI.Value.GetBasicProfileAsync();
                if (Settings.PrivacyMode)
                {
                    GamerTag = $"Gamertag: Hidden";
                    Xuid = $"XUID: Hidden";
                }
                else
                {
                    GamerTag = $"Gamertag: {response.ProfileUsers[0].Settings[0].Value}";
                    Xuid = $"XUID: {response.ProfileUsers[0].Id}";
                }

                XUIDOnly = response.ProfileUsers[0].Id;
                IsLoggedIn = true;
                XAUTHTested = true;
                InitComplete = true;
                ResetXauthScanState();

                // Start the events token worker to periodically check/refresh the token
                if (Settings.AutoGrabEventsToken && !EventsTokenWorker.IsBusy)
                    EventsTokenWorker.RunWorkerAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                QuarantineRejectedXauthCandidate(candidateUnderValidation);
                ResetAttachStateAfterFailedValidation();
            }
            catch (HttpRequestException)
            {
                ResetAttachStateAfterFailedValidation();
            }
            catch (Exception)
            {
                ResetAttachStateAfterFailedValidation();
            }
            finally
            {
                Interlocked.Exchange(ref _xauthValidationInFlight, 0);
            }
        }
        #endregion

        #region EventsToken
        private bool solitaireLaunchedByUs = false;

        private static readonly TimeSpan EventsTokenCheckInterval = TimeSpan.FromMinutes(10);
        private static readonly TimeSpan EventsTokenMaxAge = TimeSpan.FromHours(23);

        private static DateTime _eventsTokenObtainedAt = DateTime.MinValue;
        private static string _eventsUserHash = null;

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


        public void PersistEventsToken()
        {
            try
            {
                Settings.CachedEventsToken = AchievementsViewModel.EventsToken;
                Settings.EventsTokenObtainedAt = _eventsTokenObtainedAt;
                Settings.EventsUserHash = _eventsUserHash;
                var json = JsonConvert.SerializeObject(Settings);
                File.WriteAllText(SettingsFilePath, json);
            }
            catch { }
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
                var tokenAge = DateTime.UtcNow - _eventsTokenObtainedAt;
                bool isExpired = !isEmpty && isValid && tokenAge > EventsTokenMaxAge;

                EventsLog($"Check: empty={isEmpty}, valid={isValid}, age={tokenAge.TotalMinutes:F0}m, expired={isExpired}");

                if (isEmpty || !isValid || isExpired)
                {
                    if (isExpired)
                        EventsLog($"Token expired (age: {tokenAge.TotalMinutes:F0}m > {EventsTokenMaxAge.TotalMinutes:F0}m), refreshing...");
                    else
                        EventsLog("Token missing/invalid, refreshing...");

                    // Keep capturing until we get a token or settings change
                    while (Settings.AutoGrabEventsToken && IsLoggedIn)
                    {
                        var token = EtwTokenCapture.Capture(20);
                        if (!string.IsNullOrEmpty(token))
                        {
                            AchievementsViewModel.EventsToken = token;
                            _eventsTokenObtainedAt = DateTime.UtcNow;
                            PersistEventsToken();
                            EventsLog("ETW capture success.");
                            break;
                        }
                        EventsLog("ETW capture found no token, retrying in 5s...");
                        Thread.Sleep(5000);
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
        private void GrabEventsTokenFromSolitaire()
        {
            bool alreadyRunning = Process.GetProcessesByName(ProcessNames.Solitaire).Length > 0;
            EventsLog($"Solitaire already running: {alreadyRunning}");

            // Start ETW capture FIRST — the token is sent in the initial telemetry burst
            // when Solitaire contacts Xbox Live, which happens within seconds of launch.
            EventsLog("Starting ETW before Solitaire launch...");
            EtwTokenCapture.Cleanup();
            string method = EtwTokenCapture.Start();
            if (method == null)
            {
                EventsLog("Failed to start ETW trace (not running as admin?)");
                return;
            }
            EventsLog($"ETW started via {method}");

            if (!alreadyRunning)
            {
                try
                {
                    EventsLog("Launching Solitaire...");
                    var p = new Process();
                    p.StartInfo = new ProcessStartInfo
                    {
                        UseShellExecute = true,
                        FileName = AppLaunchUris.Solitaire
                    };
                    p.Start();
                    solitaireLaunchedByUs = true;
                }
                catch (Exception ex)
                {
                    EventsLog($"Failed to launch Solitaire: {ex.Message}");
                    EtwTokenCapture.Stop(method);
                    EtwTokenCapture.CleanupFiles();
                    return;
                }

                // Wait for the process to appear
                for (int i = 0; i < 15; i++)
                {
                    Thread.Sleep(1000);
                    if (Process.GetProcessesByName(ProcessNames.Solitaire).Length > 0)
                    {
                        EventsLog($"Solitaire process appeared after {i + 1}s");
                        break;
                    }
                }

                if (Process.GetProcessesByName(ProcessNames.Solitaire).Length == 0)
                {
                    EventsLog("Solitaire never appeared after 15s");
                    solitaireLaunchedByUs = false;
                    EtwTokenCapture.Stop(method);
                    EtwTokenCapture.CleanupFiles();
                    return;
                }
            }

            // Wait for Xbox Live init + initial telemetry burst (token is sent here)
            EventsLog("Waiting 25s for Xbox Live init + telemetry events...");
            Thread.Sleep(25000);

            // Stop first capture and try to extract
            EtwTokenCapture.Stop(method);
            Thread.Sleep(2000);

            string token = EtwTokenCapture.ExtractTokens();
            EtwTokenCapture.CleanupFiles();

            if (!string.IsNullOrEmpty(token))
            {
                EventsLog($"ETW capture success on initial capture, len={token.Length}");
                AchievementsViewModel.EventsToken = token;
                _eventsTokenObtainedAt = DateTime.UtcNow;
                eventsTokenFound = true;
            }
            else
            {
                // Loop: keep capturing while Solitaire is running
                EventsLog("Initial capture found nothing, entering continuous scan loop...");
                int attempt = 0;
                while (!eventsTokenFound && Process.GetProcessesByName(ProcessNames.Solitaire).Length > 0)
                {
                    attempt++;
                    EventsLog($"Capture attempt {attempt}...");

                    token = EtwTokenCapture.Capture(20);
                    if (!string.IsNullOrEmpty(token))
                    {
                        EventsLog($"ETW capture success on attempt {attempt}, len={token.Length}");
                        AchievementsViewModel.EventsToken = token;
                        _eventsTokenObtainedAt = DateTime.UtcNow;
                        eventsTokenFound = true;
                        break;
                    }

                    // Brief pause before next capture
                    Thread.Sleep(3000);
                }

                if (!eventsTokenFound)
                    EventsLog("Solitaire exited before token was found");
            }

            // Close Solitaire if we launched it
            if (solitaireLaunchedByUs)
            {
                try
                {
                    foreach (var proc in Process.GetProcessesByName(ProcessNames.Solitaire))
                        proc.Kill();
                }
                catch { }
                solitaireLaunchedByUs = false;
            }
        }

        /// <summary>
        /// Manually triggers a scan (from the "Manually Refresh Token" button).
        /// Works regardless of the auto-grab setting.
        /// </summary>
        public bool ManualScanRunning { get; private set; }

        public void ScanForEventsTokenManual()
        {
            eventsTokenFound = false;
            AchievementsViewModel.EventsToken = null;
            _eventsTokenObtainedAt = DateTime.MinValue;
            ManualScanRunning = true;

            System.Threading.Tasks.Task.Run(() =>
            {
                try
                {
                    GrabEventsTokenFromSolitaire();
                    if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
                    {
                        _eventsTokenObtainedAt = DateTime.UtcNow;
                        PersistEventsToken();
                    }
                }
                finally
                {
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

        [RelayCommand]
        private async void OAuthLogin()
        {
            //init oauth stuff
            var OAuthhttpClient = new HttpClient();
            var apiClient = new CodeFlowLiveApiClient(XboxGameTitles.XboxAppPC, XboxAuthConstants.XboxScope, OAuthhttpClient);
            xboxAuthClient = new XboxAuthClient(OAuthhttpClient);
            xboxSignedClient = new XboxSignedClient(OAuthhttpClient);
            oauth = new CodeFlowBuilder(apiClient)
                .WithUIParent(this)
                .Build();
            if (LoginText == "Logout")
            {
                oauth.Signout();
                try { File.Delete(AuthFilePath); } catch { }
                ClearProfileState();
                LoginText = "Login";
                return;
            }
            Settings.OAuthLogin = true;

            // Use saved session if valid; otherwise interactive login
            MicrosoftOAuthResponse? response = await TryRestoreSessionAsync();
            if (response == null)
                response = await TryInteractiveLoginAsync();
        }

        private void DeleteAuthFile()
        {
            try { File.Delete(AuthFilePath); } catch { }
        }

        private void CompleteLogin(MicrosoftOAuthResponse response, string? successMessage = null)
        {
            writeSession(response);
            if (!string.IsNullOrEmpty(successMessage))
                _snackbarService.Show("Success", successMessage, ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            GenerateTokens(response);
        }

        private async Task<MicrosoftOAuthResponse?> TryRestoreSessionAsync()
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

            try
            {
                response = await oauth.AuthenticateSilently(response.RefreshToken!);
                CompleteLogin(response, "Logged in with previous session");
                return response;
            }
            catch
            {
                _snackbarService.Show("Session invalid", "You are required to log in again as the session has expired", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                ClearProfileState();
                return await TryInteractiveLoginAsync();
            }
        }

        private async Task<MicrosoftOAuthResponse?> TryInteractiveLoginAsync()
        {
            try
            {
                var response = await oauth.AuthenticateInteractively();
                CompleteLogin(response, "Logged in");
                return response;
            }
            catch
            {
                _snackbarService.Show("Error", "Failed to authenticate", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return null;
            }
        }

        private async void GenerateTokens(MicrosoftOAuthResponse response)
        {
            var deviceToken = await xboxSignedClient.RequestDeviceToken(XboxDeviceTypes.Win32, "0.0.0");
            var sisuResult = await xboxSignedClient.SisuAuth(new XboxSisuAuthRequest
            {
                AccessToken = response.AccessToken,
                ClientId = XboxGameTitles.XboxAppPC,
                DeviceToken = deviceToken.Token,
                RelyingParty = XboxAuthConstants.XboxLiveRelyingParty,
            });
            try
            {
                XAUTH = $"XBL3.0 x={sisuResult.AuthorizationToken.XuiClaims.UserHash};{sisuResult.AuthorizationToken.Token}";
                ResetXboxRestApiClient();
                var xui = sisuResult.AuthorizationToken.XuiClaims;
                XUIDOnly = xui?.XboxUserId ?? "";
                if (!string.IsNullOrEmpty(XUIDOnly))
                {
                    IsLoggedIn = true;
                    XAUTHTested = true;
                    InitComplete = true;
                    if (Settings.PrivacyMode)
                    {
                        GamerTag = "Gamertag: Hidden";
                        Xuid = "XUID: Hidden";
                    }
                    else
                    {
                        GamerTag = $"Gamertag: {xui?.Gamertag ?? "Unknown"}";
                        Xuid = $"XUID: {XUIDOnly}";
                    }
                }
            }
            catch
            {
                _snackbarService.Show("Error", "Failed to generate XAUTH", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
            LoginText = "Logout";
            XauthWorker_ProgressChanged(null, null);
            if (IsLoggedIn && !GrabbedProfile)
                GrabProfile();

            // Start the events token worker to periodically check/refresh the token
            if (Settings.AutoGrabEventsToken && !EventsTokenWorker.IsBusy)
                EventsTokenWorker.RunWorkerAsync();
        }
        private void ClearProfileState()
        {
            IsLoggedIn = false;
            XAUTHTested = false;
            GrabbedProfile = false;
            XAUTH = "";
            XUIDOnly = "";
            InitComplete = false;
            _attachedXboxPid = 0;
            ResetXboxRestApiClient();
            ResetXauthScanState();
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
        private async void GrabProfile()
        {
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
                _snackbarService.Show("401 Unauthorized", "Something went wrong. Retrying.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", "Failed to grab profile information. " + ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }
        #endregion

        #region Settings

        public static XAUSettings Settings = new();

        private void LoadSettings()
        {
            var settingsJson = File.ReadAllText(SettingsFilePath);
            var settings = JsonConvert.DeserializeObject<XAUSettings>(settingsJson);
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
