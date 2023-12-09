using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages;

public class StatsViewModel : ObservableObject, INavigationAware
{
    private bool _isInitialized;
    
    public void OnNavigatedTo()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }
    public void OnNavigatedFrom() { }

    private void InitializeViewModel()
    {
        _isInitialized = true;
    }
}