using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    /// <summary>
    /// Interaction logic for AchievementsPage.xaml
    /// </summary>
    public partial class AchievementsPage : INavigableView<AchievementsViewModel>
    {
        public AchievementsViewModel ViewModel { get; }
        private readonly INavigationService _navigationService;
        private readonly MiscViewModel _miscViewModel;
        public AchievementsPage(AchievementsViewModel viewModel, INavigationService navigationService, MiscViewModel miscViewModel)
        {
            ViewModel = viewModel;

            _navigationService = navigationService;
            _miscViewModel = miscViewModel;

            DataContext = this;
            InitializeComponent();
        }

        private void UnlockButton(object sender, RoutedEventArgs e)
        {
            ButtonBase SelectedAchievement = sender as ButtonBase;
            ViewModel.UnlockAchievement(Convert.ToInt32(SelectedAchievement.Tag));
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {


        }

        private async void SearchBox_OnKeyDownAsync(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //for some reason, the search text is not being updated when pressing enter
                ViewModel.SearchText = SearchBox.Text;
                await ViewModel.SearchAndFilterAchievements();

            }
        }



        private void AutoUnlockBtn_Click(object sender, RoutedEventArgs e)
        {
            _miscViewModel.SelectedTabIndex = 4;
            _miscViewModel.UpdateInfoAutoUnlocker();
            _navigationService.Navigate(typeof(MiscPage));
        }

    }
}
