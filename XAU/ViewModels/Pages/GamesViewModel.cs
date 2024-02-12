using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.Net.Http;
using Wpf.Ui.Controls;
using XAU.Models;
using XAU.Views.Pages;
using XAU.Views.Windows;
using static Wpf.Ui.Common.SymbolRegular;
using static Wpf.Ui.Controls.ControlAppearance;

namespace XAU.ViewModels.Pages;

public partial class GamesViewModel(ISnackbarService snackBarService) : ObservableObject, INavigationAware
{
    [ObservableProperty] 
    private string? _xUidOverride = "0";
    
    [ObservableProperty] 
    private ObservableCollection<Game> _games = [];
    
    [ObservableProperty] 
    private ObservableCollection<Game> _gamesPaged = [];
    
    [ObservableProperty] 
    private string _searchLabel = "Search 0 Games";
    
    [ObservableProperty] 
    private GridLength _gamesListHeight = new(0, GridUnitType.Star);
    
    [ObservableProperty] 
    private GridLength _loadingHeight = new(1, GridUnitType.Star);
    
    [ObservableProperty] 
    private double _loadingSize = 200;
    
    [ObservableProperty] 
    private string _searchText = "";
    
    [ObservableProperty] 
    private List<string> _filterOptions = ["All", "Xbox One/Series", "PC", "Xbox 360", "Win32"];
    
    [ObservableProperty] 
    private int _filterIndex;
    
    [ObservableProperty] 
    private int _numPages;
    
    [ObservableProperty] 
    private ObservableCollection<string> _pageOptions = [];
    
    [ObservableProperty] 
    private int _currentPage;
    
    [ObservableProperty] 
    private bool _isInitialized;

