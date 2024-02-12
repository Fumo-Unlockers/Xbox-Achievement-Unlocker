using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Memory;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Newtonsoft.Json;
using System.Net;
using MessageBox = System.Windows.MessageBox;

namespace XAU.ViewModels.Pages;

public partial class HomeViewModel(ISnackbarService snackBarService, IContentDialogService contentDialogService) : ObservableObject, INavigationAware
{
    public const string ToolVersion = "EmptyDevToolVersion";

    public enum SpooofingStatuses
    {
        NotSpoofing = 0,
        Spoofing = 1,
        AutoSpoofing = 2
    }
    
    //attach vars
    [ObservableProperty] private string _attached = "Not Attached";
    [ObservableProperty] private Brush _attachedColor = new SolidColorBrush(Colors.Red);
    [ObservableProperty] private string _loggedIn = "Not Logged In";
    [ObservableProperty] private Brush _loggedInColor = new SolidColorBrush(Colors.Red);

    //profile vars
    [ObservableProperty] private string _gamerPic = "pack://application:,,,/Assets/cirno.png";
    [ObservableProperty] private string _gamerTag = "Gamertag: Unknown   ";
    [ObservableProperty] private string _xUid = "XUID: Unknown";
    [ObservableProperty] private string _gamerScore = "Gamerscore: Unknown";
    [ObservableProperty] private string _profileRep = "Reputation: Unknown";
    [ObservableProperty] private string _accountTier = "Tier: Unknown";
    [ObservableProperty] private string _currentlyPlaying = "Currently Playing: Unknown";
    [ObservableProperty] private string _activeDevice = "Active Device: Unknown";
    [ObservableProperty] private string _isVerified = "Verified: Unknown";
    [ObservableProperty] private string _location = "Location: Unknown";
    [ObservableProperty] private string _tenure = "Tenure: Unknown";
    [ObservableProperty] private string _following = "Following: Unknown";
    [ObservableProperty] private string _followers = "Followers: Unknown";
    [ObservableProperty] private string _gamePass = "Gamepass: Unknown";
    [ObservableProperty] private string _bio = "Bio: Unknown";
    [ObservableProperty] private static bool _isLoggedIn;
    [ObservableProperty] private static bool _updateAvailable;

    public static SpooofingStatuses SpoofingStatus { get; set; } = SpooofingStatuses.NotSpoofing; 
    public static string SpoofedTitleId { get; set; } = "0";
    public static string AutoSpoofedTitleId { get; set; } = "0";

    private readonly TimeSpan _snackBarDuration = TimeSpan.FromSeconds(2);

    [RelayCommand]
    private void RefreshProfile()
    {
        GrabProfile();
    }

