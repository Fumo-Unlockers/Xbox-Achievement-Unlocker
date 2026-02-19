using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
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
        private readonly HomeViewModel _homeViewModel;
        private readonly DispatcherTimer _tokenRefreshTimer;
        private string _lastKnownEventsToken;

        public SettingsPage(SettingsViewModel viewModel, ISnackbarService snackbarService, HomeViewModel homeViewModel)
        {
            ViewModel = viewModel;
            _snackbarService = snackbarService;
            _homeViewModel = homeViewModel;
            DataContext = this;

            ViewModel.OnNavigatedToEvent += (_, _) =>
            {
                XauthTextBox.Text = HomeViewModel.XAUTH;
                SyncEventsTokenUI();
            };

            // Poll for background token changes every 3 seconds
            _tokenRefreshTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(3) };
            _tokenRefreshTimer.Tick += (_, _) =>
            {
                var current = AchievementsViewModel.EventsToken;
                if (current != _lastKnownEventsToken)
                    SyncEventsTokenUI();
            };
            _tokenRefreshTimer.Start();

            InitializeComponent();
        }

        private void SyncEventsTokenUI()
        {
            _lastKnownEventsToken = AchievementsViewModel.EventsToken;
            EventsTokenBox.Text = _lastKnownEventsToken;
            UpdateEventsTokenStatus();
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
            if (string.IsNullOrWhiteSpace(EventsTokenBox.Text))
            {
                AchievementsViewModel.EventsToken = null;
                _homeViewModel.PersistEventsToken();
                UpdateEventsTokenStatus();
                return;
            }

            AchievementsViewModel.EventsToken = EventsTokenBox.Text;
            _homeViewModel.PersistEventsToken();
            UpdateEventsTokenStatus();
        }

        private void XAuthBox_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            XauthTextBox.MaxWidth = e.NewSize.Width / 3;
        }

        private void GrabEventsToken_OnClick(object sender, RoutedEventArgs e)
        {
            if (!HomeViewModel._isLoggedIn)
            {
                _snackbarService.Show(
                    "Not Logged In",
                    "You must be logged in before scanning for an events token.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24)
                );
                return;
            }

            _snackbarService.Show(
                "Scanning...",
                "Launching Solitaire and scanning for events token. This may take up to 2-3 minutes while waiting for events to fire.",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Search24)
            );

            GrabEventsTokenButton.IsEnabled = false;
            _homeViewModel.ScanForEventsTokenManual();

            // Poll for the result - the scan can take up to ~2.5 minutes
            // (process launch + 20s Xbox Live init + 18 scan retries × 7s)
            System.Threading.Tasks.Task.Run(async () =>
            {
                for (int i = 0; i < 170; i++)
                {
                    await System.Threading.Tasks.Task.Delay(1000);
                    if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
                    {
                        Dispatcher.Invoke(() =>
                        {
                            EventsTokenBox.Text = AchievementsViewModel.EventsToken;
                            UpdateEventsTokenStatus();
                            GrabEventsTokenButton.IsEnabled = true;
                            _snackbarService.Show(
                                "Events Token Found",
                                "Successfully extracted events token from Solitaire.",
                                ControlAppearance.Success,
                                new SymbolIcon(SymbolRegular.Checkmark24)
                            );
                        });
                        return;
                    }
                }

                Dispatcher.Invoke(() =>
                {
                    GrabEventsTokenButton.IsEnabled = true;
                    _snackbarService.Show(
                        "Events Token Not Found",
                        "Could not find events token. Try playing a quick game of Solitaire first, then retry.",
                        ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.Warning24)
                    );
                });
            });
        }

        private void UpdateEventsTokenStatus()
        {
            if (HomeViewModel.IsEventsTokenValid())
            {
                EventsTokenStatus.Text = "Valid Token";
                EventsTokenStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
            }
            else if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
            {
                EventsTokenStatus.Text = "Invalid Format";
                EventsTokenStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Orange);
            }
            else
            {
                EventsTokenStatus.Text = "No Token";
                EventsTokenStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
            }
        }

        private void EventsBoxGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            EventsTokenBox.MaxWidth = e.NewSize.Width / 3;
        }
    }
}
