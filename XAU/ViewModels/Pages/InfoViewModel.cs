using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class InfoViewModel : ObservableObject, INavigationAware
    {
        private bool _isInitialized = false;
        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }
        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        [RelayCommand]
        public void OpenDiscordUrl(string url)
        {
            var destinationurl = "https://discord.gg/fCqM7287jG";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
        [RelayCommand]
        public void OpenGithubUserUrl(string url)
        {
            var destinationurl = "https://github.com/ItsLogic";
            var sInfo = new System.Diagnostics.ProcessStartInfo(destinationurl)
            {
                UseShellExecute = true,
            };
            System.Diagnostics.Process.Start(sInfo);
        }
    }
}
