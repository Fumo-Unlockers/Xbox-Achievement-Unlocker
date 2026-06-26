using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    public partial class QueuePage : INavigableView<QueueViewModel>
    {
        public QueueViewModel ViewModel { get; }

        public QueuePage(QueueViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            InitializeComponent();
        }
    }
}
