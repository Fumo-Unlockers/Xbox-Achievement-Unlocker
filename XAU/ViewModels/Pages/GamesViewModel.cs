﻿using Newtonsoft.Json.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Net.Http;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.Views.Pages;
using XAU.Views.Windows;


namespace XAU.ViewModels.Pages
{
    public partial class GamesViewModel : ObservableObject, INavigationAware, INotifyPropertyChanged
    {
        [ObservableProperty] private string _xuidOverride = "0";
        [ObservableProperty] private ObservableCollection<Game> _games = new ObservableCollection<Game>();
        [ObservableProperty] private ObservableCollection<Game> _gamesPaged = new ObservableCollection<Game>();
        [ObservableProperty] private string _searchLabel = "Search 0 Games";
        [ObservableProperty] private GridLength _gamesListHeight = new GridLength(0, GridUnitType.Star);
        [ObservableProperty] private GridLength _loadingHeight = new GridLength(1, GridUnitType.Star);
        [ObservableProperty] private double _loadingSize = 200;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private List<string> _filterOptions = new List<string>() { "All", "Xbox One/Series", "PC", "Xbox 360", "Win32" };
        [ObservableProperty] private int _filterIndex = 0;
        [ObservableProperty] private int _numPages = 0;
        [ObservableProperty] private ObservableCollection<string> _pageOptions = new ObservableCollection<string>();
        [ObservableProperty] private int _currentPage = 0;
        [ObservableProperty] private bool _isInitialized = false;

        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };

        HttpClient client = new HttpClient(handler);
        dynamic GamesResponse = (dynamic)(new JObject());
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

        private string XAUTH = HomeViewModel.XAUTH;


        public GamesViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
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
            if (HomeViewModel.Settings.RegionOverride)
                currentSystemLanguage = "en-GB";

        }

        [RelayCommand]
        private async Task GetGamesList()
        {
            Games.Clear();
            GamesPaged.Clear();
            LoadingStart();
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            var responseString = await client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + XuidOverride + ")/titles/titleHistory/decoration/Achievement,scid?maxItems=10000");
            GamesResponse = (dynamic)JObject.Parse(responseString);
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
            AchievementsViewModel.TitleID = GamesResponse.titles[int.Parse(index)].titleId.ToString();
            AchievementsViewModel.IsSelectedGame360 = GamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Xbox360") || GamesResponse.titles[int.Parse(index)].devices.ToString().Contains("Mobile");
            AchievementsViewModel.NewGame = true;
            MainWindow.MainNavigationService.Navigate(typeof(AchievementsPage));
            await Task.CompletedTask;
        }
        [RelayCommand]
        public void SearchAndFilterGames()
        {
            if (SearchText.Length == 0)
            {
                _snackbarService.Show("Error", $"Please Enter Query Text", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            Games.Clear();
            GamesPaged.Clear();
            LoadingStart();
            if (FilterIndex != 0)
            {
                switch (FilterIndex)
                {
                    case 1:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("XboxSeries") || title.devices.ToString().Contains("XboxOne"))
                            {
                                if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 2:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("PC"))
                            {
                                if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 3:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("Xbox360"))
                            {
                                if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            }
                        }
                        break;
                    case 4:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("Win32"))
                            {
                                if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                                    continue;
                                AddGame(i);
                            };
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < GamesResponse.titles.Count; i++)
                {
                    dynamic title = GamesResponse.titles[i];
                    if (!title.name.ToString().ToLower().Contains(SearchText.ToLower()))
                        continue;
                    AddGame(i);

                }
            }

            LoadingEnd();
            SearchLabel = $"Search {GamesResponse.titles.Count.ToString()} Games";
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
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("XboxSeries") || title.devices.ToString().Contains("XboxOne"))
                                AddGame(i);
                        }
                        break;
                    case 2:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("PC"))
                                AddGame(i);
                        }
                        break;
                    case 3:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("Xbox360"))
                                AddGame(i);
                        }
                        break;
                    case 4:
                        for (int i = 0; i < GamesResponse.titles.Count; i++)
                        {
                            dynamic title = GamesResponse.titles[i];
                            if (title.devices.ToString().Contains("Win32"))
                                AddGame(i);
                        }
                        break;
                }
            }
            else
            {
                for (int i = 0; i < GamesResponse.titles.Count; i++)
                {
                    AddGame(i);
                }
            }

            LoadingEnd();
            SearchLabel = $"Search {GamesResponse.titles.Count.ToString()} Games";
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
            dynamic title = GamesResponse.titles[index];
            var EditedImage = title.displayImage.ToString();
            if (EditedImage.Contains("store-images.s-microsoft.com"))
            {
                EditedImage = EditedImage + "?w=256&h=256&format=jpg";
            }
            Games.Add(new Game()
            {
                Title = title.name.ToString(),
                CurrentAchievements = title.achievement.currentAchievements.ToString(),
                Gamerscore = title.achievement.currentGamerscore.ToString() + "/" +
                             title.achievement.totalGamerscore.ToString(),
                Progress = title.achievement.progressPercentage.ToString(),
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
            var titleid = GamesResponse.titles[int.Parse(index)].titleId.ToString();
            var title = GamesResponse.titles[int.Parse(index)].name.ToString();
            Clipboard.SetDataObject(GamesResponse.titles[int.Parse(index)].titleId.ToString());
            _snackbarService.Show("TitleID Copied", $"Copied the title ID of {title.ToString()} to clipboard\nTitleID: {titleid.ToString()}", ControlAppearance.Success, new SymbolIcon(SymbolRegular.ClipboardCheckmark24), _snackbarDuration);
        }
    }

}
