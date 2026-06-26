using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using Microsoft.Data.Sqlite;
using Newtonsoft.Json;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;
using XAU.Services.AutoUnlock;

namespace XAU.ViewModels.Pages
{
    // Resultado de rodar um título da fila.
    public enum QueueRunResult { Completed, AlreadyComplete, Failed, Invalid }

    // Um jogo já confirmado na fila.
    public partial class QueuedGame : ObservableObject
    {
        public string TitleId { get; set; } = "";
        public string GameName { get; set; } = "";
        public string Gamertag { get; set; } = "";
        public int AchievementCount { get; set; }
        public long EstimateSeconds { get; set; }
        public string EstimateText { get; set; } = "";
        [ObservableProperty] private string _status = "Queued";
        [ObservableProperty] private string _statusColor = "#9AA0A6";
    }

    // Linha da pré-visualização da ordem (antes de confirmar).
    public partial class OrderPreviewItem : ObservableObject
    {
        public int Position { get; set; }
        public string Name { get; set; } = "";
        public string DelayText { get; set; } = "";
        public bool Skipped { get; set; }
        public double NameOpacity => Skipped ? 0.4 : 1.0;
        public string PositionText => Skipped ? "—" : Position.ToString();
    }

    public partial class QueueViewModel : ObservableObject, INavigationAware
    {
        private readonly ISnackbarService _snackbar;
        private readonly AchievementsViewModel _achievements;
        private readonly INavigationService _navigation;
        private readonly Lazy<XboxRestAPI> _api = new(() => new XboxRestAPI(HomeViewModel.XAUTH));
        private readonly TimeSpan _snackDur = TimeSpan.FromSeconds(2);
        private CancellationTokenSource? _runCts;

        [ObservableProperty] private ObservableCollection<QueuedGame> _games = new();

        // --- Busca ---
        [ObservableProperty] private string _searchText = "";
        [ObservableProperty] private ObservableCollection<GameItem> _searchResults = new();
        [ObservableProperty] private GameItem? _selectedResult;

        // --- Rascunho (jogo escolhido, ainda não adicionado) ---
        [ObservableProperty][NotifyPropertyChangedFor(nameof(HasSelection))] private string _draftTitleId = "";
        [ObservableProperty] private string _draftName = "";
        [ObservableProperty] private string _draftGamertag = "";
        [ObservableProperty][NotifyPropertyChangedFor(nameof(HasOrder))] private bool _orderBuilt;
        [ObservableProperty] private string _draftSummary = "";
        [ObservableProperty] private ObservableCollection<OrderPreviewItem> _preview = new();

        [ObservableProperty][NotifyPropertyChangedFor(nameof(CanEdit))] private bool _isBusy;
        [ObservableProperty] private string _cooldownMinutes = "10";

