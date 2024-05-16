using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using Wpf.Ui.Services;


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
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH, System.Globalization.CultureInfo.CurrentCulture.Name));

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
        [ObservableProperty] private string _gameImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gameTime = "Time Played: ";
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _currentSpoofingID = "";
        [ObservableProperty] private string _newSpoofingID = "";
        [ObservableProperty] private string _spoofingText = "Spoofing Not Started";
        [ObservableProperty] private string _spoofingButtonText = "Start Spoofing";
        private bool SpoofingUpdate = false;
        private bool CurrentlySpoofing = false;
        private GameTitle GameInfoResponse;
        private GameStats GameStatsResponse;

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

            try
            {
                GameName = "Name: " + GameInfoResponse.Titles[0].Name.ToString();
                GameImage = GameInfoResponse.Titles[0].DisplayImage.ToString();
                GameTitleID = "Title ID: " + GameInfoResponse.Titles[0].TitleId.ToString();
                GamePFN = "PFN: " + GameInfoResponse.Titles[0].Pfn.ToString();
                GameType = "Type: " + GameInfoResponse.Titles[0].Type.ToString();
                GameGamepass = "Gamepass: " + GameInfoResponse.Titles[0].GamePass.IsGamePass.ToString();
                GameDevices = "Devices: ";
                foreach (var device in GameInfoResponse.Titles[0].Devices)
                {
                    GameDevices += device.ToString() + ", ";
                }

                GameDevices = GameDevices.Remove(GameDevices.Length - 2);
                GameGamerscore = "Gamerscore: " + GameInfoResponse.Titles[0].Achievement.CurrentGamerscore.ToString() +
                                 "/" + GameInfoResponse.Titles[0].Achievement.TotalGamerscore.ToString();
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
            SpoofingText = $"Spoofing {GameInfoResponse.Titles[0].Name.ToString()}";
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
                    SpoofingText = $"Spoofing {GameInfoResponse.Titles[0].Name.ToString()} For: {spoofingTime.ToString(@"hh\:mm\:ss")}";
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
            client.DefaultRequestHeaders.Clear();
            var SearchQuerytext = Uri.EscapeDataString(TSearchText);
            SearchQuerytext = SearchQuerytext.Replace("%20", "+");
            var response = await client.GetAsync($"https://www.trueachievements.com/searchresults.aspx?search={SearchQuerytext}");
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);

            var table = doc.DocumentNode.Descendants("table").FirstOrDefault(x => x.HasClass("maintable"));
            var templinks = new List<string>();
            try
            {
                templinks = table.Descendants("a").Select(a => a.GetAttributeValue("href", null)).Where(h => !string.IsNullOrEmpty(h)).ToList();
            }
            catch
            {
                _snackbarService.Show("Error: No Results", $"No results were found for {TSearchText}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            var tempnames = table.Descendants("td")
                .Where(td => td.HasClass("gamerwide"))
                .Select(td => td.InnerText.Trim())
                .ToList();
            templinks.RemoveAt(0);
            templinks.RemoveAt(0);
            for (var i = 0; i < templinks.Count; i++)
            {
                templinks[i] = "https://www.trueachievements.com" + templinks[i];
                templinks[i] = templinks[i].Replace("/achievements", "/price");
                if (i > 0)
                {
                    if (templinks[i - 1] == templinks[i])
                    {
                        templinks.RemoveAt(i);
                        i--;
                        continue;
                    }

                }
                if (!templinks[i].Contains("/game/"))
                {
                    templinks.RemoveAt(i);
                    templinks.RemoveAt(i);
                    tempnames.RemoveAt(i);
                    i--;
                }
            }
            TSearchGameLinks = templinks;
            TSearchGameNames = tempnames;
        }

        public async void DisplayGameInfo(int index)
        {
            var response = await client.GetAsync(TSearchGameLinks[index]);
            var html = await response.Content.ReadAsStringAsync();
            var doc = new HtmlDocument();
            doc.LoadHtml(html);
            var ProductID = "";
            try
            {
                ProductID = doc.DocumentNode.SelectSingleNode("//a[@class='price']").Attributes["href"].Value;
            }
            catch
            {
                _snackbarService.Show("Error: No Store Page",
                    $"This Game does not have a store page listed.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            ProductID = ProductID.Replace("/ext?u=", "");
            ProductID = System.Web.HttpUtility.UrlDecode(ProductID);
            ProductID = ProductID.Substring(0, ProductID.LastIndexOf('&'));
            ProductID = ProductID.Split('/').Last();
            if (ProductID.Contains("-"))
            {
                TSearchGameName = "Name: " + TSearchGameNames[index];
                TSearchGameTitleID = Convert.ToInt32(ProductID.Substring(ProductID.Length - 8), 16).ToString();
            }
            else
            {
                var TitleIDsContent = await _xboxRestAPI.Value.GetTitleIdsFromGamePass(ProductID);
                var JsonTitleIDs = (dynamic)JObject.Parse(TitleIDsContent);
                var xboxTitleId = JsonTitleIDs.Products[$"{ProductID}"].XboxTitleId;
                //here is some super dumb shit to handle bundles
                if (xboxTitleId == null)
                {
                    foreach (var Product in JsonTitleIDs.Products)
                    {
                        foreach (var Title in Product)
                        {
                            if (Title.ToString().Contains("\"ProductType\": \"Game\",") && Title.XboxTitleId != null)
                            {
                                xboxTitleId = Title.XboxTitleId;
                                break;
                            }
                        }
                    }
                }

                TSearchGameName = "Name: " + TSearchGameNames[index];
                TSearchGameTitleID = xboxTitleId;
            }

        }
        #endregion

        #region GamertagSearch


        #endregion

    }
}
