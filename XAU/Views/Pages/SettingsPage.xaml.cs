using System.Windows.Controls;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class SettingsPage : INavigableView<SettingsViewModel>
    {
        public SettingsViewModel ViewModel { get; }

        public SettingsPage(SettingsViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }

        private void XauthTextBox_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            HomeViewModel.XAUTH = XauthTextBox.Text;
            SettingsViewModel.ManualXauth = true;
        }

        private void EventsToken_OnTextChanged(object sender, TextChangedEventArgs e)
        {
            AchievementsViewModel.EventsToken = EventsTokenBox.Text;
        }
    }
}
