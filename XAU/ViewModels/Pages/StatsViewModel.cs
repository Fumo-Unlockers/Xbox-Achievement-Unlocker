using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class StatsViewModel : ObservableObject, INavigationAware
    {
        private static readonly HttpClientHandler handler = new()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        private readonly HttpClient client = new(handler);
        private readonly string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        private bool _isInitialized = false;
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH));

        public StatsViewModel()
        {
            LoadStatsCommand = new AsyncRelayCommand(LoadStatsAsync);
            SaveStatsCommand = new AsyncRelayCommand(SaveStatsAsync);
            Stats = new List<StatItem>();
            InitializeHttpClient();
        }

        [ObservableProperty] private string titleId;
        [ObservableProperty] private List<StatItem> stats;
        [ObservableProperty] private StatItem selectedStat;
        [ObservableProperty] private string statusMessage;

        public IAsyncRelayCommand LoadStatsCommand { get; }
        public IAsyncRelayCommand SaveStatsCommand { get; }

        public void OnNavigatedTo()
        {
            if (!_isInitialized)
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        private void InitializeViewModel()
        {
            _isInitialized = true;
        }

        private void InitializeHttpClient()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
        }

        private async Task LoadStatsAsync()
        {
            StatusMessage = "Loading stats...";
            try
            {
                var requestBody = new StringContent($"{{\"arrangebyfield\":\"xuid\",\"xuids\":[\"{HomeViewModel.XUIDOnly}\"],\"groups\":[{{\"name\":\"Hero\",\"titleId\":\"{TitleId}\"}}]}}", Encoding.UTF8, "application/json");
                var response = await client.PostAsync("https://userstats.xboxlive.com/batch", requestBody);
                response.EnsureSuccessStatusCode();

                var jsonResponse = JObject.Parse(await response.Content.ReadAsStringAsync());
                var statsList = new List<StatItem>();

                foreach (var stat in jsonResponse["groups"][0]["statlistscollection"][0]["stats"])
                {
                    statsList.Add(new StatItem
                    {
                        DisplayName = stat["groupproperties"]["DisplayName"].ToString(),
                        Value = stat["value"]?.ToString() ?? string.Empty,
                        Name = stat["name"].ToString(),
                        Type = stat["type"]?.ToString() ?? "Unknown",
                        Scid = stat["scid"].ToString()
                    });
                }
                Stats = statsList;
                StatusMessage = "Stats loaded successfully.";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error loading stats: {ex.Message}";
            }
        }

        private async Task SaveStatsAsync()
        {
            StatusMessage = "Saving stats...";
            try
            {
                var currentTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.ffffffZ");
                long unixTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds();

                var statsDict = new Dictionary<string, object>();
                foreach (var stat in Stats)
                {
                    statsDict[stat.Name] = new { value = stat.Value };
                }

                var requestBody = new
                {
                    schema = "http://stats.xboxlive.com/2017-1/schema#",
                    previousRevision = unixTime,
                    revision = unixTime + 1,
                    stats = new
                    {
                        title = statsDict
                    },
                    timestamp = currentTime
                };

                var requestBodyJson = JsonConvert.SerializeObject(requestBody);
                var requestContent = new StringContent(requestBodyJson, Encoding.UTF8, "application/json");

                var response = await client.PatchAsync($"https://statswrite.xboxlive.com/stats/users/{HomeViewModel.XUIDOnly}/scids/{SelectedStat.Scid}", requestContent);
                response.EnsureSuccessStatusCode();

                StatusMessage = "Stat Write Successful!";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Error saving stats: {ex.Message}";
            }
        }
    }

    public partial class StatItem : ObservableObject
    {
        [ObservableProperty]
        private string displayName;

        [ObservableProperty]
        private string value;

        [ObservableProperty]
        private string name;

        [ObservableProperty]
        private string type;

        [ObservableProperty]
        private string scid;
    }
}
