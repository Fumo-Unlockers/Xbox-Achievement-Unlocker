using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Net.Http;
using System.Windows.Media;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Contracts;
using Wpf.Ui.Controls;

namespace XAU.ViewModels.Pages
{
    public partial class ImageItem : ObservableObject
    {
        [ObservableProperty]
        private string _imageUrl;
    }

    public partial class HomeViewModel : ObservableObject, INavigationAware
    {
        public static string ToolVersion = "EmptyDevToolVersion";
        public static string EventsVersion = "1.0";

        [ObservableProperty] private string? _gamerPic = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string? _gamerTag = "Gamertag: Unknown   ";
        [ObservableProperty] private string? _xuid = "XUID: Unknown";
        [ObservableProperty] private string? _gamerScore = "Gamerscore: Unknown";
        [ObservableProperty] private string? _profileRep = "Reputation: Unknown";
        [ObservableProperty] private string? _accountTier = "Tier: Unknown";
        [ObservableProperty] private string? _currentlyPlaying = "Currently Playing: Unknown";
        [ObservableProperty] private string? _activeDevice = "Active Device: Unknown";
        [ObservableProperty] private string? _isVerified = "Verified: Unknown";
        [ObservableProperty] private string? _location = "Location: Unknown";
        [ObservableProperty] private string? _tenure = "Tenure: Unknown";
        [ObservableProperty] private string? _following = "Following: Unknown";
        [ObservableProperty] private string? _followers = "Followers: Unknown";
        [ObservableProperty] private string? _gamepass = "Gamepass: Unknown";
        [ObservableProperty] private string? _bio = "Bio: Unknown";
        [ObservableProperty] private string _loginText = "Login";
        [ObservableProperty] public static bool _isLoggedIn = false;
        [ObservableProperty] public static bool _updateAvaliable = false;
        [ObservableProperty] private ObservableCollection<ImageItem> _watermarks = new ObservableCollection<ImageItem>();

        private Lazy<XboxRestAPI> _xboxRestAPI;
        private readonly Lazy<GithubRestApi> _gitHubRestAPI = new Lazy<GithubRestApi>();
        private System.Windows.Threading.DispatcherTimer? _tokenRefreshTimer;
        private global::Windows.Security.Credentials.WebAccount? _currentWamAccount;
        private string? _selectedGdkXuid;  // definido no login via cache do Gaming Services (GDK)

        public static int SpoofingStatus = 0; // 0 = NotSpoofing, 1 = Spoofing, 2 = AutoSpoofing
        public static string SpoofedTitleID = "0";
        public static string AutoSpoofedTitleID = "0";

        public HomeViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;

            // Assume que XAUTH e o idioma do sistema já estão definidos quando isto é instanciado
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        [RelayCommand]
        private void RefreshProfile()
        {
            GrabProfile();
        }

        public static string XAUTH = "";
        public static string XUIDOnly = "";
        public static bool InitComplete = false;
        private bool _isInitialized = false;
        string SettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
        string EventsMetaFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "Events", "meta.json");

        // Contas WAM (carregadas na inicialização, escolhidas via popup)
        private List<global::Windows.Security.Credentials.WebAccount> _wamAccounts = new();

        public async void OnNavigatedTo()
        {
            if (!_isInitialized)
                await InitializeViewModel();
        }
        public void OnNavigatedFrom() { }

