using System.Collections.ObjectModel;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Windows
{
    public partial class MainWindowViewModel : ObservableObject
    {
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
