using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
        private readonly IContentDialogService _contentDialogService;
        DateTime startTime = DateTime.Now;
        public MainWindowViewModel(IContentDialogService contentDialogService)
        {
            _contentDialogService = contentDialogService;
        }

        public async Task ShowErrorDialog(Exception exception)
        {
            TimeSpan uptime = DateTime.Now - startTime;
            string OutputString = $"Information\n"+
                                  $"=================================\n" +
                                  $"Tool Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n" +
                                  $"System Version: {Environment.OSVersion.Version.ToString()}\n" +
                                  $"Tool Uptime: {uptime.ToString()}\n" +
                                  $"=================================\n" +
                                  $"Exception\n" +
                                  $"=================================\n" +
                                  $"```\n{exception.ToString()}\n```";

            ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = "Oopsie Woopsy Fucky Wucky",
                    Content = "Something has went terribly wrong.\nPress the Copy Error button and post the message as a github issue or in the support channel on discord",
                    PrimaryButtonText = "Copy Error",
                    CloseButtonText = "Close",
                });
            
            switch (result)
            {
                case ContentDialogResult.Primary:
                    Clipboard.SetDataObject(OutputString);
                    break;
            }
        }
        [ObservableProperty]
        private string _applicationTitle = "Xbox Achievement Unlocker";

        [ObservableProperty]
        private ObservableCollection<object> _menuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Home",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
                TargetPageType = typeof(Views.Pages.HomePage)
            },
            new NavigationViewItem()
            {
                Content = "Games",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Games24 },
                TargetPageType = typeof(Views.Pages.GamesPage)
            },
            new NavigationViewItem()
            {
                Content = "Achievements",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Trophy24 },
                TargetPageType = typeof(Views.Pages.AchievementsPage)
            },
            /*new NavigationViewItem()
            {
                Content = "Stats",
                Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
                TargetPageType = typeof(Views.Pages.StatsPage)
            },*/
            new NavigationViewItem()
            {
                Content = "Misc",
                Icon = new SymbolIcon { Symbol = SymbolRegular.MoreCircle24 },
                TargetPageType = typeof(Views.Pages.MiscPage)
            }
            #if DEBUG
            ,
            new NavigationViewItem()
            {
                Content = "Debug",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Bug24 },
                TargetPageType = typeof(Views.Pages.DebugPage)
            }
            #endif


        };

        [ObservableProperty]
        private ObservableCollection<object> _footerMenuItems = new()
        {
            new NavigationViewItem()
            {
                Content = "Settings",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
                TargetPageType = typeof(Views.Pages.SettingsPage)
            },
            new NavigationViewItem()
            {
                Content = "Info",
                Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
                TargetPageType = typeof(Views.Pages.InfoPage)
            }
        };

        [ObservableProperty]
        private ObservableCollection<MenuItem> _trayMenuItems = new()
        {
            new MenuItem { Header = "Home", Tag = "tray_home" }
        };
    }
}