        #region Update
        private async void CheckForToolUpdates()
        {
            if (ToolVersion == "EmptyDevToolVersion")
                return;

            // Builds pre-release reportam "PRE-<commit>"; o resto é release padrão.
            // Pre-releases buscam na lista de prereleases; builds padrão usam /releases/latest
            // (que EXCLUI pre-releases, então quem está num release padrão nunca recebe um pre-release).
            var isPreRelease = ToolVersion.StartsWith("PRE-");

            try
            {
                GitHubRelease? release;
                try
                {
                    release = isPreRelease
                        ? await _gitHubRestAPI.Value.GetLatestPreReleaseAsync()
                        : await _gitHubRestAPI.Value.GetLatestReleaseAsync();
                }
                catch (Exception ex)
                {
                    EventsLog($"Update check failed: {ex.Message}");
                    _snackbarService.Show("Update check failed", "Could not reach GitHub to check for updates.",
                        ControlAppearance.Caution, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                if (release?.TagName == null)
                    return;
                if (release.TagName == ToolVersion)
                    return;

                var changelog = new System.Windows.Controls.ScrollViewer
                {
                    MaxHeight = 320,
                    VerticalScrollBarVisibility = System.Windows.Controls.ScrollBarVisibility.Auto,
                    Margin = new Thickness(0, 8, 0, 0),
                    Content = new System.Windows.Controls.TextBlock
                    {
                        Text = string.IsNullOrWhiteSpace(release.Body)
                            ? "No changelog provided for this release."
                            : release.Body,
                        TextWrapping = TextWrapping.Wrap
                    }
                };

                var result = await _contentDialogService.ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = string.IsNullOrWhiteSpace(release.Name)
                            ? $"Version {release.TagName} available to download"
                            : release.Name,
                        Content = changelog,
                        PrimaryButtonText = "Update",
                        CloseButtonText = "Cancel"
                    }
                );
                if (result == ContentDialogResult.Primary)
                {
                    var url = release.Assets?.FirstOrDefault()?.BrowserDownloadUrl;
                    if (string.IsNullOrEmpty(url))
                    {
                        _snackbarService.Show("Update", "No downloadable build was found for this release.",
                            ControlAppearance.Caution, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                        return;
                    }
                    await DownloadAndApplyUpdateAsync(url);
                }
            }
            catch (Exception ex)
            {
                EventsLog($"Update check crashed: {ex.Message}");
            }
        }

        private async Task DownloadAndApplyUpdateAsync(string? sourceUrl)
        {
            if (string.IsNullOrEmpty(sourceUrl))
                return;
            _snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info,
                new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            string destFile = @"XAU-new.exe";
            var fileDownloader = new FileDownloader();
            await fileDownloader.DownloadFileAsync(new Uri(sourceUrl).ToString(), destFile, UpdateTool);
        }
        private async void CheckForEventUpdates()
        {
            if (EventsVersion == "EmptyDevEventsVersion")
                return;
            var response = await _gitHubRestAPI.Value.CheckForEventUpdatesAsync();
            var EventsTimestamp = 0;
            if (File.Exists(EventsMetaFilePath))
            {
                var metaJson = File.ReadAllText(EventsMetaFilePath);
                var meta = JsonConvert.DeserializeObject<EventsUpdateResponse>(metaJson);
                EventsTimestamp = meta.Timestamp;
            }

            if (response.Timestamp > EventsTimestamp && response.DataVersion == EventsVersion)
            {
                _snackbarService.Show("Downloading Events Update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                UpdateEvents();
            }
        }

        private void UpdateTool(object sender, AsyncCompletedEventArgs e)
        {
            var path = Environment.ProcessPath.ToString();
            string[] splitpath = path.Split("\\");
            using (StreamWriter writer = new StreamWriter("XAU-Updater.bat"))
            {
                writer.WriteLine("@echo off");
                writer.WriteLine("timeout 1 > nul");
                writer.WriteLine("del \"" + Environment.ProcessPath + "\" ");
                writer.WriteLine("del \"" + splitpath[splitpath.Count() - 1] + "\" ");
                writer.WriteLine("ren XAU-new.exe \"" + splitpath[splitpath.Count() - 1] + "\" ");
                writer.WriteLine("start \"\" " + "\"" + splitpath[splitpath.Count() - 1] + "\"");
                writer.WriteLine("goto 2 > nul & del \"%~f0\"");
            }
            Process proc = new Process();
            proc.StartInfo.FileName = "XAU-Updater.bat";
            proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
            proc.Start();
            Environment.Exit(0);
        }

        private async void UpdateEvents()
        {
            string XAUPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU");
            string backupFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XAU", "Events", "Backup");
            Directory.CreateDirectory(backupFolderPath);
            string eventsFolderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                "XAU", "Events");
            string[] eventFiles = Directory.GetFiles(eventsFolderPath);
            string[] backupFiles = Directory.GetFiles(backupFolderPath);

            foreach (string file in backupFiles)
            {
                File.Delete(file);
            }
            foreach (string eventFile in eventFiles)
            {
                string fileName = Path.GetFileName(eventFile);
                string destinationPath = Path.Combine(backupFolderPath, fileName);
                File.Move(eventFile, destinationPath, true);
            }

            string zipFilePath = Path.Combine(XAUPath, "Events.zip");
            string extractPath = XAUPath;

            using (var client = new FileDownloader())
            {
                await client.DownloadFileAsync(EventsUrls.Zip, zipFilePath);
            }
            ZipFile.ExtractToDirectory(zipFilePath, extractPath);
            File.Delete(zipFilePath);
            string MetaFilePath = Path.Combine(eventsFolderPath, "meta.json");
            using (var client = new FileDownloader())
            {
                await client.DownloadFileAsync(EventsUrls.MetaUrl, MetaFilePath);
            }
            _snackbarService.Show("Events Update Complete", "Events have been updated to the latest version.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
        }

        private async void CheckForXboxGamesDatabaseUpdate()
        {
            try
            {
                var fileInfo = await _gitHubRestAPI.Value.GetXboxGamesDatabaseInfoAsync();
                if (fileInfo == null)
                {
                    _snackbarService.Show("Error", "Could not check for database updates.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                string titleSearchPath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "TitleSearch");
                string shaFilePath = Path.Combine(titleSearchPath, "xbox_games_sha.txt");
                string dbFilePath = Path.Combine(titleSearchPath, "xbox_games.db");

                Directory.CreateDirectory(titleSearchPath);

                string currentSha = string.Empty;
                try
                {
                    if (File.Exists(shaFilePath))
                    {
                        currentSha = (await File.ReadAllTextAsync(shaFilePath)).Trim();
                    }
                }
                catch { }

                if (string.IsNullOrEmpty(currentSha) || !currentSha.Equals(fileInfo.Sha, StringComparison.OrdinalIgnoreCase))
                {
                    _snackbarService.Show("Database Update", "New Xbox games database available. Downloading...", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);

                    using var client = new HttpClient();
                    var response = await client.GetAsync(fileInfo.DownloadUrl);
                    response.EnsureSuccessStatusCode();

                    var content = await response.Content.ReadAsByteArrayAsync();

                    await File.WriteAllBytesAsync(dbFilePath, content);

                    try
                    {
                        await File.WriteAllTextAsync(shaFilePath, fileInfo.Sha);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Failed to store database SHA: {ex.Message}");
                    }

                    _snackbarService.Show("Success", "Xbox games database updated successfully!", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                }
                else
                {
                    Console.WriteLine("Xbox games database is up to date.");
                }
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", $"Database update check failed: {ex.Message}", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        #endregion

        private async Task InitializeViewModel()
        {
            CheckForToolUpdates();
            await LoadWamAccounts();
            if (!File.Exists(SettingsFilePath))
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "XAU")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"));
                }

                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "XAU\\Events")))
                {
                    Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU\\Events"));
                }
                var defaultSettings = new XAUSettings
                {
                    SettingsVersion = "2",
                    ToolVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                    UnlockAllEnabled = false,
                    AutoSpooferEnabled = false,
                    AutoLaunchXboxAppEnabled = false,
                    RegionOverride = false,
                    UseAcrylic = false,
                    PrivacyMode = false
                };
                string defaultSettingsJson = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
                using (var file = new StreamWriter(SettingsFilePath))
                {
                    file.Write(defaultSettingsJson);
                }
            }
            CheckForEventUpdates();
            CheckForXboxGamesDatabaseUpdate();
            LoadSettings();
            _isInitialized = true;
        }

        #region WamAuth

        private async Task LoadWamAccounts()
        {
            try
            {
                var accounts = await XAU.Services.WamAuthService.GetAccountsAsync();
                _wamAccounts = accounts.Select(a => a.Account).ToList();
            }
            catch (Exception ex)
            {
                EventsLog($"WAM account load failed: {ex.Message}");
            }
        }

        [RelayCommand]
        private async Task LoginWithWam()
        {
            // Caminho preferido: a identidade enrolled em cache do Gaming Services no disco
            // (compartilhada por Microsoft Store, app Xbox e jogos GDK). Só esse token é
            // device-enrolled, então só ele faz spoof de presença e desbloqueia title-based.
            // O picker vem desse cache, logo toda conta listada é spoof-capable. Sem WAM, sem popup.
            if (XAU.Services.GdkTokenService.CacheExists)
            {
                var accounts = XAU.Services.GdkTokenService.GetXboxLiveTokens();
                if (accounts.Count == 0)
                {
                    await _contentDialogService.ShowSimpleDialogAsync(
                        new SimpleContentDialogCreateOptions()
                        {
                            Title = "No Xbox account found",
                            Content = "Sign in to the account you want to use in the Microsoft Store or Xbox app, then try again.",
                            CloseButtonText = "OK"
                        });
                    return;
                }

                XAU.Services.GdkTokenService.XToken sel;
                if (accounts.Count == 1)
                {
                    sel = accounts[0];
                }
                else
                {
                    var idx = await ShowAccountPickerAsync("Select an account",
                        accounts.Select(a => string.IsNullOrEmpty(a.Gamertag) ? a.Xuid : a.Gamertag).ToList());
                    if (idx < 0) return;
                    sel = accounts[idx];
                }

                LoginText = "Logging in...";
                await ApplyGdkLoginAsync(sel);
                StartTokenRefreshTimer();
                GrabProfile();
                return;
            }

            // Fallback: sem cache do Gaming Services → WAM. Leitura/perfil/eventos funcionam,
            // mas spoof de presença e unlocks title-based NÃO (o token não é enrolled).
            if (_wamAccounts.Count == 0)
            {
                await _contentDialogService.ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = "No accounts found",
                        Content = "Sign in to the Microsoft Store / Xbox app, or add a Microsoft account in Windows (Settings > Accounts > Email & accounts).",
                        CloseButtonText = "OK"
                    });
                return;
            }

