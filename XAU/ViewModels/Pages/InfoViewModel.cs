using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages;

public partial class InfoViewModel : ObservableObject, INavigationAware
{
    private bool _isInitialized;
    
    [ObservableProperty] 
    private string? _toolVersion;
    
    public void OnNavigatedTo()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }
    
    public void OnNavigatedFrom() { }

    private void InitializeViewModel()
    {
        ToolVersion = $"Version: {HomeViewModel.ToolVersion}";
        _isInitialized = true;
    }

    [RelayCommand]
    private static void OpenDiscordUrl()
    {
        OpenUrl("https://discord.gg/fCqM7287jG");
    }
    
    [RelayCommand]
    private static void OpenGithubUserUrl()
    {
        OpenUrl("https://github.com/ItsLogic");
    }

    private static void OpenUrl(string destinationUrl)
    {
        var sInfo = new System.Diagnostics.ProcessStartInfo(destinationUrl) { UseShellExecute = true };
        System.Diagnostics.Process.Start(sInfo);
    }
}