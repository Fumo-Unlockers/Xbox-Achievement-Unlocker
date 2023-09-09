using System.Windows.Controls.Primitives;
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
    }
}
