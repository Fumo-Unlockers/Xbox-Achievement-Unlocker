using System.Collections.ObjectModel;
using System.Text;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using Wpf.Ui.Contracts;

namespace XAU.ViewModels.Pages
{
    public partial class StatsViewModel : ObservableObject, INavigationAware
    {
        public const string BatchRoute = "Batch route";
        public const string ScidRoute = "SCID route";
        public IReadOnlyList<string> StatRoutes { get; } = [BatchRoute, ScidRoute];

        [ObservableProperty] private string _titleId = "333628240";
        [ObservableProperty] private string _scid = "636a0100-392d-4116-9c4f-e0c513e2c350";
        [ObservableProperty] private string _statNames = "MinutesPlayed";
        [ObservableProperty] private string _selectedStatRoute = BatchRoute;
        [ObservableProperty] private bool _includeValueMetadata;
        [ObservableProperty] private string _queryStatus = "Enter exact Xbox stat names to query.";
        [ObservableProperty] private bool _isQuerying;
        [ObservableProperty] private ObservableCollection<StatExplorerRow> _stats = new();

        private bool _isInitialized = false;
        private readonly ISnackbarService _snackbarService;
        private readonly TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        public StatsViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
        }

        public class StatExplorerRow
        {
            public string? Name { get; set; }
            public string? Value { get; set; }
            public string? Type { get; set; }
            public string? TitleId { get; set; }
            public string? Scid { get; set; }
            public string? ValueMetadata { get; set; }
        }

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

        [RelayCommand]
        private async Task QueryStats()
        {
            var requestedNames = StatNames
                .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();

            if (string.IsNullOrWhiteSpace(TitleId) || requestedNames.Count == 0)
            {
                QueryStatus = "Enter a title ID and at least one stat name.";
                return;
            }
            if (SelectedStatRoute == ScidRoute && string.IsNullOrWhiteSpace(Scid))
            {
                QueryStatus = "Enter an SCID before using the SCID route.";
                return;
            }
            if (string.IsNullOrWhiteSpace(HomeViewModel.XAUTH) || string.IsNullOrWhiteSpace(HomeViewModel.XUIDOnly))
            {
                QueryStatus = "Attach to the Xbox app before querying stats.";
                return;
            }

            IsQuerying = true;
            QueryStatus = "Querying Xbox user stats...";
            Stats.Clear();

            try
            {
                var api = new XboxRestAPI(HomeViewModel.XAUTH);
                var returnedStats = SelectedStatRoute == ScidRoute
                    ? await api.GetGameStatsByScidAsync(
                        HomeViewModel.XUIDOnly,
                        TitleId.Trim(),
                        Scid.Trim(),
                        requestedNames,
                        IncludeValueMetadata)
                    : (await api.GetGameStatsAsync(HomeViewModel.XUIDOnly, TitleId.Trim(), requestedNames))
                        ?.StatListsCollection.SelectMany(collection => collection.Stats).ToList() ?? [];

                foreach (var requestedName in requestedNames)
                {
                    var matches = returnedStats.Where(stat =>
                        string.Equals(stat.Name, requestedName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count == 0)
                    {
                        Stats.Add(new StatExplorerRow
                        {
                            Name = requestedName,
                            Value = "Not returned",
                            Type = "Missing",
                            TitleId = TitleId.Trim(),
                            Scid = SelectedStatRoute == ScidRoute ? Scid.Trim() : null
                        });
                        continue;
                    }

                    foreach (var stat in matches)
                    {
                        Stats.Add(new StatExplorerRow
                        {
                            Name = stat.Name,
                            Value = stat.Value,
                            Type = stat.Type,
                            TitleId = stat.TitleId,
                            Scid = stat.Scid,
                            ValueMetadata = FormatValueMetadata(stat)
                        });
                    }
                }

                QueryStatus = $"Returned {returnedStats.Count} stat value(s); {Stats.Count(row => row.Type == "Missing")} requested stat(s) were not returned.";
            }
            catch (Exception ex)
            {
                QueryStatus = $"Stat query failed: {ex.Message}";
            }
            finally
            {
                IsQuerying = false;
            }
        }

        [RelayCommand]
        private void CopyStats()
        {
            if (Stats.Count == 0)
                return;

            var text = new StringBuilder()
                .AppendLine($"Title ID: {TitleId}")
                .AppendLine(QueryStatus);
            foreach (var stat in Stats)
            {
                text.AppendLine();
                text.AppendLine($"Stat name: {stat.Name}");
                text.AppendLine($"Value: {stat.Value}");
                text.AppendLine($"Type: {stat.Type}");
                text.AppendLine($"Title ID: {stat.TitleId}");
                text.AppendLine($"SCID: {stat.Scid}");
                text.AppendLine($"Value metadata: {stat.ValueMetadata}");
            }

            Clipboard.SetText(text.ToString());
            _snackbarService.Show("Stats Copied", "The displayed stat results were copied to the clipboard.",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.ClipboardCheckmark24), _snackbarDuration);
        }

        private static string FormatValueMetadata(Stat stat)
        {
            if (!string.IsNullOrWhiteSpace(stat.ValueMetadata))
                return stat.ValueMetadata;
            return stat.Properties.Count == 0
                ? ""
                : Newtonsoft.Json.JsonConvert.SerializeObject(stat.Properties);
        }
    }
}
