using System.Collections.ObjectModel;
using System.ComponentModel;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.Views.Pages;
namespace XAU.ViewModels.Pages
{
    public partial class GamesViewModel(ISnackbarService snackbarService, INavigationService navigationService) : ObservableObject, INavigationAware, INotifyPropertyChanged
    {
        [ObservableProperty] private string _xuidOverride = "0";
        [ObservableProperty] private ObservableCollection<Game> _games = new ObservableCollection<Game>();
        [ObservableProperty] private ObservableCollection<Game> _gamesPaged = new ObservableCollection<Game>();
        [ObservableProperty] private string _searchLabel = "Search 0 Games";
        [ObservableProperty] private GridLength _gamesListHeight = new GridLength(0, GridUnitType.Star);
        [ObservableProperty] private GridLength _loadingHeight = new GridLength(1, GridUnitType.Star);
        [ObservableProperty] private double _loadingSize = 200;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private List<string> _filterOptions = new List<string>() { "All", "Xbox One/Series", "PC", "Xbox 360", "Win32", "Incomplete Games" };
        [ObservableProperty] private int _filterIndex = 0;
        [ObservableProperty] private int _numPages = 0;
        [ObservableProperty] private ObservableCollection<string> _pageOptions = new ObservableCollection<string>();
        [ObservableProperty] private int _currentPage = 0;
        [ObservableProperty] private bool _isInitialized = false;

        TitlesList GamesResponse = new TitlesList();
        public bool PageReset = true;


        public class Game
        {
            public required string Title { get; set; }
            public required string Image { get; set; }
            public required string Gamerscore { get; set; }
            public required string CurrentAchievements { get; set; }
            public required string Progress { get; set; }
            public required string Index { get; set; }

        }

