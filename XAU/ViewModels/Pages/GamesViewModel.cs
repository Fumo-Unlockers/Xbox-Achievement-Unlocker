using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using Wpf.Ui.Controls;
using XAU.Models;
using XAU.Views.Pages;

namespace XAU.ViewModels.Pages;

public partial class GamesViewModel(ISnackbarService snackbarService, INavigationService navigationService)
    : ObservableObject, INavigationAware
{
    [ObservableProperty] 
    private long _xuidOverride;
    
    [ObservableProperty] 
    private ObservableCollection<Game> _games = [];
    
    [ObservableProperty] 
    private string _searchLabel = "Search 0 Games";
    
    [ObservableProperty] 
    private ObservableCollection<string> _searchGameNames = [];

    [ObservableProperty]
    private bool _isLoadingVisible = true;

    [ObservableProperty] 
    private string _loadingText = string.Empty;
    
    [ObservableProperty]
    private bool _isGamesListVisible;
    
    [ObservableProperty] 
    private string _searchText = "";
    
    [ObservableProperty] 
    private Dictionary<string, string> _filterOptions = new()
    {
        { "All", "all" },
        { "Xbox One", "xboxone" },
        { "Xbox Series", "xboxseries" },
        { "PC", "pc" }, 
        { "Xbox 360", "xbox360" }, 
        { "Win32", "win32" }
    };
    
    [ObservableProperty]
    private int _filterIndex;
    
    [ObservableProperty] 
    private bool _isInitialized;

    private string _currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);
    private dynamic _gamesResponse = new JObject();

    public void OnNavigatedTo()
    {
        if (!IsInitialized && HomeViewModel.InitComplete)
        {
            InitializeViewModel();
        }
    }

    public void OnNavigatedFrom()
    {
    }

    private async void InitializeViewModel()
    {
        if (!long.TryParse(HomeViewModel.XUIDOnly, out var parsedResult))
        {
            snackbarService.Show(
                "Error",
                "Invalid XUID.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(3)
            );
            return;
        }
        
        XuidOverride = parsedResult;
        await GetGamesList();
        IsInitialized = true;
        if (HomeViewModel.Settings.RegionOverride)
        {
            _currentSystemLanguage = "en-GB";
        }
    }
    
    [RelayCommand]
    private async Task GetGamesList()
    {
        Games.Clear();
        StartLoading();
        LoadingText = "Parsing games...";
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        var responseString = await _client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + XuidOverride + ")/titles/titleHistory/decoration/Achievement,scid?maxItems=10000");
        _gamesResponse = JObject.Parse(responseString);
        LoadingText = "Filtering games...";
        FilterGames(true);
    }

    [RelayCommand]
    public void OpenAchievements(string? index)
    {
        AchievementsViewModel.TitleID = _gamesResponse.titles[int.Parse(index)].titleId.ToString();
        AchievementsViewModel.IsSelectedGame360 = _gamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Xbox360") || _gamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Mobile");
        AchievementsViewModel.NewGame = true;
        navigationService.Navigate(typeof(AchievementsPage));
    }
    
    [RelayCommand]
    public void SearchAndFilterGames()
    {
        if (SearchText.Length == 0)
        {
            FilterGames();
            return;
        }

        Games.Clear();
        SearchGameNames.Clear();
        StartLoading();

        for (var i = 0; i < _gamesResponse.titles.Count; i++)
        {
            var title = _gamesResponse.titles[i];
            var type = FilterOptions.ElementAt(FilterIndex).Value;
            if (!title.devices.ToString().ToLower().Contains(type) && type != "all")
            {
                continue;
            }

            if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
            {
                continue;
            }
            
            AddGame(i);
        }

        SearchLabel = $"Search {Games.Count.ToString()} Games";
        if (!Games.Any())
        {
            snackbarService.Show(
                "Error",
                "No Games Found.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(3)
            );
        }
        
        EndLoading();
    }

    [RelayCommand]
    public void FilterGames(bool skipInitCheck = false)
    {
        if (!IsInitialized && !skipInitCheck)
        {
            return;
        }
        
        if (SearchText.Length > 0)
        {
            SearchAndFilterGames();
            return;
        }

        StartLoading();
        Games.Clear();
        SearchGameNames.Clear();
        
        for (var i = 0; i < _gamesResponse.titles.Count; i++)
        {
            var title = _gamesResponse.titles[i];
            var type = FilterOptions.ElementAt(FilterIndex).Value;
            if (!title.devices.ToString().ToLower().Contains(type) && type != "all")
            {
                continue;
            }
            
            AddGame(i);
        }

        SearchLabel = $"Search {Games.Count} Games";
        if (!Games.Any())
        {
            snackbarService.Show(
                "Error",
                "No Games Found",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(3)
            );
        }
        
        EndLoading();
    }

    private void AddGame(int index)
    {
        var title = _gamesResponse.titles[index];
        var editedImage = title.displayImage.ToString();
        if (editedImage.Contains("store-images.s-microsoft.com"))
        {
            editedImage += "?w=256&h=256&format=jpg";
        }

        var gameTitle = title.name.ToString();
        Games.Add(new Game
        {
            Title = gameTitle,
            CurrentAchievements = title.achievement.currentAchievements.ToString(),
            GamerScore = $"{title.achievement.currentGamerscore} / {title.achievement.totalGamerscore}",
            Progress = title.achievement.progressPercentage.ToString(),
            Image = editedImage,
            Index = index.ToString()
        });

        if (gameTitle != null)
        {
            SearchGameNames.Add(new string(gameTitle));
        }
    }
    
    private void StartLoading()
    {
        IsLoadingVisible = true;
        IsGamesListVisible = false;
    }

    private void EndLoading()
    {
        IsLoadingVisible = false;
        IsGamesListVisible = true;
    }

    public void CopyToClipboard(string? index)
    {
        if (!int.TryParse(index, out var parsedResult))
        {
            snackbarService.Show("Error",
                "Invalid game index.",
                ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24),
                TimeSpan.FromSeconds(3)
            );
            return;
        }
        
        var title = _gamesResponse.titles[parsedResult];
        var titleId = title.titleId.ToString();
        var titleName = title.name.ToString();
        Clipboard.SetDataObject(titleId);
        snackbarService.Show("TitleID Copied",
            $"Copied the title ID of {titleName.ToString()} to clipboard\nTitleID: {titleId.ToString()}",
            ControlAppearance.Success,
            new SymbolIcon(SymbolRegular.ClipboardCheckmark24),
            TimeSpan.FromSeconds(3)
        );
    }
}