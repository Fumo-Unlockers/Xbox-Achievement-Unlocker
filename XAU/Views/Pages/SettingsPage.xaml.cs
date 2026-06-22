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
        private bool _manualScanInProgress;

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

                if (_manualScanInProgress && !_homeViewModel.ManualScanRunning)
                {
                    _manualScanInProgress = false;
                    GrabEventsTokenButton.IsEnabled = true;
                    if (!string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
                    {
                        _snackbarService.Show(
                            "Events Token Found",
                            "Successfully extracted events token.",
                            ControlAppearance.Success,
                            new SymbolIcon(SymbolRegular.Checkmark24));
                    }
                    else
                    {
                        _snackbarService.Show(
                            "Events Token Not Found",
                            "Could not find events token. Try flipping a card in Solitaire, then retry.",
                            ControlAppearance.Caution,
                            new SymbolIcon(SymbolRegular.Warning24));
                    }
                }
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

            HomeViewModel.XAUTH = XboxRestAPI.SanitizeXauthPublic(XauthTextBox.Text);
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
                "Launching Solitaire and scanning for events token. Flip a card to speed it up.",
                ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Search24)
            );

            _manualScanInProgress = true;
            GrabEventsTokenButton.IsEnabled = false;
            _homeViewModel.ScanForEventsTokenManual();
        }

        private void UpdateEventsTokenStatus()
        {
            if (HomeViewModel.IsEventsTokenValid())
            {
                if (HomeViewModel.IsEventsTokenExpired())
                {
                    EventsTokenStatus.Text = "Expired";
                    EventsTokenStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red);
                }
                else
                {
                    EventsTokenStatus.Text = "Valid Token";
                    EventsTokenStatus.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Green);
                }
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

            UpdateEventsTokenTimestamps();
        }

        private void UpdateEventsTokenTimestamps()
        {
            var obtained = HomeViewModel.EventsTokenObtainedAtUtc;
            var expires = HomeViewModel.EventsTokenExpiresAtUtc;

            if (obtained == DateTime.MinValue || string.IsNullOrEmpty(AchievementsViewModel.EventsToken))
            {
                EventsTokenCreated.Text = "N/A";
                EventsTokenCreated.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                EventsTokenExpires.Text = "N/A";
                EventsTokenExpires.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
                return;
            }

            EventsTokenCreated.Text = obtained.ToLocalTime().ToString("g");
            EventsTokenCreated.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);

            if (expires.HasValue)
            {
                var expiresLocal = expires.Value.ToLocalTime();
                EventsTokenExpires.Text = expiresLocal.ToString("g");
                EventsTokenExpires.Foreground = expires.Value < DateTime.UtcNow
                    ? new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Red)
                    : new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.White);
            }
            else
            {
                EventsTokenExpires.Text = "N/A";
                EventsTokenExpires.Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Colors.Gray);
            }
        }

        private void EventsBoxGrid_OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            EventsTokenBox.MaxWidth = e.NewSize.Width / 3;
        }
    }
}
