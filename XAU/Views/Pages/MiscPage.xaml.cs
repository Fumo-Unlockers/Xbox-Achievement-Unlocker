using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class MiscPage : INavigableView<MiscViewModel>
    {
        public MiscViewModel ViewModel { get; }

        public MiscPage(MiscViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }

        private void Selector_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ListBox listbox = sender as ListBox;
            if (listbox.SelectedIndex == -1) return;
            ViewModel.DisplayGameInfo(listbox.SelectedIndex);
        }
        private void TitleIDSearch_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                //for some reason, the search text is not being updated when pressing enter
                ViewModel.TSearchText = TitleIdSearchBox.Text;
                ViewModel.SearchGame();
            }
        }

        private void GamertagSearch_OnKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                // Fully qualify TextBox to avoid ambiguity
                var textBox = sender as System.Windows.Controls.TextBox;
                var bindingExpression = textBox.GetBindingExpression(System.Windows.Controls.TextBox.TextProperty);
                bindingExpression?.UpdateSource();

                if (ViewModel.SearchGamertagCommand.CanExecute(null))
                {
                    ViewModel.SearchGamertagCommand.Execute(null);
                }
            }
        }
        private async void ExportToCsvButton_Click(object sender, RoutedEventArgs e)
        {
            await ViewModel.ExportToCsvAsync();
        }

        private void GamesApi_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ViewModel.ClearSearchResults();
        }

    }
}
