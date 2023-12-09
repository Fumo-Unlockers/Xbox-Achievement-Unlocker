using System.Windows.Controls.Primitives;
using Wpf.Ui.Controls;
using XAU.ViewModels.Pages;

namespace XAU.Views.Pages;

/// <summary>
/// Interaction logic for AchievementsPage.xaml
/// </summary>
public partial class AchievementsPage : INavigableView<AchievementsViewModel>
{
    public AchievementsViewModel ViewModel { get; }

    public AchievementsPage(AchievementsViewModel viewModel)
    {
        ViewModel = viewModel;
        DataContext = this;
        InitializeComponent();
    }

    private void UnlockButton(object sender, RoutedEventArgs e)
    {
        var selectedAchievement = sender as ButtonBase;
        ViewModel.UnlockAchievement(Convert.ToInt32(selectedAchievement?.Tag));
    }
}