        // --- Execução ---
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(IsIdle))]
        [NotifyPropertyChangedFor(nameof(CanEdit))]
        private bool _isRunning;
        [ObservableProperty] private string _runStatus = "";
        [ObservableProperty] private string _cooldownText = "";
        [ObservableProperty] private string _queueEtaText = "";

        private string _lastGamertag = "";

        partial void OnCooldownMinutesChanged(string value) => RecomputeEta();

        public bool IsIdle => !IsRunning;
        public bool CanEdit => !IsRunning && !IsBusy;
        public bool HasSelection => !string.IsNullOrEmpty(DraftTitleId);
        public bool HasOrder => OrderBuilt;

        public QueueViewModel(ISnackbarService snackbar, AchievementsViewModel achievements, INavigationService navigation)
        {
            _snackbar = snackbar;
            _achievements = achievements;
            _navigation = navigation;
            Games.CollectionChanged += (_, _) => RecomputeEta();
        }

        // Adiciona direto na fila (usado pelo botão "Add to Queue" da tela de Achievements,
        // onde a ordem já foi gerada). Sem gamertag: roda pela ordem já salva no disco.
        public void AddGameDirect(string titleId, string gameName)
        {
            if (string.IsNullOrWhiteSpace(titleId)) return;
            if (Games.Any(x => x.TitleId == titleId))
            {
                _snackbar.Show("Queue", "That game is already in the queue.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            var order = OrderRepository.Load(titleId);
            var skip = OrderRepository.LoadSkip(titleId);
            int count = order?.Items.Count(i => !skip.Contains(i.Id)) ?? 0;
            long secs = order?.Items.Where(i => !skip.Contains(i.Id)).Sum(i => i.DelaySeconds) ?? 0;

            Games.Add(new QueuedGame
            {
                TitleId = titleId,
                GameName = string.IsNullOrWhiteSpace(gameName) ? titleId : gameName,
                Gamertag = "",
                AchievementCount = count,
                EstimateSeconds = secs,
                EstimateText = $"{count} achievements • ~{FormatSpan(secs)}"
            });
            SaveToDisk();
            _snackbar.Show("Added to queue", $"{gameName} is now in the queue.", ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Checkmark24), _snackDur);
        }

        // ETA da fila inteira: soma os jogos ainda pendentes + os cooldowns entre eles.
        private void RecomputeEta()
        {
            var pending = Games.Where(g => !IsFinished(g.Status)).ToList();
            if (pending.Count == 0) { QueueEtaText = ""; return; }
            int cooldown = int.TryParse(CooldownMinutes, out var m) ? Math.Max(0, m) : 10;
            long total = pending.Sum(g => g.EstimateSeconds) + (long)Math.Max(0, pending.Count - 1) * cooldown * 60;
            QueueEtaText = $"Whole queue ~{FormatSpan(total)} ({pending.Count} game{(pending.Count == 1 ? "" : "s")}, incl. cooldowns)";
        }

        public void OnNavigatedTo()
        {
            if (Games.Count == 0)
                LoadFromDisk();
            _lastGamertag = LoadLastGamertag();
            RecomputeEta();
        }

        public void OnNavigatedFrom() { }

        #region Busca de jogos

        [RelayCommand]
        private async Task SearchGames()
        {
            var q = SearchText.Trim();
            if (string.IsNullOrWhiteSpace(q))
            {
                SearchResults = new ObservableCollection<GameItem>();
                return;
            }

            var db = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XAU", "TitleSearch", "xbox_games.db");
            if (!File.Exists(db))
            {
                _snackbar.Show("Queue", "Game database isn't ready yet — give it a moment to download.",
                    ControlAppearance.Caution, new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            IsBusy = true;
            try
            {
                var list = await Task.Run(() => QueryGames(db, q));
                SearchResults = new ObservableCollection<GameItem>(list);
                if (list.Count == 0)
                    _snackbar.Show("Queue", $"No games matched \"{q}\".", ControlAppearance.Caution,
                        new SymbolIcon(SymbolRegular.Search24), _snackDur);
            }
            catch (Exception ex)
            {
                _snackbar.Show("Search failed", ex.Message, ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackDur);
            }
            finally { IsBusy = false; }
        }

        private static List<GameItem> QueryGames(string dbPath, string query)
        {
            var results = new List<GameItem>();
            using var conn = new SqliteConnection($"Data Source={dbPath}");
            conn.Open();
            using var cmd = new SqliteCommand(
                "SELECT title, titleId, isTitleBased FROM games WHERE title LIKE @q ORDER BY title COLLATE NOCASE LIMIT 50", conn);
            cmd.Parameters.AddWithValue("@q", $"%{query}%");
            using var reader = cmd.ExecuteReader();
            int iTitle = reader.GetOrdinal("title");
            int iId = reader.GetOrdinal("titleId");
            int iTb = reader.GetOrdinal("isTitleBased");
            while (reader.Read())
                results.Add(new GameItem
                {
                    Title = reader.GetString(iTitle),
                    TitleId = reader.GetString(iId),
                    IsTitleBased = reader.GetInt32(iTb) == 1
                });
            return results;
        }

        // Escolher um resultado vira o rascunho atual.
        partial void OnSelectedResultChanged(GameItem? value)
        {
            if (value == null) return;
            DraftTitleId = value.TitleId;
            DraftName = value.Title;
            OrderBuilt = false;
            Preview = new ObservableCollection<OrderPreviewItem>();
            DraftSummary = "";
            // Pré-preenche com o último gamertag usado (a galera costuma espelhar o mesmo).
            if (string.IsNullOrWhiteSpace(DraftGamertag) && !string.IsNullOrWhiteSpace(_lastGamertag))
                DraftGamertag = _lastGamertag;
        }

        #endregion

        #region Rascunho da ordem

        [RelayCommand]
        private async Task BuildOrder()
        {
            if (!HasSelection)
            {
                _snackbar.Show("Queue", "Pick a game from the search results first.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }
            var gt = DraftGamertag.Trim();
            if (string.IsNullOrWhiteSpace(gt))
            {
                _snackbar.Show("Queue", "Enter a reference gamertag to mirror.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }
            if (!HomeViewModel.InitComplete)
            {
                _snackbar.Show("Queue", "Sign in before building an order.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            IsBusy = true;
            try
            {
                var order = await OrderRepository.GenerateFromReferenceAsync(
                    _api.Value, DraftTitleId, DraftName, gt, new HashSet<string>());
                OrderRepository.Save(order);
                _lastGamertag = gt;
                SaveLastGamertag(gt);
                OrderBuilt = true;
                RefreshPreview();
                _snackbar.Show("Order ready", $"{order.Items.Count} achievements mirrored from {gt}. Tweak or add it.",
                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackDur);
            }
            catch (Exception ex)
            {
                OrderBuilt = false;
                _snackbar.Show("Couldn't build order", ex.Message, ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24), _snackDur);
            }
            finally { IsBusy = false; }
        }

        [RelayCommand]
        private async Task EditDraftDelays()
        {
            if (!HasOrder) return;
            await _achievements.EditDelaysForTitleAsync(DraftTitleId);
            RefreshPreview();
        }

        [RelayCommand]
        private async Task EditDraftSkip()
        {
            if (!HasOrder) return;
            await _achievements.EditSkipListForTitleAsync(DraftTitleId);
            RefreshPreview();
        }

        [RelayCommand]
        private void ConfirmAdd()
        {
            if (!HasOrder || !HasSelection) return;
            if (Games.Any(x => x.TitleId == DraftTitleId))
            {
                _snackbar.Show("Queue", "That game is already in the queue.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            var order = OrderRepository.Load(DraftTitleId);
            var skip = OrderRepository.LoadSkip(DraftTitleId);
            int count = order?.Items.Count(i => !skip.Contains(i.Id)) ?? 0;
            long secs = order?.Items.Where(i => !skip.Contains(i.Id)).Sum(i => i.DelaySeconds) ?? 0;

            Games.Add(new QueuedGame
            {
                TitleId = DraftTitleId,
                GameName = DraftName,
                Gamertag = DraftGamertag.Trim(),
                AchievementCount = count,
                EstimateSeconds = secs,
                EstimateText = $"{count} achievements • ~{FormatSpan(secs)}"
            });
            SaveToDisk();
            RecomputeEta();
            ResetDraft();
            _snackbar.Show("Added to queue", "Line up another or press Start.", ControlAppearance.Success,
                new SymbolIcon(SymbolRegular.Checkmark24), _snackDur);
        }

        [RelayCommand]
        private void DiscardDraft() => ResetDraft();

        private void ResetDraft()
        {
            SelectedResult = null;
            DraftTitleId = "";
            DraftName = "";
            DraftGamertag = "";
            OrderBuilt = false;
            Preview = new ObservableCollection<OrderPreviewItem>();
            DraftSummary = "";
        }

        // Recarrega ordem + skip do disco e remonta a pré-visualização.
        private void RefreshPreview()
        {
            var items = new ObservableCollection<OrderPreviewItem>();
            var order = OrderRepository.Load(DraftTitleId);
            if (order == null)
            {
                Preview = items;
                DraftSummary = "";
                return;
            }

            var skip = OrderRepository.LoadSkip(DraftTitleId);
            long secs = 0;
            int pos = 1, included = 0;
            foreach (var it in order.Items)
            {
                bool sk = skip.Contains(it.Id);
                if (!sk) { secs += it.DelaySeconds; included++; }
                items.Add(new OrderPreviewItem
                {
                    Position = pos++,
                    Name = it.Name,
                    DelayText = it.DelaySeconds <= 0 ? "start" : FormatSpan(it.DelaySeconds),
                    Skipped = sk
                });
            }

            Preview = items;
            DraftSummary = $"{included} of {order.Items.Count} achievements • ~{FormatSpan(secs)}";
        }

        #endregion

        #region Fila (reordenar / remover / rodar)

        [RelayCommand]
        private void Remove(QueuedGame? game)
        {
            if (game == null || IsRunning) return;
            Games.Remove(game);
            SaveToDisk();
        }

        [RelayCommand]
        private void MoveUp(QueuedGame? game)
        {
            if (game == null || IsRunning) return;
            int i = Games.IndexOf(game);
            if (i > 0) { Games.Move(i, i - 1); SaveToDisk(); }
        }

        [RelayCommand]
        private void MoveDown(QueuedGame? game)
        {
            if (game == null || IsRunning) return;
            int i = Games.IndexOf(game);
            if (i >= 0 && i < Games.Count - 1) { Games.Move(i, i + 1); SaveToDisk(); }
        }

        [RelayCommand]
        private void ClearQueue()
        {
            if (IsRunning) return;
            Games.Clear();
            SaveToDisk();
        }

        [RelayCommand]
        private async Task StartQueue()
        {
            if (IsRunning) return;
            if (Games.Count == 0)
            {
                _snackbar.Show("Queue", "Add a game first.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }
            if (!HomeViewModel.InitComplete)
            {
                _snackbar.Show("Queue", "Sign in before starting.", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackDur);
                return;
            }

            int cooldown = int.TryParse(CooldownMinutes, out var m) ? Math.Max(0, m) : 10;
            _runCts = new CancellationTokenSource();
            IsRunning = true;

            // Leva pra tela de Achievements pra acompanhar o painel do auto unlocker ao vivo.
            try { _navigation.Navigate(typeof(XAU.Views.Pages.AchievementsPage)); } catch { }

            try
            {
                var pending = Games.ToList();
                for (int i = 0; i < pending.Count; i++)
                {
                    if (_runCts.IsCancellationRequested) break;
                    var g = pending[i];
                    if (IsFinished(g.Status)) continue;

                    SetStatus(g, "Unlocking", "#60CDFF");
                    RunStatus = $"Game {i + 1} of {pending.Count} — {g.GameName}";

                    QueueRunResult result;
                    try { result = await _achievements.RunQueuedTitleAsync(g.TitleId, g.Gamertag, _runCts.Token); }
                    catch { result = QueueRunResult.Failed; }

                    if (_runCts.IsCancellationRequested) { SetStatus(g, "Stopped", "#9AA0A6"); break; }

                    bool skipped = result == QueueRunResult.AlreadyComplete;
                    if (result == QueueRunResult.Completed || result == QueueRunResult.AlreadyComplete)
                        SetStatus(g, skipped ? "Already 100%" : "Done", "#6BD968");
                    else
                        SetStatus(g, "Failed", "#FF6B6B");
                    SaveToDisk();

                    // Jogo já 100% não roda nada -> não precisa esperar o cooldown.
                    bool hasNext = pending.Skip(i + 1).Any(x => !IsFinished(x.Status));
                    if (hasNext && cooldown > 0 && !skipped && !_runCts.IsCancellationRequested)
                        await CooldownAsync(cooldown, _runCts.Token);
                }

                RunStatus = _runCts.IsCancellationRequested ? "Queue stopped." : "Queue finished.";
            }
            finally
            {
                IsRunning = false;
                CooldownText = "";
                _runCts?.Dispose();
                _runCts = null;
            }
        }

        [RelayCommand]
        private void StopQueue() => _runCts?.Cancel();

        private async Task CooldownAsync(int minutes, CancellationToken ct)
        {
            var until = DateTime.Now.AddMinutes(minutes);
            while (!ct.IsCancellationRequested)
            {
                var left = until - DateTime.Now;
                if (left <= TimeSpan.Zero) break;
                CooldownText = $"Cooldown — next game in {left:mm\\:ss}";
                try { await Task.Delay(1000, ct); }
                catch { break; }
            }
            CooldownText = "";
        }

        private void SetStatus(QueuedGame g, string status, string color)
        {
            g.Status = status;
            g.StatusColor = color;
            RecomputeEta();
        }

        // Jogos concluídos (ou já 100%) saem da conta e são pulados na próxima execução.
        private static bool IsFinished(string status) => status == "Done" || status == "Already 100%";

        #endregion

        private static string FormatSpan(long seconds)
        {
            var t = TimeSpan.FromSeconds(seconds);
            if (t.TotalHours >= 1) return $"{(int)t.TotalHours}h {t.Minutes}m";
            if (t.TotalMinutes >= 1) return $"{t.Minutes}m {t.Seconds}s";
            return $"{t.Seconds}s";
        }

        #region Persistência

        private static string QueueDir => Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "Queue");
        private static string QueueFile => Path.Combine(QueueDir, "queue.json");
        private static string LastGamertagFile => Path.Combine(QueueDir, "last_gamertag.txt");

        private record QueueDto(string TitleId, string GameName, string Gamertag, int AchievementCount,
            long EstimateSeconds, string EstimateText, string? Status, string? StatusColor);

        private void SaveToDisk()
        {
            try
            {
                Directory.CreateDirectory(QueueDir);
                var dto = Games.Select(g => new QueueDto(g.TitleId, g.GameName, g.Gamertag, g.AchievementCount,
                    g.EstimateSeconds, g.EstimateText, g.Status, g.StatusColor)).ToList();
                File.WriteAllText(QueueFile, JsonConvert.SerializeObject(dto, Formatting.Indented));
            }
            catch { }
        }

        private void LoadFromDisk()
        {
            try
            {
                if (!File.Exists(QueueFile)) return;
                var dto = JsonConvert.DeserializeObject<List<QueueDto>>(File.ReadAllText(QueueFile));
                if (dto == null) return;
                Games.Clear();
                foreach (var d in dto)
                {
                    // Só status terminais sobrevivem ao restart; o resto volta pra "Queued".
                    bool finished = IsFinished(d.Status ?? "") || d.Status == "Failed";
                    Games.Add(new QueuedGame
                    {
                        TitleId = d.TitleId,
                        GameName = d.GameName,
                        Gamertag = d.Gamertag,
                        AchievementCount = d.AchievementCount,
                        EstimateSeconds = d.EstimateSeconds,
                        EstimateText = d.EstimateText,
                        Status = finished ? d.Status! : "Queued",
                        StatusColor = finished ? (d.StatusColor ?? "#6BD968") : "#9AA0A6"
                    });
                }
            }
            catch { }
        }

        private static string LoadLastGamertag()
        {
            try { return File.Exists(LastGamertagFile) ? File.ReadAllText(LastGamertagFile).Trim() : ""; }
            catch { return ""; }
        }

        private static void SaveLastGamertag(string gamertag)
        {
            try
            {
                Directory.CreateDirectory(QueueDir);
                File.WriteAllText(LastGamertagFile, gamertag.Trim());
            }
            catch { }
        }

        #endregion
    }
}