    private string _currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);
    private dynamic _gamesResponse = new JObject();
    private bool _pageReset = true;

    private readonly TimeSpan _snackBarDuration = TimeSpan.FromSeconds(2);

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
        XUidOverride = HomeViewModel.XUidOnly;
        GetGamesList();
        IsInitialized = true;
        if (!HomeViewModel.Settings.RegionOverride) return;
        _currentSystemLanguage = "en-GB";
    }

    [RelayCommand]
    private void OnGetGamesList()
    {
        GetGamesList();
    }

    private async void GetGamesList()
    {
        Games.Clear();
        GamesPaged.Clear();
        LoadingStart();
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        var responseString = await _client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + XUidOverride + ")/titles/titleHistory/decoration/Achievement,scid?maxItems=10000");
        _gamesResponse = JObject.Parse(responseString);
        LoadGames();
    }

    private void LoadGames()
    {
        if (SearchText.Length > 0)
        {
            SearchAndFilterGames();
        }
        else
        {
            FilterGames();
        }
    }
    
    public void OpenAchievements(string? index)
    {
        if (index == null)
        {
            return;
        }
        
        AchievementsViewModel.TitleId = _gamesResponse.titles[int.Parse(index)].titleId.ToString();
        AchievementsViewModel.IsSelectedGame360 = _gamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Xbox360") || _gamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Mobile");
        AchievementsViewModel.NewGame = true;
        MainWindow.MainNavigationService.Navigate(typeof(AchievementsPage));
    }
    
    [RelayCommand]
    public void SearchAndFilterGames()
    {
        if (!IsInitialized)
        {
            return;
        }
        
        if (SearchText.Length == 0)
        {
            snackBarService.Show("Error", "Please Enter Query Text", Danger, new SymbolIcon(ErrorCircle24), _snackBarDuration);
            return;
        }

        ProcessGamesList(true);
    }

    private void ProcessGamesList(bool useSearch = false)
    {
        Games.Clear();
        GamesPaged.Clear();
        LoadingStart();

        if (FilterIndex != 0)
        {
            switch (FilterIndex)
            {
                case 1:
                {
                    for (var i = 0; i < _gamesResponse.titles.Count; i++)
                    {
                        var title = _gamesResponse.titles[i];
                        if (!title.devices.ToString().Contains("XboxSeries") &&
                            !title.devices.ToString().Contains("XboxOne")) continue;
                        if (useSearch && !title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                            continue;
                        AddGame(i);
                    }
                    break;
                }
                case 2:
                {
                    for (var i = 0; i < _gamesResponse.titles.Count; i++)
                    {
                        var title = _gamesResponse.titles[i];
                        if (!title.devices.ToString().Contains("PC")) continue;
                        if (useSearch && !title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                            continue;
                        AddGame(i);
                    }
                    break;
                }
                case 3:
                {
                    for (var i = 0; i < _gamesResponse.titles.Count; i++)
                    {
                        var title = _gamesResponse.titles[i];
                        if (!title.devices.ToString().Contains("Xbox360")) continue;
                        if (useSearch && !title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                            continue;
                        AddGame(i);
                    }
                    break;
                }
                case 4:
                {
                    for (var i = 0; i < _gamesResponse.titles.Count; i++)
                    {
                        var title = _gamesResponse.titles[i];
                        if (!title.devices.ToString().Contains("Win32")) continue;
                        if (useSearch && !title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                            continue;
                        AddGame(i);
                    }
                    break;
                }
            }
        }
        else
        {
            for (var i = 0; i < _gamesResponse.titles.Count; i++)
            {
                var title = _gamesResponse.titles[i];
                if (useSearch && !title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                    continue;
                AddGame(i);
            }
        }

        LoadingEnd();
        SearchLabel = $"Search {_gamesResponse.titles.Count.ToString()} Games";
        if (!Games.Any())
        {
            snackBarService.Show("Error", "No Games Found", Danger, new SymbolIcon(ErrorCircle24), _snackBarDuration);
            NumPages = 0;
            return;
        }
        
        NumPages = (int)Math.Ceiling(Games.Count / 252.0);
        _pageReset = true;
        PageOptions.Clear();
        
        for (var i = 1; i <= NumPages; i++)
        {
            PageOptions.Add(i.ToString());
        }
        
        _pageReset = true;
        CurrentPage = 0;
        GamesPaged.Clear();
        
        for (var i = 252 * CurrentPage; i < 252*(CurrentPage+1); i++)
        {
            if (Games.Count > i)
            {
                GamesPaged.Add(Games[i]);
            }
        }
    }
    
    
    [RelayCommand]
    public void FilterGames()
    {
        if (!IsInitialized)
        {
            return;
        }

        if (SearchText.Length > 0)
        {
            SearchAndFilterGames();
            return;
        }

        ProcessGamesList();
    }

    private void AddGame(int index)
    {
        var title = _gamesResponse.titles[index];
        var editedImage = title.displayImage.ToString();
        
        if (editedImage.Contains("store-images.s-microsoft.com"))
        {
            editedImage += "?w=256&h=256&format=jpg";
        }
        
        Games.Add(new Game
        {
            Title = title.name.ToString(),
            CurrentAchievements = title.achievement.currentAchievements.ToString(),
            GamerScore = title.achievement.currentGamerscore.ToString() + "/" +
                         title.achievement.totalGamerscore.ToString(),
            Progress = title.achievement.progressPercentage.ToString(),
            Image = editedImage,
            Index = index.ToString()
        });
    }

    [RelayCommand]
    public void PageChanged()
    {
        if (_pageReset)
        {
            _pageReset = false;
            return;
        }
        
        GamesPaged.Clear();
        LoadingStart();
        
        for (var i = 252 * CurrentPage; i < 252 * (CurrentPage+1); i++)
        {
            if (Games.Count <= i) continue;
            GamesPaged.Add(Games[i]);
        }

        LoadingEnd();
    }

    private void LoadingStart()
    {
        LoadingSize = 200;
        GamesListHeight = new GridLength(0, GridUnitType.Star);
        LoadingHeight = new GridLength(1, GridUnitType.Star);
    }

    private void LoadingEnd()
    {
        GamesListHeight = new GridLength(1, GridUnitType.Star);
        LoadingHeight = new GridLength(0, GridUnitType.Star);
        LoadingSize = 0;
    }

    public void CopyToClipboard(string? index)
    {
        if (index == null)
        {
            return;
        }
        
        var titleId = _gamesResponse.titles[int.Parse(index)].titleId.ToString();
        var title = _gamesResponse.titles[int.Parse(index)].name.ToString();
        Clipboard.SetDataObject(_gamesResponse.titles[int.Parse(index)].titleId.ToString());
        snackBarService.Show("TitleID Copied", $"Copied the title ID of {title.ToString()} to clipboard\nTitleID: {titleId.ToString()}", Success, new SymbolIcon(ClipboardCheckmark24), _snackBarDuration);
    }
}