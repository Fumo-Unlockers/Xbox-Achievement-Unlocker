using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.ViewModels.Windows;
using XAU.Views.Pages;
using XAU.Views.Windows;
using MessageBox = Wpf.Ui.Controls.MessageBox;

namespace XAU.ViewModels.Pages
{
    public partial class GamesViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty] private string _xuidOverride = "0";
        [ObservableProperty] private List<Game> _games;


        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);
        dynamic GamesResponse = (dynamic) (new JObject());
        public string SelectedTitleID = "0";

        public class Game
        {
            public string Title { get; set; }
            public string Image { get; set; }
            public string Gamerscore { get; set; }
            public string CurrentAchievements { get; set; }
            public string Progress { get; set; }
            public string Index { get; set; }

        }
        private string XAUTH = HomeViewModel.XAUTH;
        private bool _isInitialized = false;

        public GamesViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }
        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        public void OnNavigatedTo()
        {
            
            if (!_isInitialized && HomeViewModel.InitComplete)
                InitializeViewModel();
        }

        public void OnNavigatedFrom()
        {
        }

        private void InitializeViewModel()
        {
            XuidOverride = HomeViewModel.XUIDOnly;
            GetGamesList();
            _isInitialized = true;
            
        }

        private async void GetGamesList()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("Host", "titlehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            var responseString = await client.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + XuidOverride + ")/titles/titleHistory/decoration/Achievement,scid");
            GamesResponse = (dynamic)JObject.Parse(responseString);
            List<Game> GamesList = new List<Game>();
            for (int i = 0; i < GamesResponse.titles.Count; i++)
            {
                dynamic title = GamesResponse.titles[i];
                GamesList.Add(new Game(){Title = title.name.ToString(), 
                    CurrentAchievements = title.achievement.currentAchievements.ToString(), 
                    Gamerscore = title.achievement.currentGamerscore.ToString()+"/"+ title.achievement.totalGamerscore.ToString(),
                    Progress = title.achievement.progressPercentage.ToString(),
                    Image = title.displayImage.ToString(),
                    Index = i.ToString()
                });
            }
            Games = GamesList;
        }
        public async void OpenAchievements(string index)
        {
            XAU.Views.Windows.MainWindow.MainNavigationService.Navigate(typeof(AchievementsPage));
            SelectedTitleID = GamesResponse.titles[int.Parse(index)].titleId.ToString();
            _snackbarService.Show("Success", $"Index: {index}\nTitleID: {SelectedTitleID}", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
        }
    }
}
