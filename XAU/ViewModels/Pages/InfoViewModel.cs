using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class InfoViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        [ObservableProperty] private string? _toolVersion;
        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }
        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            ToolVersion = $"Version: {HomeViewModel.ToolVersion}";
            _isInitialized = true;
        }

        [RelayCommand]
        public void OpenDiscordUrl(string url)
        {
            var destinationurl = SocialLinks.Discord;
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
        [RelayCommand]
        public void OpenGithubUserUrl(string url)
        {
            var destinationurl = SocialLinks.GitHubUserUrl;
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
    }
}