            global::Windows.Security.Credentials.WebAccount selected;
            if (_wamAccounts.Count == 1)
            {
                selected = _wamAccounts[0];
            }
            else
            {
                var idx = await ShowAccountPickerAsync("Select an account", _wamAccounts.Select(a => a.UserName).ToList());
                if (idx < 0) return;
                selected = _wamAccounts[idx];
            }

            LoginText = "Logging in...";
            _currentWamAccount = selected;
            var success = await XAU.Services.WamAuthService.LoginAsync(selected);

            if (success)
            {
                XAUTH = XAU.Services.WamAuthService.GetXblToken()!;
                XUIDOnly = XAU.Services.WamAuthService.Xuid!;
                AchievementsViewModel.EventsToken = XAU.Services.WamAuthService.GetEventsToken();
                _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
                InitComplete = true;
                IsLoggedIn = true;
                LoginText = "Logged In";
                StartTokenRefreshTimer();
                GrabProfile();
            }
            else
            {
                LoginText = "Login";
                _snackbarService.Show("Login", "Authentication failed. Make sure the account is signed in to Windows.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        // Aplica uma conta do Gaming Services (GDK): xauth enrolled lido do disco + events
        // token mintado do user token em cache da mesma conta. Usado no login e no timer de
        // refresh (tudo relê do disco, sem popup).
        private async Task ApplyGdkLoginAsync(XAU.Services.GdkTokenService.XToken sel)
        {
            // Events token: prefere WAM (device-bound, carrega a claim de xuid — é o que
            // realmente CREDITA unlocks event-based). O mint do Gaming Services é aceito pela
            // telemetria mas não tem a claim, então só ingere, nunca credita. Cai no mint
            // quando a conta não é do Windows (só Store, não adicionada ao Windows).
            string? eventsToken = await TryGetWamEventsTokenAsync(sel.Xuid);
            var eventsSource = eventsToken != null ? "wam" : "none";
            if (eventsToken == null)
            {
                try
                {
                    eventsToken = await XAU.Services.GdkTokenService.MintEventsTokenAsync(sel.Xuid, sel.Uhs);
                    if (eventsToken != null) eventsSource = "gdk-mint (may not credit unlocks)";
                }
                catch (Exception ex) { EventsLog($"events mint failed: {ex.Message}"); }
            }

            // xauth = o token enrolled do Gaming Services (spoof + unlocks title-based).
            // SetExternalToken sobrescreve só xauth/xuid/uhs; o events token WAM em cache acima
            // continua intacto, e a assinatura PoP fica desligada (o token GDK não é PoP-bound).
            XAU.Services.WamAuthService.SetExternalToken(sel.Xbl, sel.Xuid, sel.Uhs);
            XAUTH = sel.Xbl;
            XUIDOnly = sel.Xuid;
            _selectedGdkXuid = sel.Xuid;
            AchievementsViewModel.EventsToken = eventsToken;
            EventsLog($"GDK login: {sel.Gamertag} ({sel.Xuid}) | events={eventsSource}");
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
            InitComplete = true;
            IsLoggedIn = true;
            LoginText = "Logged In";
        }

        // Acha a conta Windows (WAM) cujo login silencioso resolve nesse xuid e retorna o
        // events token device-bound dela, setando _currentWamAccount no match. Só silencioso,
        // nunca abre diálogo. Null quando nenhuma conta Windows bate com o xuid escolhido.
        private async Task<string?> TryGetWamEventsTokenAsync(string xuid)
        {
            _currentWamAccount = null;
            foreach (var acct in _wamAccounts)
            {
                try
                {
                    if (!await XAU.Services.WamAuthService.LoginAsync(acct)) continue;
                    if (XAU.Services.WamAuthService.Xuid == xuid)
                    {
                        _currentWamAccount = acct;
                        return XAU.Services.WamAuthService.GetEventsToken();
                    }
                }
                catch (Exception ex) { EventsLog($"wam events probe failed: {ex.Message}"); }
            }
            return null;
        }

        // Diálogo de seleção de conta. Retorna o índice escolhido, ou -1 se cancelado.
        private async Task<int> ShowAccountPickerAsync(string title, List<string> labels)
        {
            var itemContainerStyle = new Style(typeof(System.Windows.Controls.ListBoxItem));
            itemContainerStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.PaddingProperty, new Thickness(8, 6, 8, 6)));
            itemContainerStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.MarginProperty, new Thickness(0, 2, 0, 2)));
            itemContainerStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BorderThicknessProperty, new Thickness(1)));
            itemContainerStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BorderBrushProperty, System.Windows.Media.Brushes.Transparent));
            itemContainerStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.CursorProperty, System.Windows.Input.Cursors.Hand));
            var hoverTrigger = new Trigger { Property = System.Windows.Controls.ListBoxItem.IsMouseOverProperty, Value = true };
            hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BackgroundProperty,
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x20, 0x60, 0xCD, 0xFF))));
            hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BorderBrushProperty,
                new SolidColorBrush(System.Windows.Media.Color.FromArgb(0x40, 0x60, 0xCD, 0xFF))));
            itemContainerStyle.Triggers.Add(hoverTrigger);

            var listBox = new ListBox
            {
                ItemsSource = labels,
                ItemContainerStyle = itemContainerStyle,
                BorderThickness = new Thickness(0),
                Background = System.Windows.Media.Brushes.Transparent
            };

            var result = await _contentDialogService.ShowSimpleDialogAsync(
                new SimpleContentDialogCreateOptions()
                {
                    Title = title,
                    Content = new System.Windows.Controls.ScrollViewer { MaxHeight = 300, Content = listBox },
                    PrimaryButtonText = "Select",
                    CloseButtonText = "Cancel"
                });
            if (result != ContentDialogResult.Primary || listBox.SelectedIndex < 0)
                return -1;
            return listBox.SelectedIndex;
        }

        #endregion

        private static readonly string EventsLogPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU", "events_debug.log");

        public static void EventsLog(string msg)
        {
            var line = $"[{DateTime.Now:HH:mm:ss}] {msg}";
            try
            {
                Directory.CreateDirectory(Path.GetDirectoryName(EventsLogPath)!);
                File.AppendAllText(EventsLogPath, line + Environment.NewLine);
            }
            catch { }
        }

        #region Profile
        private async void GrabProfile()
        {
            try
            {
                var profileResponse = await _xboxRestAPI.Value.GetProfileAsync(XUIDOnly);

                if (profileResponse?.People?.Any() != true)
                {
                    _snackbarService.Show("Error", "Failed to grab profile information.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }

                var person = profileResponse.People.FirstOrDefault();
                if (Settings.PrivacyMode)
                {
                    GamerTag = "Gamertag: Hidden";
                    Xuid = "XUID: Hidden";
                    GamerPic = "pack://application:,,,/Assets/cirno.png";
                    GamerScore = "Gamerscore: Hidden";
                    ProfileRep = "Reputation: Hidden";
                    AccountTier = "Tier: Hidden";
                    CurrentlyPlaying = "Currently Playing: Hidden";
                    ActiveDevice = "Active Device: Hidden";
                    IsVerified = "Verified: Hidden";
                    Location = "Location: Hidden";
                    Tenure = "Tenure: Hidden";
                    Following = "Following: Hidden";
                    Followers = "Followers: Hidden";
                    Gamepass = "Gamepass: Hidden";
                    Bio = "Bio: Hidden";
                }
                else
                {
                    GamerTag = $"Gamertag: {person?.Gamertag ?? "Unknown"}";
                    Xuid = $"XUID: {person?.Xuid ?? "Unknown"}";
                    GamerPic = (person?.DisplayPicRaw?.Replace("&mode=Padding", "")) ?? "pack://application:,,,/Assets/default.png";
                    GamerScore = $"Gamerscore: {person?.GamerScore ?? "Unknown"}";
                    ProfileRep = $"Reputation: {person?.XboxOneRep ?? "Unknown"}";
                    AccountTier = $"Tier: {person?.Detail?.AccountTier ?? "Unknown"}";

                    var presence = person?.PresenceDetails?.FirstOrDefault();
                    if (presence?.TitleId == null)
                    {
                        CurrentlyPlaying = "Currently Playing: Unknown (No Presence)";
                    }
                    else
                    {
                        var gameTitle = await _xboxRestAPI.Value.GetGameTitleAsync(XUIDOnly, presence.TitleId);
                        CurrentlyPlaying = gameTitle?.Titles?.FirstOrDefault()?.Name ?? $"Currently Playing: Unknown ({presence.TitleId})";
                    }

                    try
                    {
                        var gpuResponse = await _xboxRestAPI.Value.GetGamepassMembershipAsync(XUIDOnly);
                        Gamepass = $"Gamepass: {gpuResponse?.GamepassMembership ?? gpuResponse?.Data?.GamepassMembership ?? "Unknown"}";
                    }
                    catch
                    {
                        Gamepass = "Gamepass: Unknown";
                    }

                    ActiveDevice = $"Active Device: {presence?.Device ?? "Unknown"}";

                    if (person?.Detail != null)
                    {
                        IsVerified = $"Verified: {person.Detail.IsVerified}";
                        Location = $"Location: {person.Detail.Location ?? "Unknown"}";
                        Tenure = $"Tenure: {person.Detail.Tenure ?? "Unknown"}";
                        Following = $"Following: {person.Detail.FollowingCount}";
                        Followers = $"Followers: {person.Detail.FollowerCount}";
                        Bio = $"Bio: {person.Detail.Bio ?? "No Bio"}";

                        Watermarks.Clear();

                        if (int.TryParse(person.Detail.Tenure, out int tenureInt))
                        {
                            string tenureBadge = tenureInt.ToString("D2");
                            Watermarks.Add(new ImageItem { ImageUrl = $@"{BasicXboxAPIUris.WatermarksUrl}tenure/{tenureBadge}.png" });
                        }
                        else
                        {
                            Console.WriteLine("The tenure string is not a valid integer.");
                        }

                        if (person.Detail.Watermarks != null)
                        {
                            foreach (var watermark in person.Detail.Watermarks)
                            {
                                Watermarks.Add(new ImageItem { ImageUrl = $@"{BasicXboxAPIUris.WatermarksUrl}launch/{watermark.ToLower()}.png" });
                            }
                        }
                    }
                }

                _snackbarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsLoggedIn = false;
                StopTokenRefreshTimer();
                _snackbarService.Show("401 Unauthorized", "Something went wrong. Retrying.", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
            catch (Exception ex)
            {
                _snackbarService.Show("Error", "Failed to grab profile information. " + ex.Message, ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
            }
        }

        private void StartTokenRefreshTimer()
        {
            _tokenRefreshTimer?.Stop();
            _tokenRefreshTimer = new System.Windows.Threading.DispatcherTimer
            {
                Interval = TimeSpan.FromHours(2)
            };
            _tokenRefreshTimer.Tick += async (_, _) => await RefreshTokens();
            _tokenRefreshTimer.Start();
        }

        private void StopTokenRefreshTimer()
        {
            _tokenRefreshTimer?.Stop();
            _tokenRefreshTimer = null;
        }

        private async Task RefreshTokens()
        {
            // Caminho GDK: relê o xauth enrolled do disco + remint do events token.
            // O Gaming Services mantém o token em disco rotacionado, então pega o mais novo.
            if (_selectedGdkXuid != null)
            {
                var sel = XAU.Services.GdkTokenService.GetXboxLiveTokens().FirstOrDefault(t => t.Xuid == _selectedGdkXuid);
                if (sel == null)
                {
                    EventsLog("refresh: no enrolled token on disk (open the Microsoft Store / Xbox app to refresh it)");
                    return;
                }
                await ApplyGdkLoginAsync(sel);
                EventsLog("GDK tokens refreshed");
                return;
            }

            if (_currentWamAccount == null) return;

            var success = await XAU.Services.WamAuthService.LoginAsync(_currentWamAccount);
            if (!success)
            {
                EventsLog("Token refresh failed, will retry next interval");
                return;
            }

            XAUTH = XAU.Services.WamAuthService.GetXblToken()!;
            XUIDOnly = XAU.Services.WamAuthService.Xuid!;
            AchievementsViewModel.EventsToken = XAU.Services.WamAuthService.GetEventsToken();
            _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(XAUTH));
            EventsLog("Auth tokens refreshed");
        }

        #endregion

        #region Settings

        public static XAUSettings Settings = new();

        private void LoadSettings()
        {
            var settingsJson = File.ReadAllText(SettingsFilePath);
            var settings = JsonConvert.DeserializeObject<XAUSettings>(settingsJson);
            if (settings == null)
            {
                _snackbarService.Show(
                    "Error",
                    "Couldn't load settings.",
                    ControlAppearance.Danger,
                    new SymbolIcon(SymbolRegular.ErrorCircle24)
                );
                return;
            }

            Settings.SettingsVersion = settings.SettingsVersion;
            Settings.ToolVersion = settings.ToolVersion;
            Settings.UnlockAllEnabled = settings.UnlockAllEnabled;
            Settings.AutoSpooferEnabled = settings.AutoSpooferEnabled;
            Settings.AutoLaunchXboxAppEnabled = settings.AutoLaunchXboxAppEnabled;
            Settings.LaunchHidden = settings.LaunchHidden;
            Settings.RegionOverride = settings.RegionOverride;
            Settings.UseAcrylic = settings.UseAcrylic;
            Settings.PrivacyMode = settings.PrivacyMode;
        }

        #endregion
    }
}
