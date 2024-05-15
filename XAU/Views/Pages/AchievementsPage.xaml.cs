using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
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

        public AchievementsPage(AchievementsViewModel viewModel)
        {
            ViewModel = viewModel;
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
                await ViewModel.SearchAndFilterAchievementsAsync();

            }
        }
    }
}
