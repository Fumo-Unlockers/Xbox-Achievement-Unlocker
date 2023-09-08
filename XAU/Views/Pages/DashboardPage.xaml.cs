using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;
using MessageBox = System.Windows.MessageBox;

namespace XAU.Views.Pages
{
    public partial class DashboardPage : INavigableView<DashboardViewModel>
    {
        public DashboardViewModel ViewModel { get; }

        public DashboardPage(DashboardViewModel viewModel)
        {
            ViewModel = viewModel;
            DataContext = this;
            ViewModel.XauthWorker.RunWorkerAsync();
            ViewModel.XauthWorker.DoWork += ViewModel.XauthWorker_DoWork;
            ViewModel.XauthWorker.ProgressChanged += ViewModel.XauthWorker_ProgressChanged;
            ViewModel.XauthWorker.RunWorkerCompleted += ViewModel.XauthWorker_RunWorkerCompleted;
            ViewModel.XauthWorker.WorkerReportsProgress = true;
            InitializeComponent();
        }
    }
}
