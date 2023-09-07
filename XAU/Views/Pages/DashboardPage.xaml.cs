using System.Windows.Input;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;

            InitializeComponent();
        }
        //the info bar is supposed to close on its own. For some reason it doesnt so this fixes it
        //relevant issue https://github.com/lepoco/wpfui/issues/540
        private void CloseInfoBar(object sender, MouseButtonEventArgs e)
        {
            ViewModel.IsInfoBarOpen = false;
        }
    }
}
