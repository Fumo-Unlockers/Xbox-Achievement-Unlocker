using System.Collections.ObjectModel;
using System.Windows.Controls;
using Wpf.Ui.Controls;
using Wpf.Ui.Extensions;

namespace XAU.ViewModels.Windows;

public partial class MainWindowViewModel(IContentDialogService contentDialogService) : ObservableObject
{
    private readonly DateTime _startTime = DateTime.Now;

    public async Task ShowErrorDialog(Exception exception)
    {
        var outputString = $"Information\n"+
                           $"=================================\n" +
                           $"Tool Version: {System.Reflection.Assembly.GetExecutingAssembly().GetName().Version}\n" +
                           $"System Version: {Environment.OSVersion.Version}\n" +
                           $"Tool Uptime: {DateTime.Now - _startTime}\n" +
                           $"=================================\n" +
                           $"Exception\n" +
                           $"=================================\n" +
                           $"```\n{exception.Message}\n```";

        var result = await contentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
        {
            Title = "Error",
            Content = "Something has went terribly wrong.\nPress the Copy Error button and post the message as a github issue or in the support channel on discord",
            PrimaryButtonText = "Copy Error",
            CloseButtonText = "Close",
        });
            
        switch (result)
        {
            case ContentDialogResult.Primary:
                Clipboard.SetDataObject(outputString);
                break;
        }
    }
    [ObservableProperty]
    private string _applicationTitle = "Xbox Achievement Unlocker";

    [ObservableProperty]
    private ObservableCollection<object> _menuItems =
    [
        new Separator(),
        new NavigationViewItem
        {
            Content = "Home",
            Margin = new Thickness(0, 2.5, 0, 0),
            Icon = new SymbolIcon { Symbol = SymbolRegular.Home24 },
            TargetPageType = typeof(Views.Pages.HomePage)
        },

        new NavigationViewItem
        {
            Content = "Games",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Games24 },
            TargetPageType = typeof(Views.Pages.GamesPage)
        },

        new NavigationViewItem
        {
            Content = "Achievements",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Trophy24 },
            TargetPageType = typeof(Views.Pages.AchievementsPage)
        },
        /*new NavigationViewItem
        {
            Content = "Stats",
            Icon = new SymbolIcon { Symbol = SymbolRegular.DataHistogram24 },
            TargetPageType = typeof(Views.Pages.StatsPage)
        },*/

        new NavigationViewItem
        {
            Content = "Misc",
            Icon = new SymbolIcon { Symbol = SymbolRegular.MoreCircle24 },
            TargetPageType = typeof(Views.Pages.MiscPage)
        }
    ];

    [ObservableProperty]
    private ObservableCollection<object> _footerMenuItems =
    [
        new NavigationViewItem
        {
            Content = "Settings",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Settings24 },
            TargetPageType = typeof(Views.Pages.SettingsPage)
        },

        new NavigationViewItem
        {
            Content = "Info",
            Icon = new SymbolIcon { Symbol = SymbolRegular.Info24 },
            TargetPageType = typeof(Views.Pages.InfoPage)
        }
    ];
}