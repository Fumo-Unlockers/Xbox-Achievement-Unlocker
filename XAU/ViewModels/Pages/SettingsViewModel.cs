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

        [ObservableProperty]
        private Wpf.Ui.Appearance.ThemeType _currentTheme = Wpf.Ui.Appearance.ThemeType.Unknown;

        static string ProgramFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
        string SettingsFilePath = Path.Combine(ProgramFolderPath, "settings.json");
        //settings
        [ObservableProperty]
        private string _settingsVersion;
        [ObservableProperty]
        private string _toolVersion;
        [ObservableProperty]
        private bool _unlockAllEnabled;

        [RelayCommand]
        public void SaveSettings()
        {
            var settings = new
            {
                SettingsVersion = SettingsVersion,
                ToolVersion = ToolVersion,
                UnlockAllEnabled = UnlockAllEnabled
            };
            string settingsJson = JsonConvert.SerializeObject(settings);
            File.WriteAllText(SettingsFilePath, settingsJson);
        }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
            
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            CurrentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
            AppVersion = $"XAU - {GetAssemblyVersion()}";
            LoadSettings();
            _isInitialized = true;
        }

        public void LoadSettings()
        {
            string settingsJson = File.ReadAllText(SettingsFilePath);
            var settings = JsonConvert.DeserializeObject<dynamic>(settingsJson);
            SettingsVersion = settings.SettingsVersion;
            ToolVersion = settings.ToolVersion;
            UnlockAllEnabled = settings.UnlockAllEnabled;
        }
        private string GetAssemblyVersion()
        {
            return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
                ?? String.Empty;
        }

    }
}
