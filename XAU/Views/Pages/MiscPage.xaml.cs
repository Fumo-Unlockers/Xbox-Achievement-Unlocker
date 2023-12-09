using System.Windows.Controls;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages;

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
        if (sender is not ListBox listBox)
        {
            return;
        }
        
        if (listBox.SelectedIndex == -1) return;
        ViewModel.DisplayGameInfo(listBox.SelectedIndex);
    }
}