using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;
using System.Windows.Controls;
using MenuItem = Wpf.Ui.Controls.MenuItem;

namespace XAU.Views.Pages;

public partial class GamesPage : INavigableView<GamesViewModel>
{
    public GamesViewModel ViewModel { get; }

    public GamesPage(GamesViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        
        InitializeComponent();
    }

    private void FilterBox_OnSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        ViewModel.FilterGames();
    }

    private void MenuItem_OnClick(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem menuItem)
        {
            return;
        }
        
        ViewModel.CopyToClipboard(menuItem.Tag.ToString());
    }

    private void AutoSuggestBox_OnQuerySubmitted(AutoSuggestBox sender, AutoSuggestBoxQuerySubmittedEventArgs args)
    {
        ViewModel.SearchAndFilterGames();
    }

    private void AutoSuggestBox_OnSuggestionChosen(AutoSuggestBox sender, AutoSuggestBoxSuggestionChosenEventArgs args)
    {
        ViewModel.SearchAndFilterGames();
    }

    private void AutoSuggestBox_OnTextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
    {
        if (args.Reason == AutoSuggestionBoxTextChangeReason.ProgrammaticChange)
        {
            ViewModel.FilterGames();
        }
    }
}