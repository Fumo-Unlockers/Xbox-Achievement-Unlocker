using Newtonsoft.Json;
using System.Diagnostics;
using System.IO;
using Wpf.Ui.Controls;
using XAU.Services.HttpServer;

namespace XAU.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware, IDisposable
    {
        private bool _isInitialized = false;

        [ObservableProperty]
        private string _appVersion = String.Empty;

        static string ProgramFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
        string SettingsFilePath = Path.Combine(ProgramFolderPath, "settings.json");
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
                OAuthLogin = OAuthLogin
            };
            string settingsJson = JsonConvert.SerializeObject(settings);
            File.WriteAllText(SettingsFilePath, settingsJson);
            HomeViewModel.Settings = settings; // update ref
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
