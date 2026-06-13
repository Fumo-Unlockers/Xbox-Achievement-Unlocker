using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
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

        private async void ButtonBase_OnClick(object sender, RoutedEventArgs e)
        {
            if (sender is ButtonBase selectedGame && selectedGame.Tag is string index)
                await ViewModel.OpenAchievements(index);
        }

        private void SearchBox_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
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

        private void ButtonBase_RightClick(object sender, MouseButtonEventArgs e)
        {
            if (sender is ButtonBase selectedGame && selectedGame.Tag is string index)
                ViewModel.CopyToClipboard(index);
        }

        private void Image_ImageFailed(object sender, RoutedEventArgs e)
        {
            if (sender is Wpf.Ui.Controls.Image uiImage)
            {
                uiImage.Source = new BitmapImage(new Uri("pack://application:,,,/Assets/cirno.png"));
            }
        }

        private double _cachedRowHeight;

        // Snap scrolling to exactly one row of tiles per wheel notch instead of the
        // default (large, pixel-based) jump. Row height is read once from a realized
        // item and cached since every tile is uniform height.
        private void GamesList_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is not ListBox lb) return;
            var scrollViewer = FindVisualChild<ScrollViewer>(lb);
            if (scrollViewer == null) return;

            if (_cachedRowHeight <= 0)
            {
                for (int i = 0; i < lb.Items.Count; i++)
                {
                    if (lb.ItemContainerGenerator.ContainerFromIndex(i) is FrameworkElement fe && fe.ActualHeight > 0)
                    {
                        _cachedRowHeight = fe.ActualHeight;
                        break;
                    }
                }
            }

            if (_cachedRowHeight <= 0) return;

            double direction = e.Delta > 0 ? -1 : 1;
            scrollViewer.ScrollToVerticalOffset(scrollViewer.VerticalOffset + direction * _cachedRowHeight);
            e.Handled = true;
        }

        private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T result) return result;
                var found = FindVisualChild<T>(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}