    private readonly Mem _m = new();
    private readonly BackgroundWorker _xAuthWorker = new();
    private bool _isAttached;
    private bool _grabbedProfile;
    private bool _xAuthTested;
    public static string XAuth { get; private set; } = string.Empty;
    public static string? XUidOnly { get; private set; }
    public static bool InitComplete { get; private set; }
    private bool _isInitialized;
    private readonly string _settingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
    private string _currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);

    public void OnNavigatedTo()
    {
        if (!_isInitialized)
            InitializeViewModel();
    }
    public void OnNavigatedFrom() { }

    #region Update
    private async void CheckForUpdates()
    {
        if (ToolVersion == "EmptyDevToolVersion")
            return;
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _client.DefaultRequestHeaders.Add("Accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    
        var checkUrl = ToolVersion.Contains("DEV")
            ? "https://raw.githubusercontent.com/ItsLogic/Xbox-Achievement-Unlocker/Pre-Release/info.json"
            : "https://api.github.com/repos/ItsLogic/Xbox-Achievement-unlocker/releases";
        var host = ToolVersion.Contains("DEV") ? "raw.githubusercontent.com" : "api.github.com";
    
        _client.DefaultRequestHeaders.Add("Host", host);
        var responseString = await _client.GetStringAsync(checkUrl);
    
        var jsonResponse = ToolVersion.Contains("DEV") ? JObject.Parse(responseString) : (dynamic)JArray.Parse(responseString);
        string latestVersion = ToolVersion.Contains("DEV") ? jsonResponse.LatestBuildVersion.ToString() : jsonResponse[0].tag_name.ToString();
    
        if (latestVersion == ToolVersion) return;
    
        var result = await contentDialogService.ShowSimpleDialogAsync(new SimpleContentDialogCreateOptions()
        {
            Title = $"Version {latestVersion} available to download",
            Content = "Would you like to update to this version?",
            PrimaryButtonText = "Update",
            CloseButtonText = "Cancel"
        });
    
        if (result != ContentDialogResult.Primary) return;
    
        snackBarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackBarDuration);
    
        string sourceFile = ToolVersion.Contains("DEV") ? jsonResponse.DownloadURL.ToString() : jsonResponse[0].assets[0].browser_download_url.ToString();
        const string destFile = "XAU-new.exe";
        var webClient = new WebClient();
        webClient.DownloadFileCompleted += Update!;
        webClient.DownloadFileAsync(new Uri(sourceFile), destFile);
    }

    private static void Update(object sender, AsyncCompletedEventArgs e)
    {
        var processPath = Environment.ProcessPath;

        if (processPath == null)
        {
            MessageBox.Show("Failed to update.\n Couldn't get the app process's path");
            return;
        }
        
        var splitPath = processPath.Split("\\");
        using (var writer = new StreamWriter("XAU-Updater.bat"))
        {
            writer.WriteLine("@echo off");
            writer.WriteLine("timeout 1 > nul");
            writer.WriteLine("del \"" + Environment.ProcessPath + "\" ");
            writer.WriteLine("del \"" + splitPath[^1] + "\" ");
            writer.WriteLine("ren XAU-new.exe \"" + splitPath[^1] + "\" ");
            writer.WriteLine("start \"\" " + "\"" + splitPath[^1] + "\"");
            writer.WriteLine("goto 2 > nul & del \"%~f0\"");
        }
        
        var proc = new Process();
        proc.StartInfo.FileName = "XAU-Updater.bat";
        proc.StartInfo.WorkingDirectory = Environment.CurrentDirectory;
        proc.Start();
        Environment.Exit(0);
    }
    #endregion

    private void InitializeViewModel()
    {
        CheckForUpdates();
        _xAuthWorker.DoWork += XAuthWorkerDoWork!;
        _xAuthWorker.ProgressChanged += XAuthWorkerProgressChanged!;
        _xAuthWorker.RunWorkerCompleted += XAuthWorkerRunWorkerCompleted!;
        _xAuthWorker.WorkerReportsProgress = true;
        _xAuthWorker.RunWorkerAsync();
        
        if (!File.Exists(_settingsFilePath))
        {
            if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU")))
            {
                Directory.CreateDirectory(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"));
            }
            var defaultSettings = new
            {
                SettingsVersion = "1",
                ToolVersion = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString(),
                UnlockAllEnabled = false,
                AutoSpooferEnabled = false,
                AutoLaunchXboxAppEnabled = false,
                FakeSignatureEnabled = true,
                RegionOverride = false,
                UseAcrylic = false,
                PrivacyMode = false
            };
            var defaultSettingsJson = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
            using var file = new StreamWriter(_settingsFilePath);
            file.Write(defaultSettingsJson);
        }
        LoadSettings();
        _isInitialized = true;
        if (Settings.AutoLaunchXboxAppEnabled)
        {
            var p = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = @"shell:appsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"
                }
            };
            
            p.Start();
        }

        if (!Settings.RegionOverride) return;
        _currentSystemLanguage = "en-GB";
    }

    #region Xauth

    private void XAuthWorkerDoWork(object sender, DoWorkEventArgs e)
    {
        while (true)
        {
            Task.Delay(_isAttached ? 1000 : 500).Wait();
            if (_m.OpenProcess("XboxPcApp") != Mem.OpenProcessResults.Success)
            {
                _isAttached = false;
                continue;
            }

            _isAttached = true;
            _xAuthWorker.ReportProgress(0);
        }
        // ReSharper disable once FunctionNeverReturns
    }

    private void XAuthWorkerProgressChanged(object sender, ProgressChangedEventArgs e)
    {
        if (_isAttached)
        {
            Attached = $"Attached to xbox app ({Mem.GetProcIdFromName("XboxPCApp")})";
            AttachedColor = new SolidColorBrush(Colors.Green);
            if (IsLoggedIn)
            {
                if (!_grabbedProfile)
                    GrabProfile();
                LoggedIn = "Logged In";
                LoggedInColor = new SolidColorBrush(Colors.Green);
            }
            else
            {
                GetXAuth();
                LoggedIn = "Not Logged In";
                LoggedInColor = new SolidColorBrush(Colors.Red);
                if (_xAuthTested || XAuth.Length <= 0) return;
                TestXAuth();
            }
        }

        if (Mem.GetProcIdFromName("XboxPCApp") != 0) return;
        Attached = "Not Attached";
        AttachedColor = new SolidColorBrush(Colors.Red);
    }

    private void XAuthWorkerRunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
    {
        if (_xAuthWorker.IsBusy) return;
        _xAuthWorker.RunWorkerAsync();
    }
    
    private async void GetXAuth()
    {
        var xAuthScanList = await _m.AoBScan("58 42 4C 33 2E 30 20 78 3D", true);
        var authScanList = xAuthScanList as UIntPtr[] ?? xAuthScanList.ToArray();
        var xAuthStrings = new string[authScanList.Length];
        var i = 0;
        foreach (var address in authScanList)
        {
            xAuthStrings[i] = _m.ReadStringMemory(address, length: 10000);
            i++;
        }

        Dictionary<string, int> frequency = new();
        foreach (var str in xAuthStrings)
        {
            frequency[str] = !frequency.TryGetValue(str, out var value) ? 1 : ++value;
        }

        if (xAuthStrings.Length == 0)
        {
            return;
        }
        var mostCommon = xAuthStrings[0];
        var highestFrequency = 0;
        foreach (var pair in frequency.Where(pair => pair.Value > highestFrequency))
        {
            mostCommon = pair.Key;
            highestFrequency = pair.Value;
        }

        if (highestFrequency > 3)
        {
            XAuth = mostCommon;
        }
    }
    
    private async void TestXAuth()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        
        try
        {
            _client.DefaultRequestHeaders.Add("Authorization", XAuth);
        }
        catch (Exception)
        {
            return;
        }
        
        _client.DefaultRequestHeaders.Add("Host", "profile.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        try
        {
            var responseString =
                await _client.GetStringAsync("https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag");
            dynamic jsonResponse = JObject.Parse(responseString);
            if (Settings.PrivacyMode)
            {
                GamerTag = "Gamertag: Hidden";
                XUid = "XUID: Hidden";
            }
            else
            {
                GamerTag = $"Gamertag: {jsonResponse.profileUsers[0].settings[0].value}";
                XUid = $"XUID: {jsonResponse.profileUsers[0].id}";
            }
                
            XUidOnly = jsonResponse.profileUsers[0].id;
            IsLoggedIn = true;
            _xAuthTested= true;
            InitComplete = true;
        }
        catch (HttpRequestException ex)
        {
            if ((int)ex.StatusCode! != 401) return;
            IsLoggedIn = false;
            _xAuthTested = false;
        }
    }
    #endregion

    #region Profile
    private async void GrabProfile()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "5");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Host", "peoplehub.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _client.DefaultRequestHeaders.Add("Authorization", XAuth);
        try
        {
            var responseString = await _client.GetStringAsync(
                $"https://peoplehub.xboxlive.com/users/me/people/xuids({XUidOnly})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary");
            dynamic jsonResponse = JObject.Parse(responseString);
            if (Settings.PrivacyMode)
            {
                GamerTag = "Gamertag: Hidden";
                XUid = "XUID: Hidden";
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
                GamePass = "Gamepass: Hidden";
                Bio = "Bio: Hidden";

            }
            else
            {
                GamerTag = $"Gamertag: {jsonResponse.people[0].gamertag}";
                XUid = $"XUID: {jsonResponse.people[0].xuid}";
                GamerPic = jsonResponse.people[0].displayPicRaw;
                GamerScore = $"Gamerscore: {jsonResponse.people[0].gamerScore}";
                ProfileRep = $"Reputation: {jsonResponse.people[0].xboxOneRep}";
                AccountTier = $"Tier: {jsonResponse.people[0].detail.accountTier}";
                try
                {
                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
                    _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    _client.DefaultRequestHeaders.Add("accept", "application/json");
                    _client.DefaultRequestHeaders.Add("Authorization", XAuth);
                    _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
                    var requestBody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + jsonResponse.people[0].presenceDetails[0].TitleId + "\"]}");
                    var gameTitleResponse = (dynamic)JObject.Parse(await _client.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + XUidOnly + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestBody).Result.Content.ReadAsStringAsync());
                    CurrentlyPlaying = $"Currently Playing: {gameTitleResponse.titles[0].name} ({jsonResponse.people[0].presenceDetails[0].TitleId})";
                }
                catch
                {
                    CurrentlyPlaying = $"Currently Playing: Unknown ({jsonResponse.people[0].presenceDetails[0].TitleId})";
                }
                    
                ActiveDevice = $"Active Device: {jsonResponse.people[0].presenceDetails[0].Device}";
                IsVerified = $"Verified: {jsonResponse.people[0].detail.isVerified}";
                Location = $"Location: {jsonResponse.people[0].detail.location}";
                Tenure = $"Tenure: {jsonResponse.people[0].detail.tenure}";
                Following = $"Following: {jsonResponse.people[0].detail.followingCount}";
                Followers = $"Followers: {jsonResponse.people[0].detail.followerCount}";
                GamePass = $"Gamepass: {jsonResponse.people[0].detail.hasGamePass}";
                Bio = $"Bio: {jsonResponse.people[0].detail.bio}";
            }
            _grabbedProfile = true;
            snackBarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackBarDuration);
        }
        catch (HttpRequestException ex)
        {
            if ((int)ex.StatusCode! != 401) return;
            IsLoggedIn = false;
            _xAuthTested = false;
            snackBarService.Show("401 Unauthorised", "Something went wrong. Retrying", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
        }
    }
    #endregion
        
    #region Settings

    public struct SettingsList
    {
        public string? SettingsVersion { get; set; }
        public string? AppVersion { get; set; }
        public bool UnlockAllEnabled { get; set; }
        public bool AutoSpooferEnabled { get; set; }
        public bool AutoLaunchXboxAppEnabled { get; set; }
        public bool FakeSignatureEnabled { get; set; }
        public bool RegionOverride { get; set; }
        public bool UseAcrylic { get; set; }
        public bool PrivacyMode { get; set; }
    }

    public static SettingsList Settings;

    private void LoadSettings()
    {
        var settingsJson = File.ReadAllText(_settingsFilePath);
        var settings = JsonConvert.DeserializeObject<dynamic>(settingsJson);
        Settings.SettingsVersion = settings?.SettingsVersion;
        Settings.AppVersion = settings?.ToolVersion;
        Settings.UnlockAllEnabled = settings?.UnlockAllEnabled;
        Settings.AutoSpooferEnabled = settings?.AutoSpooferEnabled;
        Settings.AutoLaunchXboxAppEnabled = settings?.AutoLaunchXboxAppEnabled;
        Settings.FakeSignatureEnabled = settings?.FakeSignatureEnabled;
        Settings.RegionOverride = settings?.RegionOverride;
        Settings.UseAcrylic = settings?.UseAcrylic;
        Settings.PrivacyMode = settings?.PrivacyMode;
    }

    #endregion
}