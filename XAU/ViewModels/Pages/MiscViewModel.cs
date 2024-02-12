using HtmlAgilityPack;
using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Net.Http;
using System.Text;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;


namespace XAU.ViewModels.Pages;

public partial class MiscViewModel : ObservableObject, INavigationAware
{
    private string _currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);
    private readonly ISnackbarService _snackBarService;
    private readonly TimeSpan _snackBarDuration = TimeSpan.FromSeconds(2);

    public MiscViewModel(ISnackbarService snackBarService)
    {
        _snackBarService = snackBarService;
    }

    public void OnNavigatedTo()
    {
        if (IsInitialized || !HomeViewModel.InitComplete) return;
        InitializeViewModel();
    }

    public void OnNavigatedFrom()
    {
    }

    private void InitializeViewModel()
    {
        if (HomeViewModel.Settings.RegionOverride)
        {
            _currentSystemLanguage = "en-GB";
        }

        IsInitialized = true;
    }
    #region Spoofer

    [ObservableProperty] 
    private string _gameName = "Name: ";
    
    [ObservableProperty] 
    private string _gameTitleId = "Title ID: ";
    
    [ObservableProperty] 
    private string _gamePfn = "PFN: ";
    
    [ObservableProperty] 
    private string _gameType = "Type: ";
    
    [ObservableProperty] 
    private string _gameGamePass = "GamePass: ";
    
    [ObservableProperty] 
    private string _gameDevices = "Devices: ";
    
    [ObservableProperty] 
    private string _gameGamerScore = "GamerScore: ?/?";
    
    [ObservableProperty] 
    private string _gameImage = "pack://application:,,,/Assets/cirno.png";
    
    [ObservableProperty] 
    private string _gameTime = "Time Played: ";
    
    [ObservableProperty] 
    private bool _isInitialized;
    
    [ObservableProperty] 
    private string _currentSpoofingId = "";
    
    [ObservableProperty]
    private string _newSpoofingId = "";
    
    [ObservableProperty] 
    private string _spoofingText = "Spoofing Not Started";
    
    [ObservableProperty] 
    private string _spoofingButtonText = "Start Spoofing";
    
    private bool _spoofingUpdate;
    private bool _currentlySpoofing;
    private dynamic? _gameInfoResponse;
    private dynamic? _gameStatsResponse;

    [RelayCommand]
    private void SpooferButtonClicked()
    {
        if (_currentlySpoofing)
        {
            _spoofingUpdate = true;
            _currentlySpoofing = false;
            SpoofingText = "Spoofing Not Started";
            SpoofingButtonText = "Start Spoofing";
            //reset game info
            GameName = "Name: ";
            GameTitleId = "Title ID: ";
            GamePfn = "PFN: ";
            GameType = "Type: ";
            GameGamePass = "GamePass: ";
            GameDevices = "Devices: ";
            GameGamerScore = "GamerScore: ?/?";
            GameImage = "pack://application:,,,/Assets/cirno.png";
            GameTime = "Time Played: ";
            HomeViewModel.SpoofingStatus = 0;
            return;
        }
        HomeViewModel.SpoofedTitleId = NewSpoofingId;

        if (HomeViewModel.SpoofingStatus == HomeViewModel.SpooofingStatuses.AutoSpoofing)
        {
            HomeViewModel.SpoofingStatus = HomeViewModel.SpooofingStatuses.Spoofing;
            AchievementsViewModel.SpoofingUpdate = true;
        }
        HomeViewModel.SpoofingStatus = HomeViewModel.SpooofingStatuses.Spoofing;
        SpoofGame();
    }

    private async void SpoofGame()
    {
        if (HomeViewModel.XAuth == string.Empty)
        {
            _snackBarService.Show("Error: XAuth is a null string",
                "Couldn't start spoofing",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        var requestBody = new StringContent($"{{\"pfns\":null,\"titleIds\":[\"{NewSpoofingId}\"]}}");
        CurrentSpoofingId = NewSpoofingId;
        _gameInfoResponse = JObject.Parse(await _client
            .PostAsync(
                "https://titlehub.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly +
                ")/titles/batch/decoration/GamePass,Achievement,Stats", requestBody).Result.Content
            .ReadAsStringAsync());
        requestBody = new StringContent($"{{\"arrangebyfield\":\"xuid\",\"xuids\":[\"{HomeViewModel.XUidOnly}\"],\"stats\":[{{\"name\":\"MinutesPlayed\",\"titleId\":\"{NewSpoofingId}\"}}]}}");
        _gameStatsResponse = JObject.Parse(await _client
            .PostAsync("https://userstats.xboxlive.com/batch", requestBody).Result.Content
            .ReadAsStringAsync());

        try
        {
            GameName = "Name: " + _gameInfoResponse.titles[0].name.ToString();
            GameImage = _gameInfoResponse.titles[0].displayImage.ToString();
            GameTitleId = "Title ID: " + _gameInfoResponse.titles[0].titleId.ToString();
            GamePfn = "PFN: " + _gameInfoResponse.titles[0].pfn.ToString();
            GameType = "Type: " + _gameInfoResponse.titles[0].type.ToString();
            GameGamePass = "GamePass: " + _gameInfoResponse.titles[0].gamePass.isGamePass.ToString();
            GameDevices = "Devices: ";
            foreach (var device in _gameInfoResponse.titles[0].devices)
            {
                GameDevices += device.ToString() + ", ";
            }

            GameDevices = GameDevices.Remove(GameDevices.Length - 2);
            GameGamerScore = "GamerScore: " + _gameInfoResponse.titles[0].achievement.currentGamerscore.ToString() +
                             "/" + _gameInfoResponse.titles[0].achievement.totalGamerscore.ToString();
            try
            {
                var timePlayed = TimeSpan.FromMinutes(Convert.ToDouble(_gameStatsResponse.statlistscollection[0].stats[0].value));
                var formattedTime = $"{timePlayed.Days} Days, {timePlayed.Hours} Hours and {timePlayed.Minutes} minutes";
                GameTime = "Time Played: " + formattedTime;}
            catch
            {
                GameTime = "Time Played: Unknown";
            }
               
        }
        catch
        {
            GameName = "Name: ";
            _snackBarService.Show("Error: Invalid TitleID",
                "The TitleID entered is invalid or does not return information from the API",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }

        _spoofingUpdate = true;
        _currentlySpoofing = true;
        SpoofingButtonText = "Stop Spoofing";
        SpoofingText = $"Spoofing {_gameInfoResponse.titles[0].name.ToString()}";
        await Task.Run(() => Spoofing());
    }

    private async Task Spoofing()
    {
        var stopwatch = new Stopwatch();
        stopwatch.Start();
        var spoofingTime = stopwatch.Elapsed;
        SpoofingText = $@"Spoofing {GameName} For: {spoofingTime:hh\:mm\:ss}";
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        var requestBody =
            new StringContent(
                "{\"titles\":[{\"expiration\":600,\"id\":" + CurrentSpoofingId +
                ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
        await _client.PostAsync(
            "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly + ")/devices/current",
            requestBody);
        var i = 0;
        Thread.Sleep(1000);
        _spoofingUpdate = false;
        while (!_spoofingUpdate)
        {
            if (i == 300)
            {
                await _client.PostAsync(
                    "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly +
                    ")/devices/current", requestBody);
                i = 0;
            }
            else
            {
                if (_spoofingUpdate)
                {
                    HomeViewModel.SpoofingStatus = 0;
                    HomeViewModel.SpoofedTitleId = "0";
                    break;
                }
                spoofingTime = stopwatch.Elapsed;
                SpoofingText = $@"Spoofing {_gameInfoResponse?.titles[0].name.ToString()} For: {spoofingTime:hh\:mm\:ss}";
                i++;
            }
            Thread.Sleep(1000);
        }
    }

    #endregion

    #region GameSearch
    [ObservableProperty] private List<string>? _tSearchGameLinks = new();
    [ObservableProperty] private string _tSearchText = "";
    [ObservableProperty] private List<string>? _tSearchGameNames = new();
    [ObservableProperty] private string _tSearchGameImage = "pack://application:,,,/Assets/cirno.png";
    [ObservableProperty] private string _tSearchGameName = "Name: ";
    [ObservableProperty] private string _tSearchGameTitleId = "";
    
    [RelayCommand]
    private async Task SearchGame()
    {
        _client.DefaultRequestHeaders.Clear();
        var searchQueryText = Uri.EscapeDataString(TSearchText);
        searchQueryText = searchQueryText.Replace("%20", "+");
        var response = await _client.GetAsync($"https://www.trueachievements.com/searchresults.aspx?search={searchQueryText}");
        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var table = doc.DocumentNode.Descendants("table").FirstOrDefault(x => x.HasClass("maintable"));
        List<string> tempLinks;
        try
        {
            tempLinks = table!.Descendants("a").Select(a => a.GetAttributeValue("href", null)).Where(h => !string.IsNullOrEmpty(h)).ToList();
        }
        catch
        {
            _snackBarService.Show("Error: No Results",$"No results were found for {TSearchText}",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }
            
        var tempNames = table.Descendants("td")
            .Where(td => td.HasClass("gamerwide"))
            .Select(td => td.InnerText.Trim())
            .ToList();
        tempLinks.RemoveAt(0);
        tempLinks.RemoveAt(0);
        for (var i = 0; i < tempLinks.Count; i++)
        {
            tempLinks[i] = "https://www.trueachievements.com" + tempLinks[i];
            tempLinks[i] = tempLinks[i].Replace("/achievements", "/price");
            if (i >0)
            {
                if (tempLinks[i - 1] == tempLinks[i])
                {
                    tempLinks.RemoveAt(i);
                    i--;
                    continue;
                }
                    
            }

            if (tempLinks[i].Contains("/game/")) continue;
            
            tempLinks.RemoveAt(i);
            tempLinks.RemoveAt(i);
            tempNames?.RemoveAt(i);
            i--;
        }
        TSearchGameLinks = tempLinks;
        TSearchGameNames = tempNames;
    }

    public async void DisplayGameInfo(int index)
    {
        var response = await _client.GetAsync(TSearchGameLinks?[index]);
        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        string productId;
        try
        {
            productId = doc.DocumentNode.SelectSingleNode("//a[@class='price']").Attributes["href"].Value;
        }
        catch
        {
            _snackBarService.Show("Error: No Store Page",
                "This Game does not have a store page listed.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }

        productId = productId.Replace("/ext?u=", "");
        productId = System.Web.HttpUtility.UrlDecode(productId);
        productId = productId[..productId.LastIndexOf('&')];
        productId = productId.Split('/').Last();
        if (productId.Contains('-'))
        {
            TSearchGameName = "Name: " + TSearchGameNames?[index];
            TSearchGameTitleId = Convert.ToInt32(productId[^8..], 16).ToString();
        }
        else
        {
            _client.DefaultRequestHeaders.Clear();
            var productIDsConverted = new StringContent("{\"Products\":[\"" +productId+ "\"]}");
            var titleIDsResponse = await _client.PostAsync(
                "https://catalog.gamepass.com/products?market=GB&language=en-GB&hydration=PCHome",
                productIDsConverted);
            var titleIDsContent = await titleIDsResponse.Content.ReadAsStringAsync();
            var jsonTitleIDs = (dynamic)JObject.Parse(titleIDsContent);
            var xboxTitleId = jsonTitleIDs.Products[$"{productId}"].XboxTitleId;
            
            //here is some super dumb shit to handle bundles
            if (xboxTitleId == null)
            {
                foreach (var product in jsonTitleIDs.Products)
                {
                    foreach (var title in product)
                    {
                        if (!title.ToString().Contains("\"ProductType\": \"Game\",") || title.XboxTitleId == null)
                            continue;
                        xboxTitleId = title.XboxTitleId;
                        break;
                    }
                }
            }

            TSearchGameName = "Name: " + TSearchGameNames?[index];
            if (xboxTitleId != null) TSearchGameTitleId = xboxTitleId;
        }

    }
    #endregion

}