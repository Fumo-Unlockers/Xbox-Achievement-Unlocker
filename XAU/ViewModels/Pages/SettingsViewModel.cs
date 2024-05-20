using Newtonsoft.Json;
using System.IO;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class SettingsViewModel : ObservableObject, INavigationAware
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
        [ObservableProperty] private bool _fakeSignatureEnabled;
        [ObservableProperty] private bool _regionOverride;
        [ObservableProperty] private bool _useAcrylic;
        [ObservableProperty] private bool _privacyMode;
        [ObservableProperty] private string _xauth;
        public static bool ManualXauth = false;


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
                FakeSignatureEnabled = FakeSignatureEnabled,
                RegionOverride = RegionOverride,
                UseAcrylic = UseAcrylic,
                PrivacyMode = PrivacyMode

            };
            string settingsJson = JsonConvert.SerializeObject(settings);
            File.WriteAllText(SettingsFilePath, settingsJson);
            HomeViewModel.Settings = settings; // update ref
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();

        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            LoadSettings();
            ToolVersion = $"XAU - {GetAssemblyVersion()}";
            SettingsVersion = "1";
            _isInitialized = true;
        }

        public void LoadSettings()
        {
            SettingsVersion = HomeViewModel.Settings.SettingsVersion;
            ToolVersion = HomeViewModel.Settings.ToolVersion;
            UnlockAllEnabled = HomeViewModel.Settings.UnlockAllEnabled;
            AutoSpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
            AutoLaunchXboxAppEnabled = HomeViewModel.Settings.AutoLaunchXboxAppEnabled;
            FakeSignatureEnabled = HomeViewModel.Settings.FakeSignatureEnabled;
            RegionOverride = HomeViewModel.Settings.RegionOverride;
            UseAcrylic = HomeViewModel.Settings.UseAcrylic;
            PrivacyMode = HomeViewModel.Settings.PrivacyMode;
            Xauth = HomeViewModel.XAUTH;
        }

        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

    }
}
