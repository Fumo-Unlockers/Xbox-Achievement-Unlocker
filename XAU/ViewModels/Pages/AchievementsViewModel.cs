using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Windows.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Controls;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Services;
using XAU.Services;
using XAU.Util.Etw;
using XAU.Views.Pages;

namespace XAU.ViewModels.Pages
{
    public partial class AchievementsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _titleIDOverride = "0";
        [ObservableProperty] private bool _unlockable = false;
        [ObservableProperty] private bool _titleIDEnabled = false;
        [ObservableProperty] private ObservableCollection<OneCoreAchievementResponse> _achievements = new ObservableCollection<OneCoreAchievementResponse>();
        [ObservableProperty] private ObservableCollection<DGAchievement> _dGAchievements = new ObservableCollection<DGAchievement>();
        [ObservableProperty] public string _gameInfo = "";
        [ObservableProperty] private string _gameName = "";
        [ObservableProperty] private bool _isUnlockAllEnabled = false;
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private bool _isEventBased = false;
        [ObservableProperty] private bool _isEventUnlockAvailable = false;
        [ObservableProperty] private DGAchievement? _selectedAchievement;
        [ObservableProperty] private ObservableCollection<AchievementRequirements> _selectedAchievementRequirements = new ObservableCollection<AchievementRequirements>();
        [ObservableProperty] private ObservableCollection<EventMappingDetail> _selectedAchievementMappings = new ObservableCollection<EventMappingDetail>();
        [ObservableProperty] private string _selectedAchievementEventSupport = "";
        [ObservableProperty] private string _selectedAchievementSourceEndpoint = "";
        [ObservableProperty] private string _selectedAchievementRequirementJsonPath = "";
        [ObservableProperty] private string _selectedAchievementRawJson = "";
        [ObservableProperty] private string _selectedAchievementRawRequirementJson = "";
        [ObservableProperty] private string _achievementStatNames = "TimeStopBullets, Headshots, MinutesPlayed";
        [ObservableProperty] private string _achievementStatCorrelationStatus = "Select an achievement and enter candidate Xbox stat names.";
        [ObservableProperty] private bool _isQueryingAchievementStats;
        [ObservableProperty] private ObservableCollection<AchievementStatCorrelationRow> _achievementStatCorrelations = new();
        [ObservableProperty] private bool _isQuantumBreakTestAvailable;
        [ObservableProperty] private string _quantumBreakProgressionData = "";
        [ObservableProperty] private string _quantumBreakPayloadPreview = "";
        [ObservableProperty] private string _quantumBreakTestResult = "Capture a snapshot or preview one ProgressionData value.";
        [ObservableProperty] private string _quantumBreakProposedMapping = "";
        [ObservableProperty] private bool _isQuantumBreakTelemetryCapturing;
        [ObservableProperty] private string _quantumBreakTelemetryCaptureStatus = "Ready to capture genuine Quantum Break ProgressionEvent telemetry.";
        [ObservableProperty] private ObservableCollection<EtwTokenCapture.QuantumBreakTelemetryEvent> _quantumBreakTelemetryEvents = new();
        [ObservableProperty] private EtwTokenCapture.QuantumBreakTelemetryEvent? _selectedQuantumBreakTelemetryEvent;
        [ObservableProperty] private int _quantumBreakTelemetryCaptureSeconds = 90;
        public IReadOnlyList<int> QuantumBreakTelemetryCaptureDurations { get; } = [45, 90, 180];
        [ObservableProperty] private bool _useCustomEventUnlockTime = false;
        [ObservableProperty] private DateTime? _customEventUnlockDate = DateTime.Today;
        [ObservableProperty] private string _customEventUnlockTimeText = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture);
        public static string TitleID = "0";
        private bool IsTitleIDValid = false;
        public static bool NewGame = false;
        public static bool IsSelectedGame360;
        private AchievementsResponse AchievementResponse = new AchievementsResponse();
        private Xbox360AchievementResponse Xbox360AchievementResponse = new Xbox360AchievementResponse();
        private Dictionary<int, DGAchievement> _unlockedAchievements = new Dictionary<int, DGAchievement>();

        private GameTitle GameInfoResponse = new GameTitle();
        // TODO: this needs to be updated if language changes
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH));

        public static bool SpoofingUpdate = false;
        private bool IsFiltered = false;
        private dynamic EventsData = (dynamic)(new JObject());
        public static string EventsToken;
        private Dictionary<string, AchievementTestSnapshot> _quantumBreakBeforeSnapshot = new();
        private int? _lastQuantumBreakCandidate;
        private string? _correlationSelectionKey;
        private readonly Dictionary<string, decimal> _previousCorrelationStatValues = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, decimal> _previousCorrelationRequirementValues = new(StringComparer.OrdinalIgnoreCase);

        public AchievementsViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService, INavigationService navigationService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;
            _navigationService = navigationService;
        }

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private readonly INavigationService _navigationService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        public class DGAchievement
        {
            public int Index { get; set; }
            public int ID { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public bool IsSecret { get; set; }
            public DateTime DateUnlocked { get; set; }
            public int Gamerscore { get; set; }
            public float RarityPercentage { get; set; }
            public string? RarityCategory { get; set; }
            public string? ProgressState { get; set; }
            public bool IsUnlockable { get; set; }
            public string? EventMappingStatus { get; set; }
        }

        public class EventMappingDetail
        {
            public string? ReplacementType { get; set; }
            public string? Target { get; set; }
            public string? Replacement { get; set; }
        }

        public class AchievementStatCorrelationRow
        {
            public string? StatName { get; set; }
            public string? PreviousStatValue { get; set; }
            public string? StatValue { get; set; }
            public string? StatDelta { get; set; }
            public string? RequirementId { get; set; }
            public string? PreviousRequirementCurrent { get; set; }
            public string? RequirementCurrent { get; set; }
            public string? RequirementDelta { get; set; }
            public string? RequirementTarget { get; set; }
            public string? Difference { get; set; }
            public string Match { get; set; } = "No match";
            public string? Type { get; set; }
            public string? Scid { get; set; }
        }

        private class AchievementTestSnapshot
        {
            public string Name { get; set; } = "";
            public string? State { get; set; }
            public string? TimeUnlocked { get; set; }
            public Dictionary<string, string?> Requirements { get; set; } = new();
        }

        public async void OnNavigatedTo()
        {
            if (HomeViewModel.Settings.AutoSpooferEnabled)
            {

                if (!GameInfoResponse.Titles.Any() && !String.IsNullOrWhiteSpace(GameInfoResponse.Xuid))
                {
                    _snackbarService.Show("Error: Game Info Response Contained No Titles", $"There were no titles returned from the API", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }
                else
                {
                    if (HomeViewModel.SpoofingStatus == 1 && !!string.IsNullOrWhiteSpace(GameInfo))
                    {
                        if (HomeViewModel.SpoofedTitleID == TitleIDOverride)
                        {
                            GameInfo = "Manually Spoofing";
                            GameName = GameInfoResponse.Titles[0].Name;
                        }
                        else
                        {
                            GameInfo = "Spoofing Another Game";
                            GameName = GameInfoResponse.Titles[0].Name;
                        }

                    }
                    else if (HomeViewModel.SpoofingStatus == 0 && !string.IsNullOrWhiteSpace(GameInfo))
                    {
                        SpoofGame();
                    }
                }
            }

            if (IsInitialized && NewGame)
                await RefreshAchievements();
            if (TitleID != "0")
            {
                TitleIDOverride = TitleID;
                TitleID = "0";
            }
            if (HomeViewModel.InitComplete && TitleIDOverride == "0")
                TitleIDEnabled = true;
            if (!IsInitialized && HomeViewModel.InitComplete && TitleIDOverride != "0")
                InitializeViewModel();
        }

        public void OnNavigatedFrom() { }

        partial void OnSelectedAchievementChanged(DGAchievement? value)
        {
            SelectedAchievementRequirements.Clear();
            SelectedAchievementMappings.Clear();
            SelectedAchievementEventSupport = "";
            SelectedAchievementSourceEndpoint = "";
            SelectedAchievementRequirementJsonPath = "";
            SelectedAchievementRawJson = "";
            SelectedAchievementRawRequirementJson = "";
            AchievementStatCorrelations.Clear();
            AchievementStatCorrelationStatus = value == null
                ? "Select an achievement and enter candidate Xbox stat names."
                : "Enter candidate Xbox stat names, then query to compare them with requirement.current.";
            IsQuantumBreakTestAvailable = value != null && TitleIDOverride == "333628240";
            if (value == null)
                return;

            var correlationSelectionKey = $"{TitleIDOverride}:{value.ID}";
            if (!string.Equals(_correlationSelectionKey, correlationSelectionKey, StringComparison.Ordinal))
            {
                _correlationSelectionKey = correlationSelectionKey;
                _previousCorrelationStatValues.Clear();
                _previousCorrelationRequirementValues.Clear();
            }

            if (!IsSelectedGame360)
            {
                var originalAchievement = AchievementResponse.achievements.FirstOrDefault(achievement =>
                    achievement.id == value.ID.ToString(CultureInfo.InvariantCulture));
                foreach (var requirement in originalAchievement?.progression?.requirements ?? [])
                    SelectedAchievementRequirements.Add(requirement);
            }

            PopulateSelectedAchievementRawSource(value);

            SelectedAchievementEventSupport = IsEventBased
                ? IsEventUnlockAvailable ? "Available" : "Not available"
                : "Not event-based";

            var mapping = IsEventUnlockAvailable
                ? (EventsData.Achievements as JObject)?[value.ID.ToString(CultureInfo.InvariantCulture)]
                : null;
            foreach (var replacement in mapping?.Children<JProperty>() ?? [])
            {
                SelectedAchievementMappings.Add(new EventMappingDetail
                {
                    ReplacementType = replacement.Value["ReplacementType"]?.ToString(),
                    Target = replacement.Value["Target"]?.ToString(),
                    Replacement = replacement.Value["Replacement"]?.ToString()
                });
            }
        }

        private void PopulateSelectedAchievementRawSource(DGAchievement selectedAchievement)
        {
            SelectedAchievementSourceEndpoint = _xboxRestAPI.IsValueCreated
                ? _xboxRestAPI.Value.LastAchievementsRequestUrl ?? ""
                : "";
            SelectedAchievementRequirementJsonPath =
                $"$.achievements[?(@.id=='{selectedAchievement.ID}')].progression.requirements[*].current";

            var rawResponse = _xboxRestAPI.IsValueCreated
                ? _xboxRestAPI.Value.LastAchievementsResponseJson
                : null;
            if (string.IsNullOrWhiteSpace(rawResponse))
                return;

            try
            {
                var rawAchievement = JObject.Parse(rawResponse)["achievements"]?
                    .Children<JObject>()
                    .FirstOrDefault(achievement =>
                        string.Equals(
                            achievement["id"]?.ToString(),
                            selectedAchievement.ID.ToString(CultureInfo.InvariantCulture),
                            StringComparison.Ordinal));

                SelectedAchievementRawJson = rawAchievement?.ToString(Formatting.Indented) ?? "";
                SelectedAchievementRawRequirementJson =
                    rawAchievement?["progression"]?["requirements"]?.ToString(Formatting.Indented) ?? "";
            }
            catch (JsonException ex)
            {
                SelectedAchievementRawJson = $"Could not parse the raw achievement response: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task QueryAchievementStatCorrelation()
        {
            if (IsQueryingAchievementStats)
                return;
            if (SelectedAchievement == null)
            {
                AchievementStatCorrelationStatus = "Select an achievement before querying stats.";
                return;
            }

            var requestedNames = AchievementStatNames
                .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList();
            if (requestedNames.Count == 0)
            {
                AchievementStatCorrelationStatus = "Enter at least one candidate stat name.";
                return;
            }
            if (string.IsNullOrWhiteSpace(HomeViewModel.XAUTH) || string.IsNullOrWhiteSpace(HomeViewModel.XUIDOnly))
            {
                AchievementStatCorrelationStatus = "Attach to the Xbox app before querying stats.";
                return;
            }

            var numericRequirements = SelectedAchievementRequirements
                .Select((requirement, index) => new
                {
                    Requirement = requirement,
                    Key = requirement.id ?? $"Requirement {index + 1}",
                    IsNumeric = TryParseCorrelationNumber(requirement.current, out var current),
                    Current = current
                })
                .Where(requirement => requirement.IsNumeric)
                .ToList();
            if (numericRequirements.Count == 0)
            {
                AchievementStatCorrelationStatus = "The selected achievement has no numeric requirement.current values to compare.";
                return;
            }

            IsQueryingAchievementStats = true;
            AchievementStatCorrelationStatus = "Querying Xbox user stats...";
            AchievementStatCorrelations.Clear();

            try
            {
                var response = await _xboxRestAPI.Value.GetGameStatsAsync(
                    HomeViewModel.XUIDOnly, TitleIDOverride, requestedNames);
                var returnedStats = response?.StatListsCollection
                    .SelectMany(collection => collection.Stats)
                    .ToList() ?? [];
                var exactMatches = 0;
                var nearMatches = 0;

                foreach (var requestedName in requestedNames)
                {
                    var matches = returnedStats.Where(stat =>
                        string.Equals(stat.Name, requestedName, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (matches.Count == 0)
                    {
                        AchievementStatCorrelations.Add(new AchievementStatCorrelationRow
                        {
                            StatName = requestedName,
                            StatValue = "Not returned",
                            Match = "Missing"
                        });
                        continue;
                    }

                    foreach (var stat in matches)
                    {
                        if (!TryParseCorrelationNumber(stat.Value, out var statValue))
                        {
                            AchievementStatCorrelations.Add(new AchievementStatCorrelationRow
                            {
                                StatName = stat.Name,
                                StatValue = stat.Value,
                                Match = "Non-numeric",
                                Type = stat.Type,
                                Scid = stat.Scid
                            });
                            continue;
                        }

                        var closest = numericRequirements
                            .OrderBy(requirement => Math.Abs(statValue - requirement.Current))
                            .First();
                        var difference = Math.Abs(statValue - closest.Current);
                        var nearThreshold = Math.Max(5m, Math.Abs(closest.Current) * 0.05m);
                        var match = difference == 0 ? "Exact" : difference <= nearThreshold ? "Near" : "No match";
                        exactMatches += match == "Exact" ? 1 : 0;
                        nearMatches += match == "Near" ? 1 : 0;

                        var statKey = $"{stat.TitleId}|{stat.Scid}|{stat.Name ?? requestedName}";
                        _previousCorrelationStatValues.TryGetValue(statKey, out var previousStat);
                        var hasPreviousStat = _previousCorrelationStatValues.ContainsKey(statKey);
                        _previousCorrelationRequirementValues.TryGetValue(closest.Key, out var previousRequirement);
                        var hasPreviousRequirement = _previousCorrelationRequirementValues.ContainsKey(closest.Key);

                        AchievementStatCorrelations.Add(new AchievementStatCorrelationRow
                        {
                            StatName = stat.Name,
                            PreviousStatValue = hasPreviousStat ? FormatCorrelationNumber(previousStat) : "",
                            StatValue = stat.Value,
                            StatDelta = hasPreviousStat ? FormatCorrelationDelta(statValue - previousStat) : "",
                            RequirementId = closest.Requirement.id,
                            PreviousRequirementCurrent = hasPreviousRequirement ? FormatCorrelationNumber(previousRequirement) : "",
                            RequirementCurrent = closest.Requirement.current,
                            RequirementDelta = hasPreviousRequirement ? FormatCorrelationDelta(closest.Current - previousRequirement) : "",
                            RequirementTarget = closest.Requirement.target,
                            Difference = FormatCorrelationNumber(difference),
                            Match = match,
                            Type = stat.Type,
                            Scid = stat.Scid
                        });

                        _previousCorrelationStatValues[statKey] = statValue;
                    }
                }

                foreach (var requirement in numericRequirements)
                    _previousCorrelationRequirementValues[requirement.Key] = requirement.Current;

                AchievementStatCorrelationStatus =
                    $"Compared {returnedStats.Count} returned stat value(s) with {numericRequirements.Count} requirement value(s). " +
                    $"Exact: {exactMatches}; near: {nearMatches}. Near means within 5 or 5% of requirement.current.";
            }
            catch (Exception ex)
            {
                AchievementStatCorrelationStatus = $"Stat correlation query failed: {ex.Message}";
            }
            finally
            {
                IsQueryingAchievementStats = false;
            }
        }

        private static bool TryParseCorrelationNumber(string? value, out decimal number) =>
            decimal.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out number);

        private static string FormatCorrelationNumber(decimal number) =>
            number.ToString("0.################", CultureInfo.InvariantCulture);

        private static string FormatCorrelationDelta(decimal delta) =>
            delta.ToString("+0.################;-0.################;0", CultureInfo.InvariantCulture);

        [RelayCommand]
        private void CopySelectedAchievementDetails()
        {
            if (SelectedAchievement == null)
                return;

            var text = new StringBuilder()
                .AppendLine($"Achievement API ID: {SelectedAchievement.ID}")
                .AppendLine($"Achievement name: {SelectedAchievement.Name}")
                .AppendLine($"State: {SelectedAchievement.ProgressState}")
                .AppendLine($"Mapping status: {SelectedAchievement.EventMappingStatus}")
                .AppendLine($"Title event support data: {SelectedAchievementEventSupport}")
                .AppendLine($"Source endpoint: {SelectedAchievementSourceEndpoint}")
                .AppendLine($"Requirement current JSON path: {SelectedAchievementRequirementJsonPath}");

            text.AppendLine("Mappings:");
            if (SelectedAchievementMappings.Count == 0)
                text.AppendLine("  None");
            foreach (var mapping in SelectedAchievementMappings)
            {
                text.AppendLine($"  ReplacementType: {mapping.ReplacementType}");
                text.AppendLine($"  Target: {mapping.Target}");
                text.AppendLine($"  Replacement: {mapping.Replacement}");
            }

            text.AppendLine("Requirements:");
            if (SelectedAchievementRequirements.Count == 0)
                text.AppendLine("  None");
            foreach (var requirement in SelectedAchievementRequirements)
            {
                text.AppendLine($"  Requirement ID: {requirement.id}");
                text.AppendLine($"  Current: {requirement.current}");
                text.AppendLine($"  Target: {requirement.target}");
                text.AppendLine($"  Operation type: {requirement.operationType}");
                text.AppendLine($"  Value type: {requirement.valueType}");
                text.AppendLine($"  Rule participation type: {requirement.ruleParticipationType}");
            }

            text.AppendLine().AppendLine("Raw achievement JSON:").AppendLine(SelectedAchievementRawJson);
            text.AppendLine().AppendLine("Raw requirement JSON:").AppendLine(SelectedAchievementRawRequirementJson);

            Clipboard.SetText(text.ToString());
        }

        [RelayCommand]
        private void PreviewQuantumBreakTestEvent()
        {
            if (!TryGetQuantumBreakCandidate(out var candidate))
                return;

            try
            {
                QuantumBreakPayloadPreview = BuildQuantumBreakTestPayload(candidate, false);
                QuantumBreakTestResult = $"Previewed one Quantum Break ProgressionData candidate: {candidate}. No event was sent.";
            }
            catch (Exception ex)
            {
                QuantumBreakTestResult = $"Could not build preview: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task SendQuantumBreakTestEvent()
        {
            if (!TryGetQuantumBreakCandidate(out var candidate))
                return;
            if (string.IsNullOrWhiteSpace(EventsToken) || HomeViewModel.IsEventsTokenExpired())
            {
                QuantumBreakTestResult = "Cannot send: the events token is missing or expired.";
                return;
            }

            var selectedId = SelectedAchievement!.ID;
            var selectedName = SelectedAchievement.Name;
            var result = await _contentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions
            {
                Title = "Send one Quantum Break test event?",
                Content = $"Achievement: {selectedId} - {selectedName}\nProgressionData: {candidate}\n\nThis sends exactly one event and will not edit Data.json.",
                PrimaryButtonText = "Send one event",
                CloseButtonText = "Cancel"
            });
            if (result != ContentDialogResult.Primary)
                return;

            _quantumBreakBeforeSnapshot = CaptureAchievementSnapshot(AchievementResponse);
            _lastQuantumBreakCandidate = candidate;
            QuantumBreakProposedMapping = "";

            try
            {
                QuantumBreakPayloadPreview = BuildQuantumBreakTestPayload(candidate, false);
                var requestBody = BuildQuantumBreakTestPayload(candidate, true);
                await _xboxRestAPI.Value.UnlockEventBasedAchievement(
                    EventsToken,
                    new StringContent(requestBody, Encoding.UTF8, "application/x-json-stream"));

                await RefreshAndCompareQuantumBreakSnapshot(selectedId, true);
            }
            catch (Exception ex)
            {
                QuantumBreakTestResult = $"Test event failed: {ex.Message}";
            }
        }

        [RelayCommand]
        private async Task RefreshQuantumBreakSnapshot()
        {
            if (!IsQuantumBreakTestAvailable || SelectedAchievement == null)
                return;

            var selectedId = SelectedAchievement.ID;
            if (_quantumBreakBeforeSnapshot.Count == 0)
            {
                _quantumBreakBeforeSnapshot = CaptureAchievementSnapshot(AchievementResponse);
                QuantumBreakTestResult = "Before snapshot captured. No event was sent.";
                return;
            }

            await RefreshAndCompareQuantumBreakSnapshot(selectedId);
        }

        [RelayCommand]
        private void CopyQuantumBreakTestResult()
        {
            if (!IsQuantumBreakTestAvailable || SelectedAchievement == null)
                return;

            var text = new StringBuilder()
                .AppendLine($"Quantum Break achievement: {SelectedAchievement.ID} - {SelectedAchievement.Name}")
                .AppendLine($"Mapping status: {SelectedAchievement.EventMappingStatus}")
                .AppendLine($"ProgressionData candidate: {QuantumBreakProgressionData}")
                .AppendLine()
                .AppendLine(QuantumBreakTestResult);
            if (!string.IsNullOrWhiteSpace(QuantumBreakProposedMapping))
                text.AppendLine().AppendLine("Proposed Data.json mapping:").AppendLine(QuantumBreakProposedMapping);

            Clipboard.SetText(text.ToString());
        }

        private bool TryGetQuantumBreakCandidate(out int candidate)
        {
            candidate = 0;
            if (TitleIDOverride != "333628240" || SelectedAchievement == null)
            {
                QuantumBreakTestResult = "Select an achievement from Quantum Break first.";
                return false;
            }
            if (!int.TryParse(QuantumBreakProgressionData, NumberStyles.Integer, CultureInfo.InvariantCulture, out candidate))
            {
                QuantumBreakTestResult = "Enter one valid ProgressionData integer.";
                return false;
            }

            return true;
        }

        private static string BuildQuantumBreakTestPayload(int candidate, bool forSend)
        {
            var templatePath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events", "333628240.json");
            var requestBody = File.ReadAllText(templatePath).Replace("REPLACEINDEX", candidate.ToString(CultureInfo.InvariantCulture));
            requestBody = requestBody.Replace("REPLACESEQ", forSend ? DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString() : "0");
            requestBody = requestBody.Replace("REPLACEXUID", forSend ? HomeViewModel.XUIDOnly : "REDACTED_XUID");
            requestBody = requestBody.Replace("REPLACETIME", forSend
                ? DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ")
                : "CURRENT_UTC_TIME_AT_SEND");
            return JObject.Parse(requestBody).ToString(forSend ? Formatting.None : Formatting.Indented);
        }

        [RelayCommand(CanExecute = nameof(CanStartQuantumBreakTelemetryCapture))]
        private async Task StartQuantumBreakTelemetryCapture()
        {
            if (TitleIDOverride != "333628240")
            {
                QuantumBreakTelemetryCaptureStatus = "Load Quantum Break before starting telemetry capture.";
                return;
            }

            IsQuantumBreakTelemetryCapturing = true;
            QuantumBreakTelemetryEvents.Clear();
            SelectedQuantumBreakTelemetryEvent = null;
            var captureSeconds = QuantumBreakTelemetryCaptureDurations.Contains(QuantumBreakTelemetryCaptureSeconds)
                ? QuantumBreakTelemetryCaptureSeconds
                : 90;
            QuantumBreakTelemetryCaptureStatus = $"Capturing for {captureSeconds} seconds. Perform the action in Quantum Break, then exit to menu or close the game before capture ends.";

            try
            {
                var events = await Task.Run(() => EtwTokenCapture.CaptureQuantumBreakTelemetry(captureSeconds));
                foreach (var capturedEvent in events)
                    QuantumBreakTelemetryEvents.Add(capturedEvent);
                SelectedQuantumBreakTelemetryEvent = QuantumBreakTelemetryEvents.FirstOrDefault();
                QuantumBreakTelemetryCaptureStatus = events.Count == 0
                    ? "Capture finished. No Quantum Break ProgressionEvent payloads were found."
                    : $"Capture finished. Found {events.Count} sanitized Quantum Break ProgressionEvent payload(s).";
            }
            catch (Exception ex)
            {
                QuantumBreakTelemetryCaptureStatus = $"Telemetry capture failed: {ex.Message}";
            }
            finally
            {
                IsQuantumBreakTelemetryCapturing = false;
            }
        }

        private bool CanStartQuantumBreakTelemetryCapture() => !IsQuantumBreakTelemetryCapturing;

        partial void OnIsQuantumBreakTelemetryCapturingChanged(bool value)
        {
            StartQuantumBreakTelemetryCaptureCommand.NotifyCanExecuteChanged();
        }

        [RelayCommand]
        private void CopyQuantumBreakTelemetry()
        {
            if (QuantumBreakTelemetryEvents.Count == 0)
                return;

            Clipboard.SetText(string.Join(
                Environment.NewLine + Environment.NewLine,
                QuantumBreakTelemetryEvents.Select(capturedEvent => capturedEvent.SanitizedPayload)));
            QuantumBreakTelemetryCaptureStatus = $"Copied {QuantumBreakTelemetryEvents.Count} sanitized payload(s).";
        }

        private static Dictionary<string, AchievementTestSnapshot> CaptureAchievementSnapshot(AchievementsResponse response)
        {
            return response.achievements.ToDictionary(
                achievement => achievement.id,
                achievement => new AchievementTestSnapshot
                {
                    Name = achievement.name,
                    State = achievement.progressState,
                    TimeUnlocked = achievement.progression?.timeUnlocked,
                    Requirements = (achievement.progression?.requirements ?? [])
                        .Select((requirement, index) => new
                        {
                            Key = string.IsNullOrWhiteSpace(requirement.id) ? $"#{index}" : requirement.id,
                            requirement.current
                        })
                        .ToDictionary(requirement => requirement.Key!, requirement => requirement.current)
                });
        }

        private async Task RefreshAndCompareQuantumBreakSnapshot(int selectedId, bool eventSent = false)
        {
            QuantumBreakProposedMapping = "";
            var attempts = eventSent ? 3 : 1;
            var changes = "No achievement state, unlock time, or requirement changes detected.";
            var selectedChanged = false;
            var anyChanged = false;
            var afterSnapshot = new Dictionary<string, AchievementTestSnapshot>();

            try
            {
                for (var attempt = 0; attempt < attempts; attempt++)
                {
                    if (eventSent)
                        await Task.Delay(attempt == 0 ? 3000 : 4000);

                    var freshResponse = await _xboxRestAPI.Value.GetAchievementsForTitleAsync(
                        HomeViewModel.XUIDOnly, TitleIDOverride);
                    if (freshResponse == null)
                        continue;

                    AchievementResponse = freshResponse;
                    afterSnapshot = CaptureAchievementSnapshot(freshResponse);
                    changes = CompareAchievementSnapshots(
                        _quantumBreakBeforeSnapshot,
                        afterSnapshot,
                        selectedId,
                        out selectedChanged,
                        out anyChanged);
                    if (anyChanged)
                        break;
                }

                await RefreshAchievements();
                SelectedAchievement = DGAchievements.FirstOrDefault(achievement => achievement.ID == selectedId);
            }
            catch (Exception ex)
            {
                QuantumBreakTestResult = eventSent
                    ? $"Event sent; Xbox achievement data refresh failed: {ex.Message}"
                    : $"Xbox achievement data refresh failed: {ex.Message}";
                if (eventSent)
                    RecordQuantumBreakMappingResearch(selectedId, afterSnapshot, QuantumBreakTestResult, true);
                return;
            }

            QuantumBreakTestResult = anyChanged
                ? changes
                : eventSent
                    ? "Event sent; Xbox popup observed may take time to sync. No Xbox achievement API changes detected yet."
                    : changes;

            if (eventSent)
                RecordQuantumBreakMappingResearch(selectedId, afterSnapshot, QuantumBreakTestResult, anyChanged);

            if (selectedChanged && _lastQuantumBreakCandidate.HasValue)
            {
                QuantumBreakProposedMapping = new JObject
                {
                    [selectedId.ToString(CultureInfo.InvariantCulture)] = new JObject
                    {
                        ["Replacement"] = new JObject
                        {
                            ["ReplacementType"] = "Replace",
                            ["Target"] = "REPLACEINDEX",
                            ["Replacement"] = _lastQuantumBreakCandidate.Value
                        }
                    }
                }.ToString(Formatting.Indented);
            }
        }

        private void RecordQuantumBreakMappingResearch(
            int selectedId,
            Dictionary<string, AchievementTestSnapshot> afterSnapshot,
            string observedChanges,
            bool anyChanged)
        {
            if (!_lastQuantumBreakCandidate.HasValue)
                return;

            var selectedKey = selectedId.ToString(CultureInfo.InvariantCulture);
            _quantumBreakBeforeSnapshot.TryGetValue(selectedKey, out var before);
            afterSnapshot.TryGetValue(selectedKey, out var after);
            var requirementsChanged = before != null && after != null &&
                !before.Requirements.OrderBy(item => item.Key)
                    .SequenceEqual(after.Requirements.OrderBy(item => item.Key));
            var unlocked = before != null && after != null &&
                !string.Equals(before.State, StringConstants.Achieved, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(after.State, StringConstants.Achieved, StringComparison.OrdinalIgnoreCase);
            var selectedStateChanged = before != null && after != null &&
                (before.State != after.State || before.TimeUnlocked != after.TimeUnlocked);

            AchievementMappingResearchStore.Record(new AchievementMappingResearchEntry
            {
                TitleId = AchievementMappingResearchStore.QuantumBreakTitleId,
                ProgressionData = _lastQuantumBreakCandidate.Value,
                AchievementId = selectedKey,
                AchievementName = before?.Name ?? after?.Name ?? SelectedAchievement?.Name ?? "",
                Result = unlocked
                    ? "Achievement unlocked"
                    : requirementsChanged
                        ? "Progress changed"
                        : selectedStateChanged || anyChanged || after == null
                            ? "Unknown state change"
                            : "No effect",
                BeforeState = before?.State,
                AfterState = after?.State,
                BeforeUnlockTime = before?.TimeUnlocked,
                AfterUnlockTime = after?.TimeUnlocked,
                BeforeRequirements = before?.Requirements.ToDictionary(item => item.Key, item => item.Value) ?? [],
                AfterRequirements = after?.Requirements.ToDictionary(item => item.Key, item => item.Value) ?? [],
                ObservedChanges = observedChanges
            });
        }

        private static string CompareAchievementSnapshots(
            Dictionary<string, AchievementTestSnapshot> before,
            Dictionary<string, AchievementTestSnapshot> after,
            int selectedId,
            out bool selectedChanged,
            out bool anyChanged)
        {
            selectedChanged = false;
            var lines = new List<string>();
            var selectedKey = selectedId.ToString(CultureInfo.InvariantCulture);
            foreach (var afterAchievement in after)
            {
                if (!before.TryGetValue(afterAchievement.Key, out var beforeAchievement))
                    continue;

                var isSelected = afterAchievement.Key == selectedKey;
                var becameAchieved = !string.Equals(beforeAchievement.State, StringConstants.Achieved, StringComparison.OrdinalIgnoreCase)
                    && string.Equals(afterAchievement.Value.State, StringConstants.Achieved, StringComparison.OrdinalIgnoreCase);
                if (isSelected && becameAchieved)
                {
                    selectedChanged = true;
                    lines.Add($"Selected achievement unlocked: {afterAchievement.Value.Name}.");
                }

                if (beforeAchievement.State != afterAchievement.Value.State)
                {
                    lines.Add($"{afterAchievement.Value.Name}: state {beforeAchievement.State} -> {afterAchievement.Value.State}");
                    selectedChanged |= isSelected;
                }

                if (beforeAchievement.TimeUnlocked != afterAchievement.Value.TimeUnlocked)
                {
                    lines.Add($"{afterAchievement.Value.Name}: timeUnlocked {beforeAchievement.TimeUnlocked ?? "<none>"} -> {afterAchievement.Value.TimeUnlocked ?? "<none>"}");
                    selectedChanged |= isSelected;
                }

                foreach (var requirement in afterAchievement.Value.Requirements)
                {
                    beforeAchievement.Requirements.TryGetValue(requirement.Key, out var previousValue);
                    if (previousValue == requirement.Value)
                        continue;

                    var delta = decimal.TryParse(previousValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var beforeNumber) &&
                                decimal.TryParse(requirement.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var afterNumber)
                        ? $" ({afterNumber - beforeNumber:+0.################;-0.################;0})"
                        : "";
                    lines.Add($"{afterAchievement.Value.Name}: requirement {requirement.Key}: {previousValue ?? "<none>"} -> {requirement.Value ?? "<none>"}{delta}");
                    selectedChanged |= isSelected;
                }
            }

            anyChanged = lines.Count > 0;
            return !anyChanged
                ? "No achievement state, unlock time, or requirement changes detected."
                : string.Join(Environment.NewLine, lines);
        }

        private async void InitializeViewModel()
        {
            if (IsSelectedGame360)
                Unlockable = false;
            await LoadGameInfo();
            await LoadAchievements();
            if (HomeViewModel.Settings.AutoSpooferEnabled)
                SpoofGame();
            TitleIDEnabled = true;
            IsInitialized = true;
            NewGame = false;
        }


        private async Task LoadGameInfo()
        {
            // Check for a valid TitleID and set overrides
            if (TitleID != "0")
            {
                TitleIDOverride = TitleID;
                TitleID = "0";
            }

            GameInfo = string.Empty;

            // Fetch game information
            var gameInfoResponse = await _xboxRestAPI.Value.GetGameTitleAsync(HomeViewModel.XUIDOnly, TitleIDOverride);

            // Handle response validation and set properties accordingly
            if (gameInfoResponse?.Titles?.Any() != true)
            {
                GameName = "Error";
                IsTitleIDValid = false;
                return;
            }

            var gameTitle = gameInfoResponse.Titles.FirstOrDefault();
            if (gameTitle != null)
            {
                IsSelectedGame360 = gameTitle.Devices.Contains("Xbox360") || gameTitle.Devices.Contains("Mobile");
                GameName = gameTitle.Name;
                IsTitleIDValid = true;
            }
        }

        private async void SpoofGame()
        {
            if (HomeViewModel.SpoofingStatus == 1)
            {
                if (HomeViewModel.SpoofedTitleID == TitleIDOverride)
                {
                    GameInfo = "Manually Spoofing";
                    GameName = GameInfoResponse.Titles[0].Name;
                }
                else
                {
                    GameInfo = "Spoofing Another Game";
                    GameName = GameInfoResponse.Titles[0].Name;
                }
            }
            else
            {
                HomeViewModel.AutoSpoofedTitleID = TitleIDOverride;
                HomeViewModel.SpoofingStatus = 2;
                GameInfo = "Auto Spoofing";
                if (GameInfoResponse.Titles.Any())
                {
                    GameName = GameInfoResponse.Titles[0].Name;
                }

                await Task.Run(() => Spoofing());
                if (HomeViewModel.SpoofingStatus == 1)
                {
                    if (HomeViewModel.SpoofedTitleID == HomeViewModel.AutoSpoofedTitleID)
                    {
                        GameInfo = "Manually Spoofing";
                        GameName = GameInfoResponse.Titles[0].Name;
                    }
                    else
                    {
                        GameInfo = "Spoofing Another Game";
                        GameName = GameInfoResponse.Titles[0].Name;
                    }
                }
                HomeViewModel.AutoSpoofedTitleID = "0";
            }


        }

        public async Task Spoofing()
        {
            await _xboxRestAPI.Value.SendHeartbeatAsync(HomeViewModel.XUIDOnly, HomeViewModel.AutoSpoofedTitleID);
            var i = 0;
            Thread.Sleep(1000);
            SpoofingUpdate = false;
            while (!SpoofingUpdate)
            {
                if (i == 300)
                {
                    await _xboxRestAPI.Value.SendHeartbeatAsync(HomeViewModel.XUIDOnly, HomeViewModel.AutoSpoofedTitleID);
                    i = 0;
                }
                else
                {
                    if (SpoofingUpdate)
                    {

                        break;
                    }
                    i++;
                }
                Thread.Sleep(1000);
            }
        }

        private async Task LoadAchievements()
        {

            Achievements.Clear();
            DGAchievements.Clear();
            SelectedAchievement = null;
            // clears unlocked achievements from dictionary
            _unlockedAchievements.Clear();
            IsEventBased = false;
            IsEventUnlockAvailable = false;
            if (!IsTitleIDValid)
                return;
            if (!IsSelectedGame360)
            {
                Unlockable = true;
                AchievementResponse = await _xboxRestAPI.Value.GetAchievementsForTitleAsync(HomeViewModel.XUIDOnly, TitleIDOverride);
                try
                {
                    if (AchievementResponse.achievements[0].progression.requirements.Any())
                    {
                        if (AchievementResponse.achievements[0].progression.requirements[0].id !=
                            StringConstants.ZeroUid)
                        {
                            Unlockable = false;
                        }
                        else
                        {
                            Unlockable = true;
                        }
                    }
                }
                catch
                {
                    _snackbarService.Show("Error: No Achievements", $"There were no achievements returned from the API", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }
                for (int i = 0; i < AchievementResponse.achievements.Count; i++)
                {
                    //absolutely fucking dogwater event based check
                    if (AchievementResponse.achievements[i].progression.requirements.Any())
                    {
                        if (AchievementResponse.achievements[i].progression.requirements[0].id !=
                            StringConstants.ZeroUid)
                        {
                            Unlockable = false;
                            IsEventBased = true;
                        }
                        else if (!IsEventBased)
                        {
                            Unlockable = true;
                        }
                    }
                    var rewardnameplaceholder = "";
                    var rewarddescriptionplaceholder = "";
                    var rewardvalueplaceholder = "";
                    var rewardtypeplaceholder = "";
                    var rewardmediaAssetplaceholder = "";
                    var rewardvalueTypeplaceholder = "";
                    try
                    {
                        rewardnameplaceholder = AchievementResponse.achievements[i].rewards[0].name;
                        rewarddescriptionplaceholder = AchievementResponse.achievements[i].rewards[0].description;
                        rewardvalueplaceholder = AchievementResponse.achievements[i].rewards[0].value;
                        rewardtypeplaceholder = AchievementResponse.achievements[i].rewards[0].type;
                        //rewardmediaAssetplaceholder = AchievementResponse.achievements[i].rewards[0].mediaAsset;
                        rewardvalueTypeplaceholder = AchievementResponse.achievements[i].rewards[0].valueType;
                    }
                    catch
                    {
                        rewardnameplaceholder = "N/A";
                        rewarddescriptionplaceholder = "N/A";
                        rewardvalueplaceholder = "N/A";
                        rewardtypeplaceholder = "N/A";
                        rewardmediaAssetplaceholder = "N/A";
                        rewardvalueTypeplaceholder = "N/A";
                    }

                    var mediaAsset = new MediaAsset
                    {
                        name = AchievementResponse.achievements[i].mediaAssets[0].name,
                        type = AchievementResponse.achievements[i].mediaAssets[0].type,
                        url = AchievementResponse.achievements[i].mediaAssets[0].url
                    };
                    var titleAssociation = new TitleAssociation
                    {
                        name = AchievementResponse.achievements[i].titleAssociations[0].name,
                        id = AchievementResponse.achievements[i].titleAssociations[0].id
                    };
                    var progression = new AchievementProgression
                    {
                        timeUnlocked = AchievementResponse.achievements[i].progression.timeUnlocked
                    };
                    var rewards = new AchievementRewards
                    {
                        name = rewardnameplaceholder,
                        description = rewarddescriptionplaceholder,
                        value = rewardvalueplaceholder,
                        type = rewardtypeplaceholder,
                        mediaAsset = mediaAsset,
                        valueType = rewardvalueTypeplaceholder
                    };


                    Achievements.Add(new OneCoreAchievementResponse()
                    {
                        id = AchievementResponse.achievements[i].id,
                        serviceConfigId = AchievementResponse.achievements[i].serviceConfigId,
                        name = AchievementResponse.achievements[i].name,
                        titleAssociations = new List<TitleAssociation>() { titleAssociation },
                        progressState = AchievementResponse.achievements[i].progressState,
                        progression = progression,
                        mediaAssets = new List<MediaAsset>() { mediaAsset },
                        platforms = AchievementResponse.achievements[i].platforms,
                        isSecret = AchievementResponse.achievements[i].isSecret,
                        description = AchievementResponse.achievements[i].description,
                        lockedDescription = AchievementResponse.achievements[i].lockedDescription,
                        productId = AchievementResponse.achievements[i].productId,
                        achievementType = AchievementResponse.achievements[i].achievementType,
                        participationType = AchievementResponse.achievements[i].participationType,
                        timeWindow = AchievementResponse.achievements[i].timeWindow,
                        rewards = new List<AchievementRewards>() { rewards },
                        estimatedTime = AchievementResponse.achievements[i].estimatedTime,
                        deeplink = AchievementResponse.achievements[i].deeplink,
                        isRevoked = AchievementResponse.achievements[i].isRevoked,
                        raritycurrentCategory = AchievementResponse.achievements[i].rarity.currentCategory,
                        raritycurrentPercentage = AchievementResponse.achievements[i].rarity.currentPercentage
                    }
                    );
                }
                foreach (var achievement in Achievements)
                {
                    var gamerscore = 0;
                    if (achievement.rewards[0].type == StringConstants.Gamerscore)
                    {
                        gamerscore = int.Parse(achievement.rewards[0].value);
                    }
                    DGAchievements.Add(new DGAchievement()
                    {
                        Index = Achievements.IndexOf(achievement),
                        ID = int.Parse(achievement.id),
                        Name = achievement.name,
                        Description = achievement.description,
                        IsSecret = achievement.isSecret,
                        DateUnlocked = DateTime.Parse(achievement.progression.timeUnlocked),
                        Gamerscore = gamerscore,
                        RarityPercentage = float.Parse(achievement.raritycurrentPercentage, CultureInfo.InvariantCulture),
                        RarityCategory = achievement.raritycurrentCategory,
                        ProgressState = achievement.progressState,
                        IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                    });
                }
            }
            else
            {
                Unlockable = false;
                Xbox360AchievementResponse = await _xboxRestAPI.Value.GetAchievementsFor360TitleAsync(HomeViewModel.XUIDOnly, TitleIDOverride);
                if (Xbox360AchievementResponse?.achievements.Count == 0)
                {
                    IsSelectedGame360 = false;
                    LoadAchievements();
                    return;
                }
                //cut down version of the code to display minimal information about 360 achievements
                for (int i = 0; i < Xbox360AchievementResponse?.achievements.Count; i++)
                {
                    var rewards = new AchievementRewards
                    {
                        value = Xbox360AchievementResponse.achievements[i].gamerscore.ToString(),
                        valueType = "N/a"
                    };
                    var progression = new AchievementProgression
                    {
                        timeUnlocked = Xbox360AchievementResponse.achievements[i].timeUnlocked
                    };

                    Achievements.Add(new OneCoreAchievementResponse()
                    {
                        id = Xbox360AchievementResponse.achievements[i].id.ToString(),
                        name = Xbox360AchievementResponse.achievements[i].name,
                        isSecret = Xbox360AchievementResponse.achievements[i].isSecret,
                        description = Xbox360AchievementResponse.achievements[i].description,
                        rewards = new List<AchievementRewards>() { rewards },
                        raritycurrentCategory = Xbox360AchievementResponse.achievements[i].rarity.currentCategory,
                        raritycurrentPercentage = Xbox360AchievementResponse.achievements[i].rarity.currentPercentage,
                        progression = progression
                    }
                    );
                }
                foreach (var achievement in Achievements)
                {
                    var gamerscore = 0;
                    if (achievement.rewards[0].type == "Gamerscore")
                    {
                        gamerscore = int.Parse(achievement.rewards[0].value);
                    }
                    DGAchievements.Add(new DGAchievement()
                    {
                        Index = Achievements.IndexOf(achievement),
                        ID = int.Parse(achievement.id),
                        Name = achievement.name,
                        Description = achievement.description,
                        IsSecret = achievement.isSecret,
                        DateUnlocked = DateTime.Parse(achievement.progression.timeUnlocked),
                        Gamerscore = gamerscore,
                        RarityPercentage = float.Parse(achievement.raritycurrentPercentage, CultureInfo.InvariantCulture),
                        RarityCategory = achievement.raritycurrentCategory,
                        ProgressState = achievement.progressState,
                        IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable
                    });
                }
            }

            if (IsSelectedGame360)
            {
                _snackbarService.Show("Warning: Unsupported Game", $"This tool does not/will not support Xbox 360 titles. To unlock 360 achievements, you can try https://www.wemod.com/horizon", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
                IsUnlockAllEnabled = false;

                return;
            }

            if (IsEventBased)
            {
                //Event based logic
                string DataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\XAU\\Events\\Data.json";
                var data = JObject.Parse(File.ReadAllText(DataPath));
                JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
                List<int> SupportedGames = SupportedGamesJ.ToObject<List<int>>();
                if (SupportedGames.Contains(int.Parse(TitleIDOverride)))
                {
                    Unlockable = true;
                    IsEventUnlockAvailable = true;
                    EventsData = (dynamic)(JObject)data[TitleIDOverride];
                    foreach (var achievement in DGAchievements)
                    {
                        if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != StringConstants.Achieved)
                        {
                            achievement.IsUnlockable = true;
                        }
                    }
                }
                SetEventMappingStatuses();
                CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
            }



            if (!Unlockable)
            {
                _snackbarService.Show("Warning: Unsupported Game", $"This tool does not support this Event Based title", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }
            else if (IsEventBased && EventsData.FullySupported == false)
            {
                _snackbarService.Show("Warning: Partially Unsupported Game", $"This tool does not fully support this title. Not all achievements are unlockable", ControlAppearance.Caution,
                                       new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }

            if (HomeViewModel.Settings.UnlockAllEnabled && Unlockable && !IsEventBased)
                IsUnlockAllEnabled = Unlockable;
            else
                IsUnlockAllEnabled = false;
        }

        private void SetEventMappingStatuses()
        {
            var mappings = IsEventUnlockAvailable ? EventsData.Achievements as JObject : null;
            foreach (var achievement in DGAchievements)
            {
                if (mappings == null)
                {
                    achievement.EventMappingStatus = "Unsupported: title has no event mapping data";
                    continue;
                }

                var mapping = mappings[achievement.ID.ToString()];
                achievement.EventMappingStatus = mapping == null
                    ? "Unsupported: missing event mapping"
                    : $"Mapped: {DescribeEventMapping(mapping)}";
            }

            if (SelectedAchievement != null)
                OnSelectedAchievementChanged(SelectedAchievement);
        }

        private static string DescribeEventMapping(JToken mapping)
        {
            return string.Join(", ", mapping.Children<JProperty>().Select(replacement =>
            {
                var target = replacement.Value["Target"]?.ToString() ?? replacement.Name;
                var value = replacement.Value["Replacement"]?.ToString();
                if (!string.IsNullOrWhiteSpace(value))
                {
                    return value.Length <= 48 && !value.StartsWith('{')
                        ? $"{target} = {value}"
                        : $"{target} = <mapped value>";
                }

                var min = replacement.Value["Min"]?.ToString();
                var max = replacement.Value["Max"]?.ToString();
                return min != null && max != null
                    ? $"{target} = {min}..{max}"
                    : $"{target} ({replacement.Value["ReplacementType"]})";
            }));
        }

        public async void UnlockAchievement(int AchievementIndex)
        {
            if (!IsEventBased)
            {
                try
                {
                    await _xboxRestAPI.Value.UnlockTitleBasedAchievementAsync(AchievementResponse.achievements[0].serviceConfigId, AchievementResponse.achievements[0].titleAssociations[0].id, HomeViewModel.XUIDOnly, DGAchievements[AchievementIndex].ID.ToString(), HomeViewModel.Settings.FakeSignatureEnabled);

                    _snackbarService.Show("Achievement Unlocked", $"{DGAchievements[AchievementIndex].Name} has been unlocked",
                        ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                    DGAchievements[AchievementIndex].IsUnlockable = false;
                    DGAchievements[AchievementIndex].ProgressState = StringConstants.Achieved;
                    DGAchievements[AchievementIndex].DateUnlocked = DateTime.Now;

                    // Add achievement to the dictionary. this will fix search & filter unlockable state
                    var unlockedAchievement = DGAchievements[AchievementIndex];
                    unlockedAchievement.IsUnlockable = false;
                    unlockedAchievement.ProgressState = StringConstants.Achieved;
                    unlockedAchievement.DateUnlocked = DateTime.Now;

                    if (!_unlockedAchievements.ContainsKey(unlockedAchievement.ID))
                    {
                        _unlockedAchievements.Add(unlockedAchievement.ID, unlockedAchievement);
                    }

                    CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                }
                catch (HttpRequestException ex)
                {
                    _snackbarService.Show("Error: Achievement Not Unlocked",
                        $"{DGAchievements[AchievementIndex].Name} was not unlocked", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }
            }
            else
            {
                if (EventsToken == null || HomeViewModel.IsEventsTokenExpired())
                {
                    ContentDialogResult result = await _contentDialogService.ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = EventsToken == null
                                ? "Error: You have not set an events token"
                                : "Error: Your events token has expired",
                            Content = EventsToken == null
                                ? "To unlock event based games you must supply an events token. You can set one up in Settings."
                                : "Your events token has expired and needs to be refreshed before unlocking.",
                            PrimaryButtonText = "Go to Settings",
                            CloseButtonText = "Close",
                        });

                    if (result == ContentDialogResult.Primary)
                        _navigationService.Navigate(typeof(SettingsPage));

                    return;
                }

                // TODO: move this over to the rest api?
                var requestbody = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\XAU\\Events\\{TitleIDOverride}.json");
                // Experimental: Xbox may ignore this event telemetry timestamp and use server-side receipt time.
                if (!TryGetEventUnlockTimestampUtc(out DateTime timestamp))
                {
                    return;
                }
                foreach (var i in EventsData.Achievements[DGAchievements[AchievementIndex].ID.ToString()])
                {
                    var ReplacementData = i.Value;
                    switch (ReplacementData.ReplacementType.ToString())
                    {
                        case "Replace":
                            {
                                requestbody = requestbody.Replace(ReplacementData.Target.ToString(), ReplacementData.Replacement.ToString());
                                break;
                            }
                        case "RangeInt":
                            {
                                int min = ReplacementData.Min;
                                int max = ReplacementData.Max;
                                Random random = new Random();
                                int randomint = random.Next(min, max);
                                requestbody = requestbody.Replace(ReplacementData.Target.ToString(), randomint.ToString());
                                break;
                            }
                        case "RangeFloat":
                            {
                                float min = ReplacementData.Min;
                                float max = ReplacementData.Max;
                                Random random = new Random();
                                float randomfloat = (float)random.NextDouble() * (max - min) + min;
                                requestbody = requestbody.Replace(ReplacementData.Target.ToString(), randomfloat.ToString());
                                break;
                            }
                        case "StupidFuckingLDAPTimestamp":
                            {
                                long ldapTimestamp = DateTime.Now.ToFileTime();
                                requestbody = requestbody.Replace(ReplacementData.Target.ToString(), ldapTimestamp.ToString());
                                break;
                            }
                        default:
                            {
                                _snackbarService.Show("Error: Bad Achievement Data", "Something went wrong with the achievement data", ControlAppearance.Danger,
                                                                                  new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                                return;
                            }

                    }
                }
                requestbody = requestbody.Replace("REPLACETIME", timestamp.ToString("yyyy-MM-ddTHH:mm:ss.fffffffZ"));
                requestbody = requestbody.Replace("REPLACESEQ", DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
                requestbody = requestbody.Replace("REPLACEXUID", HomeViewModel.XUIDOnly);
                requestbody = JObject.Parse(requestbody).ToString(Formatting.None);
                var bodyconverted = new StringContent(requestbody, Encoding.UTF8, "application/x-json-stream");
                try
                {
                    await _xboxRestAPI.Value.UnlockEventBasedAchievement(EventsToken, bodyconverted);

                    _snackbarService.Show("Achievement Unlocked", $"{DGAchievements[AchievementIndex].Name} has been unlocked",
                        ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                    DGAchievements[AchievementIndex].IsUnlockable = false;
                    DGAchievements[AchievementIndex].ProgressState = "Achieved";
                    DGAchievements[AchievementIndex].DateUnlocked = DateTime.Now;
                    CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                }
                catch
                {
                    _snackbarService.Show("Error: Achievement Not Unlocked",
                        $"{DGAchievements[AchievementIndex].Name} was not unlocked", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }

            }

        }

        private bool TryGetEventUnlockTimestampUtc(out DateTime timestampUtc)
        {
            timestampUtc = DateTime.UtcNow;
            if (!UseCustomEventUnlockTime)
            {
                return true;
            }

            if (CustomEventUnlockDate == null)
            {
                _snackbarService.Show("Error: Invalid Event Time", "Select a valid event unlock date.", ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return false;
            }

            var timeText = CustomEventUnlockTimeText?.Trim();
            if (string.IsNullOrWhiteSpace(timeText) ||
                (!TimeSpan.TryParse(timeText, CultureInfo.CurrentCulture, out var timeOfDay) &&
                 !TimeSpan.TryParse(timeText, CultureInfo.InvariantCulture, out timeOfDay)) ||
                timeOfDay < TimeSpan.Zero ||
                timeOfDay >= TimeSpan.FromDays(1))
            {
                _snackbarService.Show("Error: Invalid Event Time", "Enter the event unlock time as HH:mm or HH:mm:ss.", ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return false;
            }

            var localTimestamp = DateTime.SpecifyKind(CustomEventUnlockDate.Value.Date.Add(timeOfDay), DateTimeKind.Local);
            timestampUtc = localTimestamp.ToUniversalTime();
            return true;
        }

        [RelayCommand]
        public async Task UnlockAll()
        {
            var lockedAchievementIds = Achievements.Where(o => o.progressState != StringConstants.Achieved).Select(o => o.id).ToList();
            try
            {
                await _xboxRestAPI.Value.UnlockTitleBasedAchievementsAsync(serviceConfigId: AchievementResponse.achievements[0].serviceConfigId,
                    titleId: AchievementResponse.achievements[0].titleAssociations[0].id, xuid: HomeViewModel.XUIDOnly, achievementIds: lockedAchievementIds, useFakeSignature: HomeViewModel.Settings.FakeSignatureEnabled);

                _snackbarService.Show("All Achievements Unlocked", $"All Achievements for this game have been unlocked",
                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                var unlocktime = DateTime.Now;
                foreach (DGAchievement achievement in DGAchievements)
                {

                    if (achievement.ProgressState != StringConstants.Achieved)
                    {
                        achievement.IsUnlockable = false;
                        achievement.ProgressState = StringConstants.Achieved;
                        achievement.DateUnlocked = unlocktime;
                    }
                }
                CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
            }
            catch (HttpRequestException hre)
            {
                _snackbarService.Show("Error: Achievements Not Unlocked",
                                        $"{hre.Message}", ControlAppearance.Danger,
                                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        [RelayCommand]
        public async Task RefreshAchievements()
        {
            // clears unlocked achievements from dictionary
            _unlockedAchievements.Clear();

            await LoadGameInfo();
            await LoadAchievements();
            NewGame = false;
            if (HomeViewModel.Settings.AutoSpooferEnabled)
                SpoofGame();
        }

        [RelayCommand]
        public async Task SearchAndFilterAchievements()
        {
            try
            {
                if (IsEventBased)
                {
                    string DataPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Events", "Data.json");
                    var data = JObject.Parse(File.ReadAllText(DataPath));
                    JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
                    List<int> SupportedGames = SupportedGamesJ.ToObject<List<int>>();
                    if (SupportedGames.Contains(int.Parse(TitleIDOverride)))
                    {
                        Unlockable = true;
                        EventsData = (dynamic)data[TitleIDOverride];
                    }
                }

                CollectionViewSource.GetDefaultView(DGAchievements).Refresh();

                if (string.IsNullOrWhiteSpace(SearchText) && !IsFiltered)
                {
                    _snackbarService.Show("Error", $"Please Enter Query Text", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                DGAchievements.Clear();

                if (string.IsNullOrWhiteSpace(SearchText) && IsFiltered)
                {
                    foreach (var achievement in Achievements)
                    {
                        var gamerscore = 0;
                        if (achievement.rewards[0].type == StringConstants.Gamerscore)
                        {
                            gamerscore = int.Parse(achievement.rewards[0].value);
                        }

                        var dgAchievement = new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progression.timeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage, CultureInfo.InvariantCulture),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        };

                        // Override with the state from _unlockedAchievements dictionary if it exists.
                        if (_unlockedAchievements.ContainsKey(dgAchievement.ID))
                        {
                            var unlocked = _unlockedAchievements[dgAchievement.ID];
                            dgAchievement.IsUnlockable = unlocked.IsUnlockable;
                            dgAchievement.ProgressState = unlocked.ProgressState;
                            dgAchievement.DateUnlocked = unlocked.DateUnlocked;
                        }

                        DGAchievements.Add(dgAchievement);
                    }

                    if (IsEventBased && Unlockable)
                    {
                        foreach (var achievement in DGAchievements)
                        {
                            if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != StringConstants.Achieved)
                            {
                                achievement.IsUnlockable = true;
                            }
                        }
                        CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                    }
                    if (IsEventBased)
                    {
                        SetEventMappingStatuses();
                        CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                    }
                    IsFiltered = false;
                    return;
                }

                bool achievementsFound = false;

                foreach (var achievement in Achievements)
                {
                    if (achievement.name.Contains(SearchText, StringComparison.OrdinalIgnoreCase) || achievement.description.Contains(SearchText, StringComparison.OrdinalIgnoreCase))
                    {
                        var gamerscore = 0;
                        if (achievement.rewards[0].type == StringConstants.Gamerscore)
                        {
                            gamerscore = int.Parse(achievement.rewards[0].value);
                        }

                        var dgAchievement = new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progression.timeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage, CultureInfo.InvariantCulture),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        };

                        // Override with the state from _unlockedAchievements dictionary if it exists.
                        if (_unlockedAchievements.ContainsKey(dgAchievement.ID))
                        {
                            var unlockedAchievement = _unlockedAchievements[dgAchievement.ID];
                            dgAchievement.IsUnlockable = unlockedAchievement.IsUnlockable;
                            dgAchievement.ProgressState = unlockedAchievement.ProgressState;
                            dgAchievement.DateUnlocked = unlockedAchievement.DateUnlocked;
                        }

                        DGAchievements.Add(dgAchievement);
                        achievementsFound = true;
                    }
                }

                if (!achievementsFound)
                {
                    _snackbarService.Show("Error", $"No Achievements Found", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }

                if (IsEventBased && Unlockable)
                {
                    foreach (var achievement in DGAchievements)
                    {
                        if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != StringConstants.Achieved)
                        {
                            achievement.IsUnlockable = true;
                        }
                    }
                    CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                }
                if (IsEventBased)
                {
                    SetEventMappingStatuses();
                    CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                }

                IsFiltered = true;
            }
            catch (Exception ex)
            {
                // Log exception (ex) if necessary
                _snackbarService.Show("Error", "An error occurred while searching. Please try again.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }

            await Task.CompletedTask;
        }
    }
}
