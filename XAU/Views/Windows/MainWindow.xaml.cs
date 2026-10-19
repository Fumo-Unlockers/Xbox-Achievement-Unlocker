using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.ViewModels.Windows;

namespace XAU.Views.Windows;

public partial class MainWindow
{
    public MainWindowViewModel ViewModel { get; }

    public MainWindow(
        MainWindowViewModel viewModel,
        INavigationService navigationService,
        IServiceProvider serviceProvider,
        ISnackbarService snackbarService,
        IContentDialogService contentDialogService
    )
    {
        // Accent discovery can fail in restricted desktop sessions. The app already
        // supplies its accent resources, so keep theme/backdrop watching without it.
        Wpf.Ui.Appearance.Watcher.Watch(
            this,
            WindowBackdropType.Mica,
            updateAccents: false,
            forceBackground: false);

        ViewModel = viewModel;
        DataContext = this;

        InitializeComponent();
        navigationService.SetNavigationControl(NavigationView);
        snackbarService.SetSnackbarPresenter(SnackbarPresenter);
        contentDialogService.SetContentPresenter(RootContentDialog);

        NavigationView.SetServiceProvider(serviceProvider);
    }

    private void NavigationView_OnLoaded(object sender, RoutedEventArgs e)
    {
        if (sender is not NavigationView navigationView)
        {
            return;
        }

        navigationView.IsPaneOpen = false;
    }
}
