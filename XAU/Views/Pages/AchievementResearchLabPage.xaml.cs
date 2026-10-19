using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages;

public partial class AchievementResearchLabPage : INavigableView<AchievementResearchLabViewModel>
{
    public AchievementResearchLabViewModel ViewModel { get; }

    public AchievementResearchLabPage(AchievementResearchLabViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }
}
