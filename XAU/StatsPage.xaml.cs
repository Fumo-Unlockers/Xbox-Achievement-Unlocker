using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages
{
    /// <summary>
    /// Interaction logic for StatsPage.xaml
    /// </summary>
    public partial class StatsPage : INavigableView<StatsViewModel>
    {
        public StatsViewModel ViewModel { get; }

        public StatsPage()
        {
            ViewModel = new StatsViewModel();
            DataContext = ViewModel;
            InitializeComponent();
        }
    }
}
