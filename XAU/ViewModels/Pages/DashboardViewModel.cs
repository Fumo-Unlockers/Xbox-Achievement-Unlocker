using System.ComponentModel;
using System.Windows.Media;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        //attach vars
        [ObservableProperty] private string _attached = "Not Attached";
        [ObservableProperty] private Brush _attachedColor = new SolidColorBrush(Colors.Red);

        //profile vars
        [ObservableProperty] private String _gamerPic = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gamerTag = "Gamertag: Unknown   ";
        [ObservableProperty] private string _xuid = "XUID: Unknown";
        [ObservableProperty] private string _gamerScore = "Gamerscore: Unknown";
        [ObservableProperty] private string _profileRep = "Reputation: Unknown";
        [ObservableProperty] private string _accountTier = "Tier: Unknown";
        [ObservableProperty] private string _currentlyPlaying = "Currently Playing: Unknown";
        [ObservableProperty] private string _activeDevice = "Active Device: Unknown";
        [ObservableProperty] private string _isVerified = "Verified: Unknown";
        [ObservableProperty] private string _location = "Location: Unknown";
        [ObservableProperty] private string _tenure = "Tenure: Unknown";
        [ObservableProperty] private string _following = "Following: Unknown";
        [ObservableProperty] private string _followers = "Followers: Unknown";
        [ObservableProperty] private string _gamepass = "Gamepass: Unknown";

        [ObservableProperty] private string _bio = "Bio: Unknown";

        //infobar
        [ObservableProperty] private bool _isInfoBarOpen = false;
        [ObservableProperty] private string _infoBarText = "InfoBar Text";
        [ObservableProperty] private string _infoBarTitle = "InfoBar Title";
        [ObservableProperty] private InfoBarSeverity _infoBarType = InfoBarSeverity.Informational;



        [RelayCommand]
        private void RefreshProfile()
        {
            //refresh profile
            GamerTag = "Gamertag: balls";
            InfoBarText = "Profile Refreshed";
            InfoBarTitle = "Success";
            InfoBarType = InfoBarSeverity.Success;
            IsInfoBarOpen = true;
        }

    }
}
