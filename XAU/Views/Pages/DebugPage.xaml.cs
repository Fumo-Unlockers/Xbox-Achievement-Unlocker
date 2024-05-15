using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class DebugPage : INavigableView<DebugViewModel>
    {
        public DebugViewModel ViewModel { get; }

        public DebugPage(DebugViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
