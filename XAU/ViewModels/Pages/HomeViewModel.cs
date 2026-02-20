using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Text;
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

        private readonly Lazy<XboxRestAPI> _xboxRestAPI;
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
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        private const string XAuthScanPattern = "58 42 4C 33 2E 30 20 78 3D";

        // ETW network capture settings
        private static readonly string EtwSessionName = "XAU_EventsTokenCapture";
        private static readonly string EtwTempDir = Path.Combine(Path.GetTempPath(), "XAU_ETW");
        private static readonly string EtwEtlPath = Path.Combine(EtwTempDir, "capture.etl");

        // Regex patterns for extracting tokens from ETL binary data
        private static readonly Regex TicketHeaderRegex = new Regex(
            @"""(\d{5,12})""\s*=\s*""(x:XBL3\.0 x=[^""]{100,})""",
            RegexOptions.Compiled);
        private static readonly Regex BareTokenRegex = new Regex(
            @"x:XBL3\.0 x=[\w;+/=\-\.]{100,}",
            RegexOptions.Compiled);
        private static readonly Regex OneCollectorUrlRegex = new Regex(
            @"v20\.events\.data\.microsoft\.com|OneCollector",
            RegexOptions.Compiled);

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
                if (!m.OpenProcess((ProcessNames.XboxPcApp)))
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
        private async void GetXAUTH()
        {
            IEnumerable<long> XauthScanList = await m.AoBScan(XAuthScanPattern, true);
            string[] XauthStrings = new string[XauthScanList.Count()];
            var i = 0;
            foreach (var address in XauthScanList)
            {
                XauthStrings[i] = m.ReadString(address.ToString("X"), length: 10000);
                i++;
            }

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
                return;
            }

            string mostCommon = XauthStrings[0];
            int highestFrequency = 0;
            foreach (KeyValuePair<string, int> pair in frequency)
            {
                if (pair.Value > highestFrequency)
                {
                    mostCommon = pair.Key;
                    highestFrequency = pair.Value;
                }
            }

            if (highestFrequency > 3)
            {
                XAUTH = mostCommon;
                XAUTHTested = false;
            }
        }
        private async void TestXAUTH()
        {
            try
            {
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

                // Start the events token worker to periodically check/refresh the token
                if (Settings.AutoGrabEventsToken && !EventsTokenWorker.IsBusy)
                    EventsTokenWorker.RunWorkerAsync();
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    IsLoggedIn = false;
                    XAUTHTested = true;

                }
            }
        }
        #endregion

        #region EventsToken
        private bool solitaireLaunchedByUs = false;

        // How often the worker loop checks
        private static readonly TimeSpan EventsTokenCheckInterval = TimeSpan.FromMinutes(10);
        // How old a token can be before we proactively refresh it (~24h XSTS expiry, refresh early)
        private static readonly TimeSpan EventsTokenMaxAge = TimeSpan.FromHours(23);

        private static DateTime _eventsTokenObtainedAt = DateTime.MinValue;
        // The user hash for the events relying party - used to identify the correct token in memory
        private static string _eventsUserHash = null;

        private static readonly string EventsLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "events_debug.log");

        private static void EventsLog(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            Debug.WriteLine($"[EventsToken] {msg}");
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EventsLogPath)!);
                File.AppendAllText(EventsLogPath, line + Environment.NewLine);
            }
            catch { /* best-effort */ }
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
                        var token = CaptureEventsTokenViaEtw(20);
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
            EtwCleanup();
            string method = EtwStart();
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
                    EtwStop(method);
                    EtwCleanupFiles();
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
                    EtwStop(method);
                    EtwCleanupFiles();
                    return;
                }
            }

            // Wait for Xbox Live init + initial telemetry burst (token is sent here)
            EventsLog("Waiting 25s for Xbox Live init + telemetry events...");
            Thread.Sleep(25000);

            // Stop first capture and try to extract
            EtwStop(method);
            Thread.Sleep(2000);

            string token = EtwExtractTokens();
            EtwCleanupFiles();

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

                    token = CaptureEventsTokenViaEtw(20);
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

        #region ETW Token Capture

        // Events RP x5t — used to distinguish events tokens from XAUTH tokens.
        // This is the certificate thumbprint for events.xboxlive.com; it appears
        // in the decoded JWE header JSON as "x5t":"9wLGzMJDNz..."
        private const string EventsRpX5t = "9wLGzMJDNz";

        /// <summary>
        /// Checks whether a token was encrypted for the events RP by decoding
        /// the JWE header and verifying the x5t certificate thumbprint.
        /// Token format: "x:XBL3.0 x={hash};{JWE}" or "XBL3.0 x={hash};{JWE}"
        /// </summary>
        private static bool IsEventsRpToken(string token)
        {
            try
            {
                int semiIdx = token.IndexOf(';');
                if (semiIdx < 0) return false;

                string jwe = token.Substring(semiIdx + 1);
                int dotIdx = jwe.IndexOf('.');
                if (dotIdx <= 0) return false;

                string headerB64 = jwe.Substring(0, dotIdx);
                // Base64url → standard Base64
                string padded = headerB64.Replace('-', '+').Replace('_', '/');
                switch (padded.Length % 4)
                {
                    case 2: padded += "=="; break;
                    case 3: padded += "="; break;
                }

                string headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                return headerJson.Contains(EventsRpX5t);
            }
            catch
            {
                return false;
            }
        }

        private static (int exitCode, string stdout, string stderr) RunShellCommand(string fileName, string args, int timeoutMs = 30000)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(timeoutMs);
                return (proc.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }

        private static void EtwCleanup()
        {
            try { RunShellCommand("netsh", "trace stop", 15000); } catch { }
            try { RunShellCommand("logman", $"stop {EtwSessionName} -ets", 10000); } catch { }
        }

        private string EtwStart()
        {
            Directory.CreateDirectory(EtwTempDir);

            // Try netsh trace (captures most HTTP traffic including WinHTTP)
            var (code, stdout, stderr) = RunShellCommand("netsh",
                $"trace start scenario=InternetClient_dbg capture=no tracefile=\"{EtwEtlPath}\" maxsize=256 overwrite=yes report=disabled",
                15000);
            EventsLog($"netsh trace start: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "netsh";

            // Try logman with WinHttp provider
            (code, stdout, stderr) = RunShellCommand("logman",
                $"start {EtwSessionName} -p Microsoft-Windows-WinHttp 0xFFFFFFFF 0xFF -o \"{EtwEtlPath}\" -ets",
                10000);
            EventsLog($"logman WinHttp: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "logman-winhttp";

            // Try logman with WinINet provider
            (code, stdout, stderr) = RunShellCommand("logman",
                $"start {EtwSessionName} -p Microsoft-Windows-WinINet 0xFFFFFFFF 0xFF -o \"{EtwEtlPath}\" -ets",
                10000);
            EventsLog($"logman WinINet: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "logman-wininet";

            EventsLog("All ETW start methods failed");
            return null;
        }

        private void EtwStop(string method)
        {
            if (method == "netsh")
            {
                var (code, _, _) = RunShellCommand("netsh", "trace stop", 30000);
                EventsLog($"netsh trace stop: exit={code}");
            }
            else if (method != null)
            {
                var (code, _, _) = RunShellCommand("logman", $"stop {EtwSessionName} -ets", 15000);
                EventsLog($"logman stop: exit={code}");
            }
        }

        private string CaptureEventsTokenViaEtw(int captureSeconds)
        {
            EventsLog($"Starting ETW capture for {captureSeconds}s...");

            EtwCleanup();

            string method = EtwStart();
            if (method == null)
            {
                EventsLog("Failed to start ETW trace (not running as admin?)");
                return null;
            }
            EventsLog($"ETW started via {method}");

            Thread.Sleep(captureSeconds * 1000);

            EtwStop(method);

            // Give it a moment to flush
            Thread.Sleep(2000);

            string token = EtwExtractTokens();

            EtwCleanupFiles();

            return token;
        }

        private string EtwExtractTokens()
        {
            if (!File.Exists(EtwEtlPath))
            {
                EventsLog("ETL file not found");
                return null;
            }

            var fileSize = new FileInfo(EtwEtlPath).Length;
            EventsLog($"ETL file size: {fileSize / 1024}KB");

            const int chunkSize = 64 * 1024 * 1024; // 64MB
            const int overlap = 8 * 1024; // 8KB overlap
            var candidates = new List<(string token, int score)>();

            int totalXblHits = 0;

            using (var fs = new FileStream(EtwEtlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[chunkSize + overlap];
                long position = 0;
                int chunkNum = 0;

                while (position < fs.Length)
                {
                    fs.Position = position;
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    // Work directly from buffer (avoid extra copy)
                    string ascii = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Diagnostic: count raw "XBL3.0" occurrences
                    int xblCount = 0;
                    int searchIdx = 0;
                    while ((searchIdx = ascii.IndexOf("XBL3.0", searchIdx, StringComparison.Ordinal)) >= 0)
                    {
                        xblCount++;
                        searchIdx += 6;
                    }
                    totalXblHits += xblCount;
                    EventsLog($"Chunk {chunkNum}: {bytesRead / 1024}KB, XBL3.0 hits={xblCount}, regex searching...");

                    SearchForTokens(ascii, candidates);

                    // UTF-16 → strip null bytes to get ASCII
                    string stripped = StripNullBytes(buffer, bytesRead);
                    SearchForTokens(stripped, candidates);

                    EventsLog($"Chunk {chunkNum}: candidates so far={candidates.Count}");

                    position += chunkSize;
                    chunkNum++;
                }
            }

            EventsLog($"Total XBL3.0 hits across all chunks: {totalXblHits}");

            if (candidates.Count == 0)
            {
                EventsLog($"No regex candidates found (XBL3.0 hits={totalXblHits}). Trying IndexOf fallback...");

                // Fallback: re-read and use IndexOf to extract tokens directly
                if (totalXblHits > 0)
                {
                    using var fs2 = new FileStream(EtwEtlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    byte[] allBytes = new byte[fs2.Length];
                    fs2.Read(allBytes, 0, allBytes.Length);
                    string stripped = StripNullBytes(allBytes, allBytes.Length);

                    string marker = "x:XBL3.0 x=";
                    int idx = 0;
                    int found = 0;
                    while ((idx = stripped.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
                    {
                        // Extract up to 4000 chars from this position
                        int maxLen = Math.Min(4000, stripped.Length - idx);
                        string raw = stripped.Substring(idx, maxLen);
                        string token = CleanToken(raw);
                        if (token != null)
                        {
                            int score = ScoreCandidate(stripped, idx, token, null);
                            candidates.Add((token, score));
                            found++;
                            EventsLog($"IndexOf fallback found token: len={token.Length}, score={score}");
                        }
                        else
                        {
                            // Log why CleanToken rejected it
                            int endSnip = Math.Min(80, raw.Length);
                            EventsLog($"IndexOf hit rejected by CleanToken at pos={idx}, start: {raw.Substring(0, endSnip)}");
                        }
                        idx += marker.Length;
                    }
                    EventsLog($"IndexOf fallback: {found} tokens from {totalXblHits} XBL3.0 hits");
                }

                if (candidates.Count == 0)
                {
                    EventsLog("No token candidates found in ETL");
                    return null;
                }
            }

            // Sort by score descending
            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            EventsLog($"Found {candidates.Count} candidate(s):");
            foreach (var (token, score) in candidates.Take(5))
            {
                int semi = token.IndexOf(';');
                string hash = semi > 0 ? token.Substring(token.IndexOf("x=") + 2, semi - token.IndexOf("x=") - 2) : "?";
                EventsLog($"  score={score}, len={token.Length}, hash={hash}");
            }

            // Validate the best candidates
            foreach (var (token, score) in candidates)
            {
                if (IsEventsRpToken(token))
                {
                    EventsLog($"Candidate validated (x5t check passed), score={score}, len={token.Length}");
                    return token;
                }
            }

            EventsLog("No candidate passed x5t validation");
            return null;
        }

        private void SearchForTokens(string text, List<(string token, int score)> candidates)
        {
            // Search with ticket header pattern (has title ID context)
            foreach (Match match in TicketHeaderRegex.Matches(text))
            {
                string titleId = match.Groups[1].Value;
                string token = CleanToken(match.Groups[2].Value);
                if (token == null) continue;

                int score = ScoreCandidate(text, match.Index, token, titleId);
                candidates.Add((token, score));
            }

            // Search with bare token pattern
            foreach (Match match in BareTokenRegex.Matches(text))
            {
                string token = CleanToken(match.Value);
                if (token == null) continue;

                // Skip if already found via ticket header
                if (candidates.Any(c => c.token == token)) continue;

                int score = ScoreCandidate(text, match.Index, token, null);
                candidates.Add((token, score));
            }
        }

        private static string CleanToken(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            // Trim at first non-token character
            int end = raw.Length;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c < 0x20 || c > 0x7E || c == '"' || c == '\'' || c == '<' || c == '>' || c == '{' || c == '}')
                {
                    end = i;
                    break;
                }
            }

            string token = raw.Substring(0, end).TrimEnd();
            if (!token.StartsWith("x:XBL3.0 x=")) return null;
            if (token.Length < 100) return null;
            if (!token.Contains(';')) return null;

            return token;
        }

        private int ScoreCandidate(string text, int matchIndex, string token, string titleId)
        {
            int score = 0;

            // OneCollector URL proximity (+5000)
            int searchStart = Math.Max(0, matchIndex - 2000);
            int searchLen = Math.Min(4000, text.Length - searchStart);
            string vicinity = text.Substring(searchStart, searchLen);
            if (OneCollectorUrlRegex.IsMatch(vicinity))
                score += 5000;

            // Has ticket ID context (+2000)
            if (!string.IsNullOrEmpty(titleId))
                score += 2000;

            // Proper x:XBL3.0 prefix (+1000)
            if (token.StartsWith("x:XBL3.0 x="))
                score += 1000;

            // Length bonus (longer tokens are more likely complete)
            score += token.Length / 10;

            return score;
        }

        private static string StripNullBytes(byte[] data, int length)
        {
            var sb = new StringBuilder(length / 2);
            for (int i = 0; i < length; i++)
            {
                if (data[i] != 0 && data[i] >= 0x20 && data[i] <= 0x7E)
                    sb.Append((char)data[i]);
            }
            return sb.ToString();
        }

        private void EtwCleanupFiles()
        {
            try
            {
                if (Directory.Exists(EtwTempDir))
                {
                    foreach (var file in Directory.GetFiles(EtwTempDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    try { Directory.Delete(EtwTempDir, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                EventsLog($"Cleanup error: {ex.Message}");
            }
        }

        #endregion

        /// <summary>
        /// Manually triggers a scan (from the "Grab from Game" button).
        /// Works regardless of the auto-grab setting.
        /// </summary>
        public void ScanForEventsTokenManual()
        {
            eventsTokenFound = false;
            AchievementsViewModel.EventsToken = null;
            _eventsTokenObtainedAt = DateTime.MinValue;

            System.Threading.Tasks.Task.Run(() =>
            {
                GrabEventsTokenFromSolitaire();
                if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
                {
                    _eventsTokenObtainedAt = DateTime.UtcNow;
                    PersistEventsToken();
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
