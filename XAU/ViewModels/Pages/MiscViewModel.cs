using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Animation;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.Views.Windows;

namespace XAU.ViewModels.Pages
{
    public partial class MiscViewModel : ObservableObject, INavigationAware
    {
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        HttpClient client = new HttpClient(handler);
        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

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

        [ObservableProperty] private int _spoofingStatus = 0; //0=NotSpoofing, 1 =Spoofing, 2 = AutoSpoofing
        [ObservableProperty] private string _gameName = "Name: ";
        [ObservableProperty] private string _gameTitleID = "Title ID: ";
        [ObservableProperty] private string _gamePFN = "PFN: ";
        [ObservableProperty] private string _gameType = "Type: ";
        [ObservableProperty] private string _gameGamepass = "Gamepass: ";
        [ObservableProperty] private string _gameDevices = "Devices: ";
        [ObservableProperty] private string _gameGamerscore = "Gamerscore: ?/?";
        [ObservableProperty] private string _gameImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gameTime = "Time Played: ";
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _currentSpoofingID = "";
        [ObservableProperty] private string _newSpoofingID = "";
        [ObservableProperty] private string _spoofingText = "Spoofing Not Started";
        [ObservableProperty] private string _spoofingButtonText = "Start Spoofing";
        private bool SpoofingUpdate = false;
        private bool CurrentlySpoofing = false;
        private dynamic GameInfoResponse;
        private dynamic GameStatsResponse;

        [RelayCommand]
        public void SpooferButtonClicked()
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
                return;
            }
            switch (SpoofingStatus)
            {
                case 0:
                    SpoofGame(1);
                    break;
                case 1:
                    if (CurrentSpoofingID != NewSpoofingID)
                        SpoofGame(1);
                    break;
                case 2:
                    if (CurrentSpoofingID != NewSpoofingID)
                    {
                        SpoofGame(1);
                    }

                    break;
            }
        }

        public async void SpoofGame(int SpoofingSource)
        {

            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            StringContent requestbody = new StringContent($"{{\"pfns\":null,\"titleIds\":[\"{NewSpoofingID}\"]}}");
            CurrentSpoofingID = NewSpoofingID;
            GameInfoResponse = (dynamic)JObject.Parse(await client
                .PostAsync(
                    "https://titlehub.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly +
                    ")/titles/batch/decoration/GamePass,Achievement,Stats", requestbody).Result.Content
                .ReadAsStringAsync());
            requestbody =
                new StringContent($"{{\"arrangebyfield\":\"xuid\",\"xuids\":[\"{HomeViewModel.XUIDOnly}\"],\"stats\":[{{\"name\":\"MinutesPlayed\",\"titleId\":\"{NewSpoofingID}\"}}]}}");
            GameStatsResponse = (dynamic)JObject.Parse(await client
                .PostAsync("https://userstats.xboxlive.com/batch", requestbody).Result.Content
                .ReadAsStringAsync());

            try
            {
                GameName = "Name: " + GameInfoResponse.titles[0].name.ToString();
                GameImage = GameInfoResponse.titles[0].displayImage.ToString();
                GameTitleID = "Title ID: " + GameInfoResponse.titles[0].titleId.ToString();
                GamePFN = "PFN: " + GameInfoResponse.titles[0].pfn.ToString();
                GameType = "Type: " + GameInfoResponse.titles[0].type.ToString();
                GameGamepass = "Gamepass: " + GameInfoResponse.titles[0].gamePass.isGamePass.ToString();
                GameDevices = "Devices: ";
                foreach (var device in GameInfoResponse.titles[0].devices)
                {
                    GameDevices += device.ToString() + ", ";
                }

                GameDevices = GameDevices.Remove(GameDevices.Length - 2);
                GameGamerscore = "Gamerscore: " + GameInfoResponse.titles[0].achievement.currentGamerscore.ToString() +
                                 "/" + GameInfoResponse.titles[0].achievement.totalGamerscore.ToString();
                GameTime = "Time Played: " + TimeSpan.FromMinutes(Convert.ToDouble(GameStatsResponse.statlistscollection[0].stats[0].value)).ToString(@"hh\:mm") ;
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
            SpoofingText = $"Spoofing {GameInfoResponse.titles[0].name.ToString()}";
            Task.Run(() => Spoofing());

        }

        public async Task Spoofing()
        {
            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            TimeSpan spoofingTime = stopwatch.Elapsed;
            SpoofingText = $"Spoofing {GameName} For: {spoofingTime.ToString(@"hh\:mm\:ss")}";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            var requestbody =
                new StringContent(
                    "{\"titles\":[{\"expiration\":600,\"id\":" + CurrentSpoofingID +
                    ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
            await client.PostAsync(
                "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/devices/current",
                requestbody);
            var i = 0;
            Thread.Sleep(1000);
            SpoofingUpdate = false;
            while (!SpoofingUpdate)
            {
                if (i == 300)
                {
                    await client.PostAsync(
                        "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly +
                        ")/devices/current", requestbody);
                    i = 0;
                }
                else
                {
                    if (SpoofingUpdate)
                    {
                        break;
                    }
                    spoofingTime = stopwatch.Elapsed;
                    SpoofingText = $"Spoofing {GameInfoResponse.titles[0].name.ToString()} For: {spoofingTime.ToString(@"hh\:mm\:ss")}";
                    i++;
                }
                Thread.Sleep(1000);
            }
        }

        #endregion


    }
}