        // TODO: this needs to be updated if language changes
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH));

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService = snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        public async void OnNavigatedTo()
        {
            if (!IsInitialized && HomeViewModel.InitComplete)
                await InitializeViewModel();
        }

        public void OnNavigatedFrom()
        {
        }

        private async Task InitializeViewModel()
        {
            XuidOverride = HomeViewModel.XUIDOnly;

            IsInitialized = true;
            await GetGamesList();

        }

        [RelayCommand]
        private async Task GetGamesList()
        {
            if (string.IsNullOrWhiteSpace(XuidOverride) || string.IsNullOrEmpty(XuidOverride))
            {
                _snackbarService.Show(
                    "Error",
                    "XUID Override cannot be empty.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24),
                    _snackbarDuration
                );
                return;
            }

            Games.Clear();
            GamesPaged.Clear();
            LoadingStart();
            GamesResponse = await _xboxRestAPI.Value.GetGamesListAsync(XuidOverride) ?? new TitlesList();
            LoadGame();
        }

        private void LoadGame()
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
        public async Task OpenAchievements(string index)
        {
            AchievementsViewModel.TitleID = GamesResponse.Titles[int.Parse(index)].TitleId;
            AchievementsViewModel.IsSelectedGame360 = GamesResponse.Titles[int.Parse(index)].Devices.Contains("Xbox360") || GamesResponse.Titles[int.Parse(index)].Devices.Contains("Mobile");
            AchievementsViewModel.NewGame = true;
            navigationService.Navigate(typeof(AchievementsPage));
            await Task.CompletedTask;
        }
        [RelayCommand]
        public void SearchAndFilterGames()
        {
            Games.Clear();
            GamesPaged.Clear();
            LoadingStart();
            if (FilterIndex != 0)
            {
                switch (FilterIndex)
                {
                    case 1:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("XboxSeries") || GamesResponse.Titles[i].Devices.Contains("XboxOne"))
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 2:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("PC"))
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 3:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("Xbox360"))
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 4:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("Win32"))
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            };
                        }
                        break;
                    case 5:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (double.TryParse(GamesResponse.Titles[i].Achievement.ProgressPercentage.ToString(), out double progress) && progress < 100)
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }

                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < GamesResponse.Titles.Count; i++)
                {
                    var title = GamesResponse.Titles[i];
                    if (!title.Name.ToLower().Contains(SearchText.ToLower()))
                        continue;
                    AddGame(i);

                }
            }

            LoadingEnd();
            SearchLabel = $"Search {GamesResponse.Titles.Count.ToString()} Games";
            if (Games.Count() == 0)
            {
                _snackbarService.Show("Error", $"No Games Found", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                NumPages = 0;
                return;
            }
            NumPages = (int)Math.Ceiling(Games.Count / 252.0);
            PageReset = true;
            PageOptions.Clear();
            for (int i = 1; i <= NumPages; i++)
            {
                PageOptions.Add(i.ToString());
            }
            PageReset = true;
            CurrentPage = 0;
            GamesPaged.Clear();
            for (int i = ((252 * CurrentPage)); i < (252 * (CurrentPage + 1)); i++)
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
            GamesPaged.Clear();
            LoadingStart();
            Games.Clear();
            if (FilterIndex != 0)
            {
                switch (FilterIndex)
                {
                    case 1:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("XboxSeries") || GamesResponse.Titles[i].Devices.Contains("XboxOne"))
                                AddGame(i);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("PC"))
                                AddGame(i);
                        }
                        break;
                    case 3:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("Xbox360"))
                                AddGame(i);
                        }
                        break;
                    case 4:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (GamesResponse.Titles[i].Devices.Contains("Win32"))
                                AddGame(i);
                        }
                        break;
                    case 5:
                        for (int i = 0; i < GamesResponse.Titles.Count; i++)
                        {
                            if (double.TryParse(GamesResponse.Titles[i].Achievement.ProgressPercentage.ToString(), out double progress) && progress < 100)
                            {
                                if (!GamesResponse.Titles[i].Name.ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < GamesResponse.Titles.Count; i++)
                {
                    AddGame(i);
                }
            }

            LoadingEnd();
            SearchLabel = $"Search {GamesResponse.Titles.Count.ToString()} Games";
            if (Games.Count() == 0)
            {
                _snackbarService.Show("Error", $"No Games Found", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                NumPages = 0;
                return;
            }
            NumPages = (int)Math.Ceiling(Games.Count / 252.0);
            PageReset = true;
            PageOptions.Clear();
            for (int i = 1; i <= NumPages; i++)
            {
                PageOptions.Add(i.ToString());
            }
            PageReset = true;
            CurrentPage = 0;
            GamesPaged.Clear();
            for (int i = ((252 * CurrentPage)); i < (252 * (CurrentPage + 1)); i++)
            {
                if (Games.Count > i)
                {
                    GamesPaged.Add(Games[i]);
                }
            }
        }

        private void AddGame(int index)
        {
            var title = GamesResponse.Titles[index];
            var EditedImage = title.DisplayImage != null ? title.DisplayImage.ToString() : "pack://application:,,,/Assets/cirno.png";
            if (EditedImage.Contains("store-images.s-microsoft.com"))
            {
                EditedImage = EditedImage + "?w=256&h=256&format=jpg";
            }
            Games.Add(new Game()
            {
                Title = title.Name.ToString(),
                CurrentAchievements = title.Achievement.CurrentAchievements.ToString(),
                Gamerscore = title.Achievement.CurrentGamerscore.ToString() + "/" +
                             title.Achievement.TotalGamerscore.ToString(),
                Progress = title.Achievement.ProgressPercentage.ToString(),
                Image = EditedImage, //"pack://application:,,,/Assets/cirno.png", //
                Index = index.ToString()
            });
        }

        [RelayCommand]
        public void PageChanged()
        {
            if (PageReset)
            {
                PageReset = false;
                return;
            }
            GamesPaged.Clear();
            LoadingStart();
            for (int i = ((252 * (CurrentPage))); i < (252 * (CurrentPage + 1)); i++)
            {
                if (Games.Count > i)
                {
                    GamesPaged.Add(Games[i]);
                }
            }
            LoadingEnd();
        }

        public void LoadingStart()
        {
            LoadingSize = 200;
            GamesListHeight = new GridLength(0, GridUnitType.Star);
            LoadingHeight = new GridLength(1, GridUnitType.Star);
        }

        public void LoadingEnd()
        {
            GamesListHeight = new GridLength(1, GridUnitType.Star);
            LoadingHeight = new GridLength(0, GridUnitType.Star);
            LoadingSize = 0;
        }

        public void CopyToClipboard(string index)
        {
            var titleid = GamesResponse.Titles[int.Parse(index)].TitleId.ToString();
            var title = GamesResponse.Titles[int.Parse(index)].Name.ToString();
            Clipboard.SetDataObject(GamesResponse.Titles[int.Parse(index)].TitleId.ToString());
            _snackbarService.Show("TitleID Copied", $"Copied the title ID of {title.ToString()} to clipboard\nTitleID: {titleid.ToString()}", ControlAppearance.Success, new SymbolIcon(SymbolRegular.ClipboardCheckmark24), _snackbarDuration);
        }
    }

}
