using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using Wpf.Ui.Services;


namespace XAU.ViewModels.Pages
{
    public partial class MiscViewModel : ObservableObject, INavigationAware
    {
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH, System.Globalization.CultureInfo.CurrentCulture.Name));
        private Lazy<TrueAchievementRestApi> _taRestApi = new Lazy<TrueAchievementRestApi>();

        public MiscViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }

        public void OnNavigatedTo()
        {
            if (!IsInitialized && HomeViewModel.InitComplete)
                InitializeViewModel();
        }

        public void OnNavigatedFrom()
        {
        }

        private void InitializeViewModel()
        {
            if (HomeViewModel.Settings.RegionOverride)
                currentSystemLanguage = "en-GB";
            IsInitialized = true;
        }

        #region Spoofer

        [ObservableProperty] private string _gameName = "Name: ";
        [ObservableProperty] private string _gameTitleID = "Title ID: ";
        [ObservableProperty] private string _gamePFN = "PFN: ";
        [ObservableProperty] private string _gameType = "Type: ";
        [ObservableProperty] private string _gameGamepass = "Gamepass: ";
        [ObservableProperty] private string _gameDevices = "Devices: ";
        [ObservableProperty] private string _gameGamerscore = "Gamerscore: ?/?";
        [ObservableProperty] private string? _gameImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gameTime = "Time Played: ";
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _currentSpoofingID = "";
        [ObservableProperty] private string _newSpoofingID = "";
        [ObservableProperty] private string _spoofingText = "Spoofing Not Started";
        [ObservableProperty] private string _spoofingButtonText = "Start Spoofing";
        private bool SpoofingUpdate = false;
        private bool CurrentlySpoofing = false;
        private GameTitle GameInfoResponse;
        private GameStatsResponse GameStatsResponse;

        [RelayCommand]
        public async Task SpooferButtonClicked()
        {
            if (CurrentlySpoofing)
            {
                SpoofingUpdate = true;
                CurrentlySpoofing = false;
                SpoofingText = "Spoofing Not Started";
                SpoofingButtonText = "Start Spoofing";
                //reset game info
                GameName = "Name: ";
                GameTitleID = "Title ID: ";
                GamePFN = "PFN: ";
                GameType = "Type: ";
                GameGamepass = "Gamepass: ";
                GameDevices = "Devices: ";
                GameGamerscore = "Gamerscore: ?/?";
                GameImage = "pack://application:,,,/Assets/cirno.png";
                GameTime = "Time Played: ";
                HomeViewModel.SpoofingStatus = 0;
                await _xboxRestAPI.Value.StopHeartbeatAsync(HomeViewModel.XUIDOnly);
                return;
            }
            HomeViewModel.SpoofedTitleID = NewSpoofingID;

            if (HomeViewModel.SpoofingStatus == 2)
            {
                HomeViewModel.SpoofingStatus = 1;
                AchievementsViewModel.SpoofingUpdate = true;
            }
            HomeViewModel.SpoofingStatus = 1;
            SpoofGame();
        }

        public async void SpoofGame()
        {
            CurrentSpoofingID = NewSpoofingID;
            GameInfoResponse = await _xboxRestAPI.Value.GetGameTitleAsync(HomeViewModel.XUIDOnly, NewSpoofingID);
            GameStatsResponse = await _xboxRestAPI.Value.GetGameStatsAsync(HomeViewModel.XUIDOnly, NewSpoofingID);

            if (GameInfoResponse == null || GameStatsResponse == null)
            {
                _snackbarService.Show("Error: Unable to acquire game info or stats",
                    $"The game info was invalid.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            try
            {
                GameName = "Name: " + GameInfoResponse.Titles[0].Name;
                GameImage = GameInfoResponse.Titles[0].DisplayImage;
                GameTitleID = "Title ID: " + GameInfoResponse.Titles[0].TitleId;
                GamePFN = "PFN: " + GameInfoResponse.Titles[0].Pfn;
                GameType = "Type: " + GameInfoResponse.Titles[0].Type;
                GameGamepass = "Gamepass: " + GameInfoResponse.Titles[0].GamePass?.IsGamePass;
                GameDevices = "Devices: ";
                foreach (var device in GameInfoResponse.Titles[0].Devices)
                {
                    GameDevices += device.ToString() + ", ";
                }

                GameDevices = GameDevices.Remove(GameDevices.Length - 2);
                GameGamerscore = "Gamerscore: " + GameInfoResponse.Titles[0].Achievement?.CurrentGamerscore.ToString() +
                                 "/" + GameInfoResponse.Titles[0].Achievement?.TotalGamerscore.ToString();
                try
                {
                    var timePlayed = TimeSpan.FromMinutes(Convert.ToDouble(GameStatsResponse.StatListsCollection[0].Stats[0].Value));
                    var formattedTime = $"{timePlayed.Days} Days, {timePlayed.Hours} Hours and {timePlayed.Minutes} minutes";
                    GameTime = "Time Played: " + formattedTime;
                }
                catch
                {
                    GameTime = "Time Played: Unknown";
                }

            }
            catch
            {
                GameName = "Name: ";
                _snackbarService.Show("Error: Invalid TitleID",
                    $"The TitleID entered is invalid or does not return information from the API",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            SpoofingUpdate = true;
            CurrentlySpoofing = true;
            SpoofingButtonText = "Stop Spoofing";
            SpoofingText = $"Spoofing {GameInfoResponse.Titles[0].Name}";
            await Task.Run(() => Spoofing());

        }

        // TODO: this code seems like it's duplicated in AchievementsViewModel.cs too.
        public async Task Spoofing()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            TimeSpan spoofingTime = stopwatch.Elapsed;
            SpoofingText = $"Spoofing {GameName} For: {spoofingTime.ToString(@"hh\:mm\:ss")}";
            await _xboxRestAPI.Value.SendHeartbeatAsync(HomeViewModel.XUIDOnly, CurrentSpoofingID);
            var i = 0;
            Thread.Sleep(1000);
            SpoofingUpdate = false;
            while (!SpoofingUpdate)
            {
                if (i == 300)
                {
                    await _xboxRestAPI.Value.SendHeartbeatAsync(HomeViewModel.XUIDOnly, CurrentSpoofingID);
                    i = 0;
                }
                else
                {
                    if (SpoofingUpdate)
                    {
                        HomeViewModel.SpoofingStatus = 0;
                        HomeViewModel.SpoofedTitleID = "0";
                        break;
                    }
                    spoofingTime = stopwatch.Elapsed;
                    SpoofingText = $"Spoofing {GameInfoResponse.Titles[0].Name} For: {spoofingTime.ToString(@"hh\:mm\:ss")}";
                    i++;
                }
                Thread.Sleep(1000);
            }
        }

        #endregion

        #region GameSearch
        [ObservableProperty] private List<string> _tSearchGameLinks = new List<string>();
        [ObservableProperty] private string _tSearchText = "";
        [ObservableProperty] private List<string> _tSearchGameNames = new List<string>();
        [ObservableProperty] private string _tSearchGameImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _tSearchGameName = "Name: ";
        [ObservableProperty] private string _tSearchGameTitleID = "";
        [RelayCommand]
        public async Task SearchGame()
        {
            try
            {
                var response = await _taRestApi.Value.SearchAsync(TSearchText);
                TSearchGameNames = response.Item1;
                TSearchGameLinks = response.Item2;
            }
            catch
            {
                _snackbarService.Show("Error: No Results", $"No results were found for {TSearchText}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
        }

        public async void DisplayGameInfo(int index)
        {
            try
            {
                var titleId = await _taRestApi.Value.GetGameLinkAsync(_xboxRestAPI.Value, TSearchGameLinks[index]);
                TSearchGameName = "Name: " + TSearchGameNames[index];
                TSearchGameTitleID = titleId;
                if (titleId == "-1")
                {
                    _snackbarService.Show("Error: TitleID not found",
                        $"The TitleID for {TSearchGameNames[index]} was not available via TrueAchievement Search",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }
            }
            catch
            {
                _snackbarService.Show("Error: No Store Page",
                    $"This Game does not have a store page listed.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
        }
        #endregion

        #region GamertagSearch


        #endregion

    }
}
