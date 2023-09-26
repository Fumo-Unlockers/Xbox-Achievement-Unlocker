using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    /// <summary>
    /// Interaction logic for GamesPage.xaml
    /// </summary>
    public partial class GamesPage : INavigableView<GamesViewModel>
    {
        public GamesViewModel ViewModel { get; }
        public GamesPage(GamesViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        private void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            ButtonBase selectedGame = sender as ButtonBase;
            ViewModel.OpenAchievements(selectedGame.Content.ToString());
        }

        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key==Key.Enter)
            {
                //for some reason, the search text is not being updated when pressing enter
                ViewModel.SearchText = SearchBox.Text;
                ViewModel.SearchAndFilterGames();

            }
        }

        private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.FilterGames();
        }

        private void PageBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.PageChanged();
        }

        private void ButtonBase_RightClick(object sender, MouseButtonEventArgs e)
        {
            ButtonBase selectedGame = sender as ButtonBase;
            ViewModel.CopyToClipboard(selectedGame.Content.ToString());
        }
    }
}
