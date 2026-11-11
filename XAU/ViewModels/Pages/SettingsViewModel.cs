using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Wpf.Ui.Controls;
using XAU.Services.HttpServer;
using XAU.Util.Logging;

namespace XAU.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware, IDisposable
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        private readonly string _settingsFilePath;

        public SettingsViewModel() : this(Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "settings.json"))
        { }

        // File-path seam keeps settings persistence tests out of the real Documents directory.
        internal SettingsViewModel(string settingsFilePath)
        {
            _settingsFilePath = settingsFilePath;
        }
        //settings
        [ObservableProperty] private string _settingsVersion;
        [ObservableProperty] private string _toolVersion;
        [ObservableProperty] private bool _unlockAllEnabled;
        [ObservableProperty] private bool _autoSpooferEnabled;
        [ObservableProperty] private bool _autoLaunchXboxAppEnabled;
        [ObservableProperty] private bool _launchHidden;
        [ObservableProperty] private bool _fakeSignatureEnabled;
        [ObservableProperty] private bool _regionOverride;
        [ObservableProperty] private bool _useAcrylic;
        [ObservableProperty] private bool _privacyMode;
        [ObservableProperty] private bool _oAuthLogin;
        [ObservableProperty] private bool _autoGrabEventsToken;
        [ObservableProperty] private bool _enableDiagnosticsLog;
        [ObservableProperty] private int _xauthScanReadLength = 16384;
        [ObservableProperty] private string _xauth;

        [ObservableProperty] private bool _serverEnabled;
        [ObservableProperty] private string _serverPort = "1337";
        [ObservableProperty] private string _listeningAddress = "http://localhost:1337";

        private HttpServer? _httpServer;
        private bool _disposed;

        public static bool ManualXauth = false;
        public RoutedEventHandler OnNavigatedToEvent = null!;

        [RelayCommand]
        public void SaveSettings()
        {
            lock (HomeViewModel.SettingsWriteLock)
            {
                var settings = new XAUSettings
                {
                    SettingsVersion = SettingsVersion,
                    ToolVersion = ToolVersion,
                    UnlockAllEnabled = UnlockAllEnabled,
                    AutoSpooferEnabled = AutoSpooferEnabled,
                    AutoLaunchXboxAppEnabled = AutoLaunchXboxAppEnabled,
                    LaunchHidden = LaunchHidden,
                    FakeSignatureEnabled = FakeSignatureEnabled,
                    RegionOverride = RegionOverride,
                    UseAcrylic = UseAcrylic,
                    PrivacyMode = PrivacyMode,
                    OAuthLogin = OAuthLogin,
                    AutoGrabEventsToken = AutoGrabEventsToken,
                    EnableDiagnosticsLog = EnableDiagnosticsLog,
                    XauthScanReadLength = XauthScanReadLength,
                    // The events token and provenance are not settings-page fields. Copy them only
                    // after acquiring the same lock used by PersistEventsToken so saves cannot
                    // overwrite a concurrent token update with stale data.
                    CachedEventsToken = HomeViewModel.Settings.CachedEventsToken,
                    EventsTokenObtainedAt = HomeViewModel.Settings.EventsTokenObtainedAt,
                    EventsUserHash = HomeViewModel.Settings.EventsUserHash
                };
                HomeViewModel.Settings = settings;
                string settingsJson = JsonConvert.SerializeObject(settings);
                File.WriteAllText(_settingsFilePath, settingsJson);
            }
            // Apply the just-saved choice immediately so turning logging off takes effect without a restart.
            DiagLog.Enabled = EnableDiagnosticsLog;
            // Live-apply the scan read length too, so the very next token scan uses it (no restart needed).
            HomeViewModel.ScanReadLength = HomeViewModel.NormalizeScanReadLength(XauthScanReadLength);
        }

        /// <summary>
        /// Called by the Settings "Token Scan Read Length" box on each edit. Ignores non-numeric/transient
        /// text (so the committed value is never clobbered mid-type), clamps into the acceptable band,
        /// live-applies to HomeViewModel.ScanReadLength (the next scan uses it, no restart), and persists.
        /// A no-op when unchanged, so populating the box on load writes nothing.
        /// </summary>
        public void OnScanReadLengthTextChanged(string text)
        {
            if (!int.TryParse(text,
                    System.Globalization.NumberStyles.Integer,
                    System.Globalization.CultureInfo.InvariantCulture, out int parsed))
                return;

            int normalized = HomeViewModel.NormalizeScanReadLength(parsed);
            if (HomeViewModel.ScanReadLength == normalized && XauthScanReadLength == normalized)
                return;

            XauthScanReadLength = normalized;
            HomeViewModel.ScanReadLength = normalized;
            SaveSettings();
        }

        [RelayCommand]
        private void ToggleServer()
        {
            if (_httpServer == null)
            {
                var routes = Routes.GetRoutes(
                    getXauthToken: () => HomeViewModel.XAUTH,
                    getXboxRestAPI: () => new XboxRestAPI(HomeViewModel.XAUTH),
                    getXUIDOnly: () => HomeViewModel.XUIDOnly
                ); _httpServer = new HttpServer(ServerPort, routes);
            }

            if (ServerEnabled)
            {
                _httpServer.Start();
                UpdateListeningAddress();
            }
            else
            {
                _httpServer.Stop();
                ListeningAddress = $"http://localhost:{ServerPort}";
            }
            // TO DO: SAVE SERVER ENABLED/DISABLED STATUS & PORT NUMBER
            //SaveSettings();
        }

        [RelayCommand]
        public void UpdateServerPort()
        {
            if (_httpServer != null)
            {
                _httpServer.UpdatePort(ServerPort);
                UpdateListeningAddress();
            }

            // TO DO: SAVE SERVER ENABLED/DISABLED STATUS & PORT NUMBER
            //SaveSettings();
        }

        [RelayCommand]
        public void RestartAsAdmin()
        {
            if (_httpServer != null)
            {
                _httpServer.RestartAsAdmin();
            }
        }

        [RelayCommand]
        private void OpenListeningAddress()
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(ListeningAddress))
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = ListeningAddress,
                        UseShellExecute = true
                    });
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to open address: {ex.Message}");
            }
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
            {
                InitializeViewModel();
            }

            OnNavigatedToEvent.Invoke(this, new RoutedEventArgs());
        }

        public void OnNavigatedFrom()
        { }

        private void InitializeViewModel()
        {
            LoadSettings();
            ToolVersion = $"XAU - {GetAssemblyVersion()}";
            SettingsVersion = "2";
            _isInitialized = true;

            if (_httpServer == null)
            {
                var routes = Routes.GetRoutes(
                    getXauthToken: () => HomeViewModel.XAUTH,
                    getXboxRestAPI: () => new XboxRestAPI(HomeViewModel.XAUTH),
                    getXUIDOnly: () => HomeViewModel.XUIDOnly
                );
                _httpServer = new HttpServer(ServerPort, routes);
            }
            ListeningAddress = $"http://localhost:{ServerPort}";
        }

        // ClearAuthCache changes the Home settings while this page remains open. Refresh all
        // fields before another toggle calls SaveSettings and could re-enable stale OAuth mode.
        public void RefreshAfterAuthCacheCleared()
        {
            LoadSettings();
        }

        public void LoadSettings()
        {
            SettingsVersion = HomeViewModel.Settings.SettingsVersion;
            ToolVersion = HomeViewModel.Settings.ToolVersion;
            UnlockAllEnabled = HomeViewModel.Settings.UnlockAllEnabled;
            AutoSpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
            AutoLaunchXboxAppEnabled = HomeViewModel.Settings.AutoLaunchXboxAppEnabled;
            LaunchHidden = HomeViewModel.Settings.LaunchHidden;
            FakeSignatureEnabled = HomeViewModel.Settings.FakeSignatureEnabled;
            RegionOverride = HomeViewModel.Settings.RegionOverride;
            UseAcrylic = HomeViewModel.Settings.UseAcrylic;
            PrivacyMode = HomeViewModel.Settings.PrivacyMode;
            Xauth = HomeViewModel.XAUTH;
            OAuthLogin = HomeViewModel.Settings.OAuthLogin;
            AutoGrabEventsToken = HomeViewModel.Settings.AutoGrabEventsToken;
            EnableDiagnosticsLog = HomeViewModel.Settings.EnableDiagnosticsLog;
            XauthScanReadLength = HomeViewModel.Settings.XauthScanReadLength;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

        private void UpdateListeningAddress()
        {
            if (_httpServer != null)
            {
                ListeningAddress = _httpServer.GetListeningAddress();
            }
        }
        partial void OnServerPortChanged(string value)
        {
            if (_httpServer != null)
            {
                _httpServer.UpdatePort(value);
                UpdateListeningAddress();
            }
            // TO DO: SAVE SERVER ENABLED/DISABLED STATUS & PORT NUMBER
            //SaveSettings();
        }
        public void Dispose()
        {
            if (_disposed) return;

            if (_httpServer != null)
            {
                _httpServer.Dispose();
                _httpServer = null;
            }

            _disposed = true;
        }
    }
}
