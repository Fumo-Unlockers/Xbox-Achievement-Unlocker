using Newtonsoft.Json;
using System.IO;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages;

public partial class SettingsViewModel : ObservableObject, INavigationAware
{
    private bool _isInitialized;

    [ObservableProperty]
    private Wpf.Ui.Appearance.ThemeType _currentTheme = Wpf.Ui.Appearance.ThemeType.Unknown;

    private static readonly string ProgramFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
    private readonly string _settingsFilePath = Path.Combine(ProgramFolderPath, "settings.json");
    
    [ObservableProperty]
    private string? _settingsVersion;
    
    [ObservableProperty] 
    private string? _toolVersion;
    
    [ObservableProperty] 
    private bool _unlockAllEnabled;
    
    [ObservableProperty] 
    private bool _autoSpooferEnabled;
    
    [ObservableProperty] 
    private bool _autoLaunchXboxAppEnabled;
    
    [ObservableProperty] 
    private bool _fakeSignatureEnabled;
    
    [ObservableProperty] 
    private bool _regionOverride;
    
    [ObservableProperty] 
    private bool _useAcrylic;
    
    [ObservableProperty] 
    private bool _privacyMode;


    [RelayCommand]
    private void SaveSettings()
    {
        var settings = new
        {
            SettingsVersion,
            ToolVersion,
            UnlockAllEnabled,
            AutoSpooferEnabled,
            AutoLaunchXboxAppEnabled,
            FakeSignatureEnabled,
            RegionOverride,
            UseAcrylic,
            PrivacyMode
        };
            
        var settingsJson = JsonConvert.SerializeObject(settings);
        File.WriteAllText(_settingsFilePath, settingsJson);
        HomeViewModel.Settings.SettingsVersion = SettingsVersion;
        HomeViewModel.Settings.AppVersion = ToolVersion;
        HomeViewModel.Settings.UnlockAllEnabled = UnlockAllEnabled;
        HomeViewModel.Settings.AutoSpooferEnabled = AutoSpooferEnabled;
        HomeViewModel.Settings.AutoLaunchXboxAppEnabled = AutoLaunchXboxAppEnabled;
        HomeViewModel.Settings.FakeSignatureEnabled = FakeSignatureEnabled;
        HomeViewModel.Settings.RegionOverride = RegionOverride;
        HomeViewModel.Settings.UseAcrylic = UseAcrylic;
        HomeViewModel.Settings.PrivacyMode = PrivacyMode;
    }

    public void OnNavigatedTo()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }

    public void OnNavigatedFrom() { }

    private void InitializeViewModel()
    {
        CurrentTheme = Wpf.Ui.Appearance.Theme.GetAppTheme();
        LoadSettings();
        ToolVersion = $"XAU - {GetAssemblyVersion()}";
        SettingsVersion = "1";
        _isInitialized = true;
    }

    private void LoadSettings()
    {
        SettingsVersion = HomeViewModel.Settings.SettingsVersion;
        ToolVersion = HomeViewModel.Settings.AppVersion;
        UnlockAllEnabled = HomeViewModel.Settings.UnlockAllEnabled;
        AutoSpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
        AutoLaunchXboxAppEnabled = HomeViewModel.Settings.AutoLaunchXboxAppEnabled;
        FakeSignatureEnabled = HomeViewModel.Settings.FakeSignatureEnabled;
        RegionOverride = HomeViewModel.Settings.RegionOverride;
        UseAcrylic = HomeViewModel.Settings.UseAcrylic;
        PrivacyMode = HomeViewModel.Settings.PrivacyMode;
    }
    
    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString() 
               ?? "Couldn't get the tool's version.";
    }
}