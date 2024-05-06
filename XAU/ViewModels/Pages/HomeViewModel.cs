using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Memory;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Net;
using System.Collections.ObjectModel;
using System.IO.Compression;
using System.Management;
using Wpf.Ui.Extensions;
using XAU.Models;

namespace XAU.ViewModels.Pages;

public partial class HomeViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
    : ObservableObject, INavigationAware
{
    public const string ToolVersion = "EmptyDevToolVersion";
    private const string EventsVersion = "1.0";

    private const string ProcessName = "XboxPcApp.exe";
        
    [ObservableProperty] 
    private string _attached = "Not Attached";
        
    [ObservableProperty] 
    private Brush _attachedColor = new SolidColorBrush(Colors.Red);
        
    [ObservableProperty] 
    private string _loggedIn = "Not Logged In";
        
    [ObservableProperty] 
    private Brush _loggedInColor = new SolidColorBrush(Colors.Red);

    [ObservableProperty] private string _gamerPic = "pack://application:,,,/Assets/cirno.png";
    [ObservableProperty] private string _gamerTag = "Gamertag: Unknown";
    [ObservableProperty] private string _xuid = "XUID: Unknown";
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
    [ObservableProperty] private string _gamepass = "Gamepass: Unknown";
    [ObservableProperty] private string _bio = "Bio: Unknown";
    [ObservableProperty] public static bool _isLoggedIn = false;
    [ObservableProperty] public static bool _updateAvaliable = false;
    [ObservableProperty] private ObservableCollection<ImageItem> _watermarks = [];

    public enum SpoofingStatusEnum
    {
        NotSpoofing = 0,
        Spoofing,
        AutoSpoofing
    }
        
    public static SpoofingStatusEnum SpoofingStatus = SpoofingStatusEnum.NotSpoofing; //0 = NotSpoofing, 1 = Spoofing, 2 = AutoSpoofing
    public static string SpoofedTitleID = "0";
    public static string AutoSpoofedTitleID = "0";

    private const string WatermarksUrl = "https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/";

    [RelayCommand]
    private void RefreshProfile()
    {
        GrabProfile();
    }

    private readonly Mem _mem = new();
        
    private bool _isAttached;
    private bool _grabbedProfile;
    private bool _xAuthTested;
        
    public static string XAuth="";
    public static string XUIDOnly;
    public static bool InitComplete;
    private bool _isInitialized;
    string SettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
    string EventsMetaFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "Events", "meta.json");
    string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);

    public void OnNavigatedTo()
    {
        if (_isInitialized) return;
        InitializeViewModel();
    }
    public void OnNavigatedFrom() { }

    #region Update
    private async void CheckForToolUpdates()
    {
        if (ToolVersion == "EmptyDevToolVersion")
            return;
        
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _client.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        if (ToolVersion.Contains("DEV"))
        {
            _client.DefaultRequestHeaders.Add("Host", "raw.githubusercontent.com");
            var responseString =
                await _client.GetStringAsync("https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Pre-Release/info.json");
            var Jsonresponse = (dynamic)(new JArray());
            Jsonresponse = (dynamic)JObject.Parse(responseString);

            if (("DEV-"+Jsonresponse.LatestBuildVersion.ToString()) != ToolVersion)
            {
                var result = await contentDialogService.ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = $"Version {Jsonresponse.LatestBuildVersion.ToString()} available to download",
                        Content = "Would you like to update to this version?",
                        PrimaryButtonText = "Update",
                        CloseButtonText = "Cancel"
                    }
                );
                if (result == ContentDialogResult.Primary)
                {
                    snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), TimeSpan.FromSeconds(3));
                    string sourceFile = Jsonresponse.DownloadURL.ToString();
                    string destFile = @"XAU-new.exe";
                    WebClient webClient = new WebClient();
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateTool);
                    webClient.DownloadFileAsync(new Uri(sourceFile), destFile);
                }
            }
        }
        else
        {
            _client.DefaultRequestHeaders.Add("Host", "api.github.com");
            var responseString =
                await _client.GetStringAsync("https://api.github.com/repos/Fumo-Unlockers/Xbox-Achievement-unlocker/releases");
            var Jsonresponse = (dynamic)(new JArray());
            Jsonresponse = (dynamic)JArray.Parse(responseString);
            if (Jsonresponse[0].tag_name.ToString() != ToolVersion)
            {
                var result = await contentDialogService.ShowSimpleDialogAsync(
                    new SimpleContentDialogCreateOptions()
                    {
                        Title = $"Version {Jsonresponse[0].tag_name.ToString()} available to download",
                        Content = "Would you like to update to this version?",
                        PrimaryButtonText = "Update",
                        CloseButtonText = "Cancel"
                    }
                );
                if (result == ContentDialogResult.Primary)
                {
                    snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), TimeSpan.FromSeconds(3));
                    string sourceFile = Jsonresponse[0].assets[0].browser_download_url.ToString();
                    string destFile = @"XAU-new.exe";
                    WebClient webClient = new WebClient();
                    webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(UpdateTool);
                    webClient.DownloadFileAsync(new Uri(sourceFile), destFile);
                }
            }
        }
            
    }
    private async void CheckForEventUpdates()
    {
        if (EventsVersion == "EmptyDevEventsVersion")
            return;
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
        _client.DefaultRequestHeaders.Add("Accept",
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
        _client.DefaultRequestHeaders.Add("Host", "raw.githubusercontent.com");
        var responseString =
            await _client.GetStringAsync("https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Events-Data/meta.json");
        var Jsonresponse = (dynamic)(new JObject());
        Jsonresponse = (dynamic)JObject.Parse(responseString);
        var EventsTimestamp = 0;
        if (File.Exists(EventsMetaFilePath))
        {
            var metaJson = File.ReadAllText(EventsMetaFilePath);
            var meta = JsonConvert.DeserializeObject<dynamic>(metaJson);
            EventsTimestamp = meta.Timestamp;
        }

        if (Jsonresponse.Timestamp > EventsTimestamp && Jsonresponse.DataVersion == EventsVersion)
        {
            snackbarService.Show("Downloading Events Update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), TimeSpan.FromSeconds(3));
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

    private void UpdateEvents()
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

        string zipUrl = "https://github.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/raw/Events-Data/Events.zip";
        string zipFilePath = Path.Combine(XAUPath, "Events.zip");
        string extractPath = XAUPath;

        using (var client = new WebClient())
        {
            client.DownloadFile(zipUrl, zipFilePath);
        }
        ZipFile.ExtractToDirectory(zipFilePath, extractPath);
        File.Delete(zipFilePath);
        //download and place meta.json in the events folder
        string MetaURL = "https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Events-Data/meta.json";
        string MetaFilePath = Path.Combine(eventsFolderPath, "meta.json");
        using (var client = new WebClient())
        {
            client.DownloadFile(MetaURL, MetaFilePath);
        }
        snackbarService.Show("Events Update Complete", "Events have been updated to the latest version.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), TimeSpan.FromSeconds(3));
    }

    #endregion

    private void InitializeViewModel()
    {
        CheckForToolUpdates();
        SetupAttach();
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
            using var file = new StreamWriter(SettingsFilePath);
            file.Write(defaultSettingsJson);
        }
        CheckForEventUpdates();
        LoadSettings();
        _isInitialized = true;
        if (Settings.AutoLaunchXboxAppEnabled)
        {
            var p = new Process();
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                FileName = @"shell:appsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"
            };
            p.StartInfo = startInfo;
            p.Start();
        }

        if (Settings.RegionOverride)
        {
            currentSystemLanguage = "en-GB";
        }
    }

    private void SetupAttach()
    {
        Process.EnterDebugMode();
        if (_mem.OpenProcess(ProcessName) == Mem.OpenProcessResults.Success)
        {
            HandleOpenProcess();
            SetupExit();
        }
        Process.LeaveDebugMode();

        var watcher = new ManagementEventWatcher(new WqlEventQuery("SELECT * FROM Win32_ProcessStartTrace"));
        watcher.EventArrived += (_, e) =>
        {
            if (_isAttached)
            {
                return;
            }
            
            var name = e.NewEvent.Properties["ProcessName"].Value.ToString();
            if (name == null || !string.Equals(name, ProcessName, StringComparison.CurrentCultureIgnoreCase))
            {
                return;
            }

            Process.EnterDebugMode();
            var result = _mem.OpenProcess(ProcessName);
            Process.LeaveDebugMode();
            if (result != Mem.OpenProcessResults.Success) return;
            Application.Current.Dispatcher.BeginInvoke(HandleOpenProcess);
            SetupExit();
        };

        watcher.Start();
    }
    
    private void SetupExit()
    {
        _mem.MProc.Process.EnableRaisingEvents = true;
        _mem.MProc.Process.Exited += (_, _) => HandleCloseProcess();
    }

    private async void HandleOpenProcess()
    {
        _isAttached = true;
        Attached = $"Attached to xbox app (PID: {Mem.GetProcIdFromName("XboxPCApp").ToString()})";
        AttachedColor = Brushes.Green;
            
        if (IsLoggedIn)
        {
            if (!_grabbedProfile)
            {
                GrabProfile();
            }
                
            LoggedIn = "Logged In";
            LoggedInColor = Brushes.Green;
        }
        else
        {
            LoggedInColor = Brushes.Red;
            LoggedIn = "Not Logged In";
                
            while (!_xAuthTested && (!SettingsViewModel.ManualXauth || XAuth.Length == 0))
            {
                if (!SettingsViewModel.ManualXauth)
                {
                    GetXAuth();
                }

                if (XAuth.Length > 0)
                {
                    TestXAuth();
                }

                if (_xAuthTested)
                {
                    break;
                }

                if (!SettingsViewModel.ManualXauth)
                {
                    await Task.Delay(2500);
                }
            }
            
            if (_grabbedProfile)
            {
                return;
            }

            while (!_grabbedProfile)
            {
                GrabProfile();
                if (_grabbedProfile)
                {
                    break;
                }

                await Task.Delay(2500);
            }
                
            LoggedIn = "Logged In";
            LoggedInColor = Brushes.Green;
        }
    }

    private void HandleCloseProcess()
    {
        _isAttached = false;
        AttachedColor = Brushes.Red;
        Attached = "Not Attached";
    }
        
    private async void GetXAuth()
    {
        var xAuthScanList = await _mem.AoBScan("58 42 4C 33 2E 30 20 78 3D", true, false);
        var authScanList = xAuthScanList as UIntPtr[] ?? xAuthScanList.ToArray();
        var xAuthStrings = new string[authScanList.Length];
            
        for (var i = 0; i < authScanList.Length; i++)
        {
            var address = authScanList[i];
            xAuthStrings[i] = _mem.ReadStringMemory(address, length: 10000);
        }
            
        var frequency = new Dictionary<string, int>();
        foreach (var str in xAuthStrings)
        {
            if (!frequency.TryAdd(str, 1))
            {
                frequency[str]++;
            }
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
        _client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            
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
            var responseString = await _client.GetStringAsync("https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag");
            var jsonResponse = (dynamic)JObject.Parse(responseString);
                
            if (Settings.PrivacyMode)
            {
                GamerTag = "Gamertag: Hidden";
                Xuid = "XUID: Hidden";
            }
            else
            {
                GamerTag = $"Gamertag: {jsonResponse.profileUsers[0].settings[0].value}";
                Xuid = $"XUID: {jsonResponse.profileUsers[0].id}";
            }
                
            XUIDOnly = jsonResponse.profileUsers[0].id;
            IsLoggedIn = true;
            _xAuthTested = true;
            InitComplete = true;
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsLoggedIn = false;
                _xAuthTested = false;
            }
        }
    }
 
    private async void GrabProfile()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "5");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Host", "peoplehub.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _client.DefaultRequestHeaders.Add("Authorization", XAuth);
        try
        {
            var responseString = await _client.GetStringAsync(
                $"https://peoplehub.xboxlive.com/users/me/people/xuids({XUIDOnly})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary");
            dynamic jsonResponse = JObject.Parse(responseString);
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
                GamerTag = $"Gamertag: {jsonResponse.people[0].gamertag}";
                Xuid = $"XUID: {jsonResponse.people[0].xuid}";
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
                    _client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                    var requestBody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + jsonResponse.people[0].presenceDetails[0].TitleId + "\"]}");
                    var gameTitleResponse = (dynamic)JObject.Parse(await _client.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + XUIDOnly + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestBody).Result.Content.ReadAsStringAsync());
                    CurrentlyPlaying = $"Currently Playing: {gameTitleResponse.titles[0].name} ({jsonResponse.people[0].presenceDetails[0].TitleId})";
                }
                catch
                {
                    CurrentlyPlaying = $"Currently Playing: Unknown ({jsonResponse.people[0].presenceDetails[0].TitleId})";
                }

                // GPU details
                try
                {
                    _client.DefaultRequestHeaders.Clear();
                    _client.DefaultRequestHeaders.Add("accept", "application/json");
                    _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                    _client.DefaultRequestHeaders.Add("Authorization", XAuth);
                    _client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                    var gpu = (dynamic)JObject.Parse(await _client.GetAsync("https://xgrant.xboxlive.com/users/xuid(" + XUIDOnly + ")/programInfo?filter=profile,activities,catalog").Result.Content.ReadAsStringAsync());
                    Gamepass = string.IsNullOrEmpty(gpu.gamePassMembership)
                        ? $"Gamepass: {gpu.data.gamePassMembership}"
                        : $"Gamepass: {gpu.gamePassMembership}";
                }
                catch
                {
                    Gamepass = "Gamepass: Unknown";
                }
                    
                ActiveDevice = $"Active Device: {jsonResponse.people[0].presenceDetails[0].Device}";
                IsVerified = $"Verified: {jsonResponse.people[0].detail.isVerified}";
                Location = $"Location: {jsonResponse.people[0].detail.location}";
                Tenure = $"Tenure: {jsonResponse.people[0].detail.tenure}";
                Following = $"Following: {jsonResponse.people[0].detail.followingCount}";
                Followers = $"Followers: {jsonResponse.people[0].detail.followerCount}";
                Bio = $"Bio: {jsonResponse.people[0].detail.bio}";
                
                Watermarks.Clear();

                // Tenure image format, 01..05..10
                // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/tenure/15.png
                // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/launch/ba75b64a-9a80-47ea-8c3a-76d3e2ea1422.png
                // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/launch/xboxoneteam.png
                if (jsonResponse.people[0].detail.tenure != "0")
                {
                    var tenureBadge = jsonResponse.people[0].detail.tenure.ToString("D2");
                    Watermarks.Add(new ImageItem { ImageUrl = $"{WatermarksUrl}tenure/{tenureBadge}.png" });
                }
                    
                string[] watermarkNames = jsonResponse.people[0].detail.watermarks.ToObject<string[]>();
                foreach (var watermark in watermarkNames) 
                {
                    Watermarks.Add(new ImageItem { ImageUrl = $"{WatermarksUrl}launch/{watermark.ToLower()}.png" });
                }
            }
            _grabbedProfile = true;
            snackbarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), TimeSpan.FromSeconds(3));
        }
        catch (HttpRequestException ex)
        {
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
            {
                IsLoggedIn = false;
                _xAuthTested = false;
                snackbarService.Show("401 Unauthorized", "Something went wrong. Retrying", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), TimeSpan.FromSeconds(3));
            }
        }
    }
        
    #region Settings

    public class SettingsList
    {
        public string SettingsVersion { get; set; }
        public string ToolVersion { get; set; }
        public bool UnlockAllEnabled { get; set; }
        public bool AutoSpooferEnabled { get; set; }
        public bool AutoLaunchXboxAppEnabled { get; set; }
        public bool FakeSignatureEnabled { get; set; }
        public bool RegionOverride { get; set; }
        public bool UseAcrylic { get; set; }
        public bool PrivacyMode { get; set; }
    }
    public static SettingsList Settings = new SettingsList();

    public void LoadSettings()
    {
        string settingsJson = File.ReadAllText(SettingsFilePath);
        var settings = JsonConvert.DeserializeObject<dynamic>(settingsJson);
        Settings.SettingsVersion = settings.SettingsVersion;
        Settings.ToolVersion = settings.ToolVersion;
        Settings.UnlockAllEnabled = settings.UnlockAllEnabled;
        Settings.AutoSpooferEnabled = settings.AutoSpooferEnabled;
        Settings.AutoLaunchXboxAppEnabled = settings.AutoLaunchXboxAppEnabled;
        Settings.FakeSignatureEnabled = settings.FakeSignatureEnabled;
        Settings.RegionOverride = settings.RegionOverride;
        Settings.UseAcrylic = settings.UseAcrylic;
        Settings.PrivacyMode = settings.PrivacyMode;
    }

    #endregion
}