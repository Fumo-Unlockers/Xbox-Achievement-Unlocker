using System.Windows.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }
        private readonly ISnackbarService _snackbarService;

        public SettingsPage(SettingsViewModel viewModel, ISnackbarService snackbarService)
        {
            ViewModel = viewModel;
            _snackbarService = snackbarService;
            DataContext = this;

            ViewModel.OnNavigatedToEvent += (_, _) =>
            {
                XauthTextBox.Text = HomeViewModel.XAUTH;
                EventsTokenBox.Text = AchievementsViewModel.EventsToken;
            };

            InitializeComponent();
        }

        private void XauthTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(XauthTextBox.Text) || string.IsNullOrEmpty(XauthTextBox.Text))
            {
                _snackbarService.Show(
                    "Error",
                    "XAuth Token cannot be empty/whitespace",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24)
                );
                return;
            }

            HomeViewModel.XAUTH = XauthTextBox.Text;
            SettingsViewModel.ManualXauth = true;
            HomeViewModel.XAUTHTested = false;
        }

        private void EventsToken_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(EventsTokenBox.Text) || string.IsNullOrEmpty(EventsTokenBox.Text))
            {
                _snackbarService.Show(
                    "Error",
                    "Events Token cannot be empty/whitespace",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24)
                );
                return;
            }

            AchievementsViewModel.EventsToken = EventsTokenBox.Text;
        }

        private void XAuthBox_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            XauthTextBox.MaxWidth = e.NewSize.Width / 3;
        }

        private void EventsBoxGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            EventsTokenBox.MaxWidth = e.NewSize.Width / 3;
        }
    }
}
