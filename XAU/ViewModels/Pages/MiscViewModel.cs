using HtmlAgilityPack;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json.Linq;
using System.Data;
using System.Diagnostics;
using System.DirectoryServices;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Windows.Input;
using XAU.Util.Diagnostics;
using XAU.Util.Logging;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using Wpf.Ui.Services;


namespace XAU.ViewModels.Pages
{
    public partial class MiscViewModel : ObservableObject, INavigationAware
    {
        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH));



        public MiscViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }

        // Xbox Live endpoint health check. It deliberately lives here (Misc) and runs ONLY on-demand,
        // so the verbose per-service detail never clutters the Home landing page -- Home only offers
        // a button that opens the official status page.
        [ObservableProperty] private string _xboxServiceStatus = "not checked";
        [ObservableProperty] private bool _isCheckingXboxServiceHealth = false;
        [ObservableProperty] private List<string> _xboxServiceStatusLines = new List<string>();

        /// <summary>
        /// Probes Xbox Live endpoint reachability without authentication. An HTTP response
        /// does not prove that the token can read stats or write presence; a timeout may also be
        /// caused by the user's network rather than the service.
        /// </summary>
        [RelayCommand]
        private async Task CheckXboxServiceHealth()
        {
            if (IsCheckingXboxServiceHealth)
                return;
            IsCheckingXboxServiceHealth = true;
            try
            {
                var report = await _xboxRestAPI.Value.CheckServiceHealthAsync();
                XboxServiceStatus = XblServiceHealth.OverallStatus(report.Results);
                XboxServiceStatusLines = XblServiceHealth.StatusLines(report.Results);
                if (report.LooksLikeServiceOutage)
                    _snackbarService.Show("Xbox Live status",
                        "A service errored or could not be reached. This could be Xbox Live or your local network.",
                        ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
                else
                    _snackbarService.Show("Xbox Live status",
                        "Endpoints responded to unauthenticated checks. This does not verify login, stats or presence writes.",
                        ControlAppearance.Info, new SymbolIcon(SymbolRegular.Info24), _snackbarDuration);
            }
            catch (Exception ex)
            {
                XboxServiceStatus = "check failed";
                XboxServiceStatusLines = new List<string>();
                Debug.WriteLine($"[XBLSTATUS] check threw {ex.GetType().Name}: {ex.Message}");
            }
            finally
            {
                IsCheckingXboxServiceHealth = false;
            }
        }

        /// <summary>
        /// Pulls the MinutesPlayed figure (in minutes) out of a userstats batch response by scanning
        /// EVERY stat bucket for one whose Name is "MinutesPlayed", instead of trusting [0][0]. Returns
        /// the LARGEST matching value (the all-time aggregate dominates any seasonal/device slice) or -1
        /// when no MinutesPlayed stat exists at all (caller then shows "Unknown").
        /// </summary>
        public static double GetMinutesPlayed(GameStatsResponse response)
        {
            if (response == null || response.StatListsCollection == null)
                return -1;

            double best = -1;
            foreach (var list in response.StatListsCollection)
            {
                if (list == null || list.Stats == null)
                    continue;
                foreach (var stat in list.Stats)
                {
                    if (stat == null)
                        continue;
                    // Only ever consider the MinutesPlayed statistic, so a Gamerscore/Score/etc. can
                    // never be mistaken for play-time (which the old positional [0][0] read risked).
                    if (!string.Equals(stat.Name?.Trim(), "MinutesPlayed", StringComparison.OrdinalIgnoreCase))
                        continue;

                    // The figure usually sits in Value; some responses surface it only via Properties /
                    // GroupProperties, so fall back to a numeric found there before giving up.
                    if ((TryParseMinutes(stat.Value, out double value)
                            || TryParseAnyNumeric(stat.Properties, out value)
                            || TryParseAnyNumeric(stat.GroupProperties, out value))
                        && value > best)
                    {
                        best = value;
                    }
                }
            }
            return best;
        }

        private static bool TryParseMinutes(string candidate, out double value)
        {
            value = 0;
            return !string.IsNullOrWhiteSpace(candidate)
                && double.TryParse(candidate.Trim(),
                    System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                    System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        private static bool TryParseAnyNumeric(Dictionary<string, object> props, out double value)
        {
            value = 0;
            if (props == null)
                return false;
            foreach (var kv in props)
            {
                if (kv.Value == null)
                    continue;
                if (double.TryParse(kv.Value.ToString()?.Trim(),
                        System.Globalization.NumberStyles.Float | System.Globalization.NumberStyles.AllowThousands,
                        System.Globalization.CultureInfo.InvariantCulture, out value))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Debug-only: renders every (listIndex, statIndex) Name/Type/Value so a live run shows
        /// WHICH bucket actually carries the minutesplayed figure (see [STATDBG] log).</summary>
        public static string DumpStatBuckets(GameStatsResponse response)
        {
            if (response == null || response.StatListsCollection == null)
                return "<no stat lists>";

            var sb = new StringBuilder();
            for (int i = 0; i < response.StatListsCollection.Count; i++)
            {
                var list = response.StatListsCollection[i];
                if (list == null || list.Stats == null)
                    continue;
                for (int j = 0; j < list.Stats.Count; j++)
                {
                    var s = list.Stats[j];
                    sb.Append($"[{i}][{j}] name={s?.Name} type={s?.Type} value={(s?.Value == null ? "<null>" : "\"" + s.Value + "\"")}");
                    if (s?.Properties != null && s.Properties.Count > 0)
                        sb.Append(" props={" + string.Join(",", s.Properties.Select(p => p.Key + ":" + p.Value)) + "}");
                    if (s?.GroupProperties != null && s.GroupProperties.Count > 0)
                        sb.Append(" grp={" + string.Join(",", s.GroupProperties.Select(p => p.Key + ":" + p.Value)) + "}");
                    sb.Append("; ");
                }
            }
            return sb.Length == 0 ? "<empty>" : sb.ToString();
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
        [ObservableProperty] private string? _gameImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gameTime = "Time Played: ";
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _currentSpoofingID = "";
        [ObservableProperty] private string _newSpoofingID = "";
        [ObservableProperty] private string _spoofingText = "Spoofing Not Started";
        [ObservableProperty] private string _spoofingButtonText = "Start Spoofing";
        private bool CurrentlySpoofing = false;
        private GameTitle GameInfoResponse;
        private GameStatsResponse GameStatsResponse;

        // A run owns its fetches, UI updates and heartbeats from the first button click onward.
        // In particular, a pending title fetch must not be mistaken for an idle spoofer.
        private CancellationTokenSource? _spoofCts;
        private bool _stoppingHeartbeat;
        internal Func<TimeSpan, CancellationToken, Task> HeartbeatDelayAsync { get; set; } = Task.Delay;
        internal Func<Task<bool>> RecoverAuthAsync { get; set; } = () =>
            HomeViewModel.Instance?.TryRecoverAuthAsync() ?? Task.FromResult(false);
        internal Func<TimeSpan, CancellationToken, Task> AuthRecoveryDelayAsync { get; set; } = Task.Delay;
        internal TimeSpan AuthRecoveryTimeout { get; set; } = TimeSpan.FromSeconds(60);

        [RelayCommand(AllowConcurrentExecutions = true)]
        public async Task SpooferButtonClicked()
        {
            if (_stoppingHeartbeat)
                return;

            if (_spoofCts is not null)
            {
                var run = _spoofCts;
                bool hadStarted = CurrentlySpoofing;
                run.Cancel();
                _spoofCts = null;
                CurrentlySpoofing = false;
                if (hadStarted)
                    HomeViewModel.SpoofedTitleID = "0";
                SpoofingText = "Spoofing Not Started";
                SpoofingButtonText = "Start Spoofing";
                GameName = "Name: ";
                GameTitleID = "Title ID: ";
                GamePFN = "PFN: ";
                GameType = "Type: ";
                GameGamepass = "Gamepass: ";
                GameDevices = "Devices: ";
                GameGamerscore = "Gamerscore: ?/?";
                GameImage = "pack://application:,,,/Assets/cirno.png";
                GameTime = "Time Played: ";
                // Cancelling before the fetch completed must leave an existing auto-spoof intact.
                if (hadStarted)
                {
                    HomeViewModel.SpoofingStatus = 0;
                }
                // No manual presence exists if the info fetch hasn't finished yet.
                if (hadStarted)
                {
                    _stoppingHeartbeat = true;
                    try
                    {
                        await _xboxRestAPI.Value.StopHeartbeatAsync(HomeViewModel.XUIDOnly);
                    }
                    catch (Exception ex)
                    {
                        DiagLog.Write($"[SPOOF] stop heartbeat failed: {ex.GetType().Name}: {ex.Message}");
                    }
                    finally
                    {
                        _stoppingHeartbeat = false;
                    }
                }
                return;
            }

            await SpoofGame();
        }

        /// <summary>
        /// Tears down the spoof loop after unrecoverable heartbeat failures (or any future hard-stop
        /// need): cancels the loop, resets the spoofing state, and shows WHY it stopped so the UI no
        /// longer claims a spoof is alive that has actually lapsed.
        /// </summary>
        private bool IsCurrentRun(CancellationTokenSource run) =>
            ReferenceEquals(_spoofCts, run) && !run.IsCancellationRequested;

        private void AbortSpoofing(CancellationTokenSource run, string reason)
        {
            if (!IsCurrentRun(run))
                return;
            bool hadStarted = CurrentlySpoofing;
            run.Cancel();
            _spoofCts = null;
            CurrentlySpoofing = false;
            if (hadStarted)
            {
                HomeViewModel.SpoofingStatus = 0;
                HomeViewModel.SpoofedTitleID = "0";
            }
            SpoofingText = reason;
            SpoofingButtonText = "Start Spoofing";
            DiagLog.Write($"[SPOOF] aborted: {reason}");
        }

        public async Task SpoofGame()
        {
            if (_spoofCts is not null || _stoppingHeartbeat)
                return;

            string titleId = NewSpoofingID;
            var run = new CancellationTokenSource();
            _spoofCts = run; // reserve the run before either info fetch can yield
            CurrentSpoofingID = titleId;
            try
            {
                // These API methods have no cancellation overload. An old completion must be
                // ignored, including when Stop -> Start happens during a pending fetch.
                var gameInfo = await _xboxRestAPI.Value.GetGameTitleAsync(HomeViewModel.XUIDOnly, titleId);
                if (!IsCurrentRun(run))
                    return;
                var gameStats = await _xboxRestAPI.Value.GetGameStatsAsync(HomeViewModel.XUIDOnly, titleId);
                if (!IsCurrentRun(run))
                    return;

                if (gameInfo?.Titles?.Any() != true || gameStats is null)
                {
                    _snackbarService.Show("Error: Unable to acquire game info or stats",
                        "The game info was invalid.", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    AbortSpoofing(run, "Spoofing Not Started");
                    return;
                }
                GameInfoResponse = gameInfo;
                GameStatsResponse = gameStats;

            try
            {
                GameName = "Name: " + GameInfoResponse.Titles[0].Name;
                GameImage = string.IsNullOrWhiteSpace(GameInfoResponse.Titles[0].DisplayImage)
                    ? "pack://application:,,,/Assets/cirno.png" : GameInfoResponse.Titles[0].DisplayImage;
                GameTitleID = "Title ID: " + GameInfoResponse.Titles[0].TitleId;
                GamePFN = "PFN: " + GameInfoResponse.Titles[0].Pfn;
                GameType = "Type: " + GameInfoResponse.Titles[0].Type;
                GameGamepass = "Gamepass: " + GameInfoResponse.Titles[0].GamePass?.IsGamePass;
                GameDevices = "Devices: ";
                foreach (var device in GameInfoResponse.Titles[0].Devices)
                {
                    GameDevices += device.ToString() + ", ";
                }

                GameDevices = GameDevices.Remove(GameDevices.Length - 2);
                GameGamerscore = "Gamerscore: " + GameInfoResponse.Titles[0].Achievement?.CurrentGamerscore.ToString() +
                                 "/" + GameInfoResponse.Titles[0].Achievement?.TotalGamerscore.ToString();
                // The userstats batch can return MORE THAN ONE "MinutesPlayed" bucket (an all-time
                // aggregate plus seasonal/device slices). The old code read [0][0] blindly, so when the
                // first bucket happened to be a 0-minute seasonal slice XAU showed 0h 0m even though
                // TrueAchievements (which selects by statistic + takes the aggregate) showed real time.
                // Select by statistic NAME across every bucket and keep the largest -- the all-time
                // aggregate always dominates any single slice. See GetMinutesPlayed.
                DiagLog.Write($"[STATDBG] stat-list count={GameStatsResponse?.StatListsCollection?.Count ?? 0}");
                double minutesPlayed = GetMinutesPlayed(GameStatsResponse);
                if (minutesPlayed >= 0)
                {
                    var timePlayed = TimeSpan.FromMinutes(minutesPlayed);
                    var formattedTime = $"{timePlayed.Days} Days, {timePlayed.Hours} Hours and {timePlayed.Minutes} minutes";
                    GameTime = "Time Played: " + formattedTime;
                }
                else
                {
                    // "Unknown" must NOT conflate two very different causes: (a) we're not signed in /
                    // the token isn't authorized for userstats (these calls never check the HTTP status
                    // code, so a denied/empty batch deserialises to an empty response and looks benign),
                    // vs (b) genuinely no MinutesPlayed data for this title. Split them so the user can
                    // tell, and log the evidence to the diagnostics file.
                    bool statsEmpty = GameStatsResponse == null
                        || GameStatsResponse.StatListsCollection == null
                        || GameStatsResponse.StatListsCollection.Count == 0;
                    DiagLog.Write(
                        $"[STATDBG] no usable MinutesPlayed: loggedIn={HomeViewModel._isLoggedIn}, " +
                        $"xauthLen={(HomeViewModel.XAUTH?.Length ?? 0)}, statsEmpty={statsEmpty}");

                    if (!HomeViewModel._isLoggedIn)
                        GameTime = "Time Played: unavailable (signed out)";
                    else if (statsEmpty)
                        GameTime = "Time Played: unavailable (stats empty - sign-in/token?)";
                    else
                        GameTime = "Time Played: Unknown (no MinutesPlayed stat)";
                }

            }
                catch (Exception ex)
                {
                    DiagLog.Write($"[SPOOF] invalid game info: {ex.GetType().Name}: {ex.Message}");
                    GameName = "Name: ";
                    _snackbarService.Show("Error: Invalid TitleID",
                        "The TitleID entered is invalid or does not return information from the API",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    AbortSpoofing(run, "Spoofing Not Started");
                    return;
                }

                string spoofedName = gameInfo.Titles[0].Name;
                if (!IsCurrentRun(run))
                    return;
                HomeViewModel.SpoofedTitleID = titleId;
                if (HomeViewModel.SpoofingStatus == 2)
                    AchievementsViewModel.SpoofingUpdate = true;
                HomeViewModel.SpoofingStatus = 1;
                CurrentlySpoofing = true;
                SpoofingButtonText = "Stop Spoofing";
                SpoofingText = $"Spoofing {spoofedName}";
                DiagLog.Write($"[SPOOF] starting spoof loop: titleId={titleId} name={spoofedName}");
                await Spoofing(run, titleId, spoofedName);
            }
            catch (Exception ex)
            {
                if (IsCurrentRun(run))
                {
                    DiagLog.Write($"[SPOOF] game fetch failed: {ex.GetType().Name}: {ex.Message}");
                    _snackbarService.Show("Error: Unable to acquire game info or stats",
                        $"The request failed: {ex.Message}", ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    AbortSpoofing(run, "Spoofing Not Started");
                }
            }
        }

        /// <summary>Heartbeats use a title ID frozen at start; a stopped run cannot publish UI.</summary>
        private async Task Spoofing(CancellationTokenSource run, string titleId, string titleName)
        {
            var watch = Stopwatch.StartNew();
            string xuid = HomeViewModel.XUIDOnly;
            int failures = 0;
            bool attemptedAuthRecovery = false;
            TimeSpan nextHeartbeat = TimeSpan.Zero;
            try
            {
                while (IsCurrentRun(run))
                {
                    if (HomeViewModel.XUIDOnly != xuid)
                    {
                        AbortSpoofing(run, "Spoofing Stopped: signed-in account changed.");
                        break;
                    }
                    if (watch.Elapsed >= nextHeartbeat)
                    {
                        try
                        {
                            // Cancel transport and loop; a request already sent may still land.
                            await _xboxRestAPI.Value.SendHeartbeatAsync(xuid, titleId, run.Token)
                                .WaitAsync(run.Token);
                            if (!IsCurrentRun(run))
                                break;
                            failures = 0;
                            nextHeartbeat = watch.Elapsed + TimeSpan.FromMinutes(5);
                        }
                        catch (OperationCanceledException) when (run.IsCancellationRequested)
                        {
                            break;
                        }
                        catch (Exception ex)
                        {
                            if (!IsCurrentRun(run))
                                break;
                            var status = (ex as HttpRequestException)?.StatusCode;
                            if (status == HttpStatusCode.Unauthorized)
                            {
                                if (attemptedAuthRecovery)
                                {
                                    AbortSpoofing(run, "Spoofing Stopped: refreshed token was also rejected (401).");
                                    break;
                                }
                                attemptedAuthRecovery = true;
                                string rejectedToken = HomeViewModel.XAUTH;
                                SpoofingText = "Spoofing paused: Refreshing authentication...";
                                bool refreshed = false;
                                try
                                {
                                    refreshed = await SpoofAuthRecovery.WaitForFreshTokenAsync(
                                        RecoverAuthAsync,
                                        () => HomeViewModel._isLoggedIn && HomeViewModel.XAUTHTested &&
                                            HomeViewModel.XAUTH != rejectedToken && HomeViewModel.XUIDOnly == xuid,
                                        AuthRecoveryTimeout, run.Token, AuthRecoveryDelayAsync);
                                }
                                catch (OperationCanceledException) when (run.IsCancellationRequested)
                                {
                                    break;
                                }
                                catch (Exception recoveryError)
                                {
                                    DiagLog.Write($"[SPOOF] auth recovery failed: {recoveryError.GetType().Name}");
                                }
                                if (!IsCurrentRun(run))
                                    break;
                                if (!refreshed)
                                {
                                    AbortSpoofing(run, "Spoofing Stopped: authentication did not refresh in time. Re-spoof after login.");
                                    break;
                                }
                                failures = 0;
                                nextHeartbeat = watch.Elapsed;
                                continue;
                            }

                            failures++;
                            DiagLog.Write($"[SPOOF] heartbeat failed ({failures} consecutive): {ex.GetType().Name}: {ex.Message}");
                            if (status == HttpStatusCode.Forbidden)
                                DiagLog.Write("[SPOOF] heartbeat 403 Forbidden: presence rejected this request; authorization cause is not established.");
                            if (status == HttpStatusCode.Forbidden || failures >= 3)
                            {
                                AbortSpoofing(run, status == HttpStatusCode.Forbidden
                                    ? "Spoofing Stopped: heartbeat rejected (403 Forbidden). Check authorization or try another sign-in method."
                                    : "Spoofing Stopped: heartbeats failing. Check authorization or network and try again.");
                                break;
                            }
                            nextHeartbeat = watch.Elapsed + TimeSpan.FromSeconds(1);
                        }
                    }

                    if (!IsCurrentRun(run))
                        break;
                    SpoofingText = $"Spoofing {titleName} For: {watch.Elapsed.ToString(@"hh\:mm\:ss")}";
                    await HeartbeatDelayAsync(TimeSpan.FromSeconds(1), run.Token);
                }
            }
            catch (OperationCanceledException) when (run.IsCancellationRequested)
            {
                // Stop or abort.
            }
            finally
            {
                DiagLog.Write($"[SPOOF] spoof loop for '{titleName}' (titleId={titleId}) ended.");
            }
        }

        #endregion

        #region GameSearch
        [ObservableProperty] private List<GameItem> _tSearchResults = new List<GameItem>();
        [ObservableProperty] private List<string> _tSearchTitleNames = new List<string>();
        [ObservableProperty] private string _tSearchText = "";
        [ObservableProperty] private string _tSearchGameName = "Name: ";
        [ObservableProperty] private string _tSearchGameTitleID = "";
        [ObservableProperty] private string _tSearchGameTitleBased = "Title Based: Unknown";

        private string GetDatabasePath()
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "TitleSearch", "xbox_games.db");
        }

        [RelayCommand]
        public async Task SearchGame()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TSearchText))
                {
                    TSearchTitleNames = new List<string>();
                    TSearchResults = new List<GameItem>();
                    return;
                }

                string dbPath = GetDatabasePath();

                if (!File.Exists(dbPath))
                {
                    _snackbarService.Show("Error", "Game database not found. Please wait for it to download.",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                var results = await Task.Run(() => SearchGamesInDatabase(dbPath, TSearchText));

                if (!results.Any())
                {
                    _snackbarService.Show("Error", $"No results were found for '{TSearchText}'",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    TSearchTitleNames = new List<string>();
                    TSearchResults = new List<GameItem>();
                    return;
                }

                results = results.OrderBy(game => game.Title, StringComparer.OrdinalIgnoreCase).ToList();

                TSearchResults = results;
                TSearchTitleNames = results.Select(game => game.Title).ToList();
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", $"Search failed: {ex.Message}",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                TSearchTitleNames = new List<string>();
                TSearchResults = new List<GameItem>();
            }
        }

        private List<GameItem> SearchGamesInDatabase(string dbPath, string searchText)
        {
            var results = new List<GameItem>();

            using var connection = new SqliteConnection($"Data Source={dbPath}");
            connection.Open();

            // Search for games that contain the search text (case-insensitive)
            string sql = @"
                    SELECT title, titleId, isTitleBased 
                    FROM games 
                    WHERE title LIKE @searchText 
                    ORDER BY title COLLATE NOCASE
                    LIMIT 100";

            using var command = new SqliteCommand(sql, connection);
            command.Parameters.AddWithValue("@searchText", $"%{searchText}%");

            using var reader = command.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new GameItem
                {
                    Title = reader.GetString("title"),
                    TitleId = reader.GetString("titleId"),
                    IsTitleBased = reader.GetInt32("isTitleBased") == 1
                });
            }

            return results;
        }

        public void DisplayGameInfo(int index)
        {
            try
            {
                if (TSearchResults == null || TSearchResults.Count <= index || index < 0)
                {
                    _snackbarService.Show("Error", "No game found at the selected index.",
                        ControlAppearance.Danger,
                        new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                var selectedGame = TSearchResults[index];

                TSearchGameName = selectedGame.Title;
                TSearchGameTitleID = selectedGame.TitleId;
                TSearchGameTitleBased = $"Title Based: {(selectedGame.IsTitleBased ? "True" : "False")}";

            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", "Failed to display game info.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        #endregion

        #region GamertagSearch
        [ObservableProperty] private string _gamertag = "";
        [ObservableProperty] private string _gamertagName = "Gamertag:";
        [ObservableProperty] private string _gamertagImage = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gamertagScore = "Gamerscore: ";
        [ObservableProperty] private string _gamertagXuid;
        [ObservableProperty] private bool _excludeZeroGamerscoreGames;
        [ObservableProperty] private bool _excludeXbox360Games;

        [RelayCommand]
        public async Task SearchGamertag()
        {
            if (string.IsNullOrWhiteSpace(Gamertag))
            {
                _snackbarService.Show("Error", "Please enter a valid gamertag.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            JObject profileData;
            try
            {
                profileData = await _xboxRestAPI.Value.GetGamertagProfileAsync(Gamertag) ?? new JObject();
            }
            catch (Exception)
            {
                _snackbarService.Show("Error", "Failed to fetch gamertag information.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
            var profileUsers = profileData["profileUsers"]?.FirstOrDefault();
            if (profileUsers == null)
            {
                _snackbarService.Show("Error", "Failed to fetch gamertag information.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }

            GamertagXuid = profileUsers["id"]?.ToString() ?? string.Empty;
            GamertagName = "Gamertag: " + profileUsers["settings"]?.FirstOrDefault(setting => setting["id"]?.ToString() == "Gamertag")?["value"]?.ToString() ?? "Unknown";
            GamertagScore = "Gamerscore: " + profileUsers["settings"]?.FirstOrDefault(setting => setting["id"]?.ToString() == "Gamerscore")?["value"]?.ToString() ?? "Unknown";
            GamertagImage = profileUsers["settings"]?.FirstOrDefault(setting => setting["id"]?.ToString() == "GameDisplayPicRaw")?["value"]?.ToString()?.Replace("&mode=Padding", "") ?? string.Empty;

        }

        public async Task ExportToCsvAsync()
        {
            if (string.IsNullOrWhiteSpace(GamertagXuid))
            {
                _snackbarService.Show("Error", "Search for a user first.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
            try
            {
                _snackbarService.Show("Fetching Games", "Trying to get games. This may take a moment depending on the number of games the user has.", ControlAppearance.Primary, new SymbolIcon(SymbolRegular.XboxController24), _snackbarDuration);
                var gamesResponse = await _xboxRestAPI.Value.GetGamesListAsync(GamertagXuid);

                if (gamesResponse == null || gamesResponse.Titles == null)
                {
                    await Task.Delay(2500);
                    _snackbarService.Show("Error", "Failed to fetch games list.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                if (gamesResponse.Titles.Count == 0)
                {
                    await Task.Delay(2500);
                    _snackbarService.Show("No Titles Found", "No games found for this user. This could be due to user privacy settings or other reasons.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                var sb = new StringBuilder();
                sb.AppendLine("\"Title ID\",\"Title\",\"CurrentAchievements\",\"Gamerscore\",\"Progress\",\"Devices\",\"Genres\"");

                foreach (var title in gamesResponse.Titles)
                {
                    if (ExcludeZeroGamerscoreGames && title.Achievement.TotalGamerscore == 0)
                    {
                        continue;
                    }

                    if (ExcludeXbox360Games && title.Devices != null && title.Devices.Contains("Xbox360"))
                    {
                        continue;
                    }

                    var titleName = title.Name.Replace("\"", "\"\"");
                    var devices = title.Devices != null ? string.Join(", ", title.Devices).Replace("\"", "\"\"") : string.Empty;
                    var genres = title.Detail?.Genres != null ? string.Join(", ", title.Detail.Genres).Replace("\"", "\"\"") : string.Empty;

                    sb.AppendLine($"\"{title.TitleId}\",\"{titleName}\",\"{title.Achievement.CurrentAchievements}\",\"{title.Achievement.CurrentGamerscore}/{title.Achievement.TotalGamerscore}\",\"{title.Achievement.ProgressPercentage}\",\"{devices}\",\"{genres}\"");
                }

                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "CSV files (*.csv)|*.csv",
                    FileName = $"{GamertagXuid}.csv"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await Task.Run(() =>
                    {
                        File.WriteAllText(saveFileDialog.FileName, sb.ToString());
                    });

                    _snackbarService.Show("Success", "Games list exported successfully.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                }
                else
                {
                    _snackbarService.Show("Cancelled", "Game export was not completed", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", "Failed to export games list: " + ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }
        #endregion
    }
}
