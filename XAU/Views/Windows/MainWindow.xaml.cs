using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;

namespace XAU.Views.Windows
{
    public partial class MainWindow
    {
        public MainWindowViewModel ViewModel { get; }
        public static INavigationService MainNavigationService;
        public MainWindow(
            MainWindowViewModel viewModel,
            INavigationService navigationService,
            IServiceProvider serviceProvider,
            ISnackbarService snackbarService,
            IContentDialogService contentDialogService
        )
        {
            Wpf.Ui.Appearance.Watcher.Watch(this);

            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
            MainNavigationService = navigationService;
            navigationService.SetNavigationControl(NavigationView);
            snackbarService.SetSnackbarPresenter(SnackbarPresenter);
            contentDialogService.SetContentPresenter(RootContentDialog);

            NavigationView.SetServiceProvider(serviceProvider);
        }
    }
}
