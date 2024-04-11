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
using System.Collections.ObjectModel;

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
        //attach vars
        [ObservableProperty] private string _attached = "Not Attached";
        [ObservableProperty] private Brush _attachedColor = new SolidColorBrush(Colors.Red);
        [ObservableProperty] private string _loggedIn = "Not Logged In";
        [ObservableProperty] private Brush _loggedInColor = new SolidColorBrush(Colors.Red);

        //profile vars
        [ObservableProperty] private string _gamerPic = "pack://application:,,,/Assets/cirno.png";
        [ObservableProperty] private string _gamerTag = "Gamertag: Unknown   ";
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
        [ObservableProperty] private ObservableCollection<ImageItem> _watermarks = new ObservableCollection<ImageItem>();

        public static int SpoofingStatus = 0; //0 = NotSpoofing, 1 = Spoofing, 2 = AutoSpoofing
        public static string SpoofedTitleID = "0";
        public static string AutoSpoofedTitleID = "0";

        private const string WatermarksUrl = "https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/";

        //SnackBar
        public HomeViewModel(ISnackbarService snackbarService, IContentDialogService contentDialogService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = contentDialogService;
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);
        private readonly IContentDialogService _contentDialogService;

        [RelayCommand]
        private void RefreshProfile()
        {
            GrabProfile();
        }

        Mem m = new Mem();
        public BackgroundWorker XauthWorker = new BackgroundWorker();
        bool IsAttached = false;
        bool GrabbedProfile = false;
        bool XAUTHTested = false;
        public static string XAUTH="";
        public static string XUIDOnly;
        public static bool InitComplete = false;
        private bool _isInitialized = false;
        string SettingsFilePath = Path.Combine(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "XAU"), "settings.json");
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);

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
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate, br");
            client.DefaultRequestHeaders.Add("Accept",
                "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
            if (ToolVersion.Contains("DEV"))
            {
                client.DefaultRequestHeaders.Add("Host", "raw.githubusercontent.com");
                var responseString =
                    await client.GetStringAsync("https://raw.githubusercontent.com/ItsLogic/Xbox-Achievement-Unlocker/Pre-Release/info.json");
                var Jsonresponse = (dynamic)(new JArray());
                Jsonresponse = (dynamic)JObject.Parse(responseString);

                if (("DEV-"+Jsonresponse.LatestBuildVersion.ToString()) != ToolVersion)
                {
                    var result = await _contentDialogService.ShowSimpleDialogAsync(
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
                        _snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                        string sourceFile = Jsonresponse.DownloadURL.ToString();
                        string destFile = @"XAU-new.exe";
                        WebClient webClient = new WebClient();
                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Update);
                        webClient.DownloadFileAsync(new Uri(sourceFile), destFile);
                    }
                }
            }
            else
            {
                client.DefaultRequestHeaders.Add("Host", "api.github.com");
                var responseString =
                    await client.GetStringAsync("https://api.github.com/repos/ItsLogic/Xbox-Achievement-unlocker/releases");
                var Jsonresponse = (dynamic)(new JArray());
                Jsonresponse = (dynamic)JArray.Parse(responseString);
                if (Jsonresponse[0].tag_name.ToString() != ToolVersion)
                {
                    var result = await _contentDialogService.ShowSimpleDialogAsync(
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
                        _snackbarService.Show("Downloading update...", "Please wait", ControlAppearance.Info, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                        string sourceFile = Jsonresponse[0].assets[0].browser_download_url.ToString();
                        string destFile = @"XAU-new.exe";
                        WebClient webClient = new WebClient();
                        webClient.DownloadFileCompleted += new AsyncCompletedEventHandler(Update);
                        webClient.DownloadFileAsync(new Uri(sourceFile), destFile);
                    }
                }
            }
            
        }

        private void Update(object sender, AsyncCompletedEventArgs e)
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
#endregion

        private void InitializeViewModel()
        {
            CheckForUpdates();
            XauthWorker.DoWork += XauthWorker_DoWork;
            XauthWorker.ProgressChanged += XauthWorker_ProgressChanged;
            XauthWorker.RunWorkerCompleted += XauthWorker_RunWorkerCompleted;
            XauthWorker.WorkerReportsProgress = true;
            XauthWorker.RunWorkerAsync();
            if (!File.Exists(SettingsFilePath))
            {
                if (!Directory.Exists(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                        "XAU")))
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
                string defaultSettingsJson = JsonConvert.SerializeObject(defaultSettings, Formatting.Indented);
                using (var file = new StreamWriter(SettingsFilePath))
                {
                    file.Write(defaultSettingsJson);
                }
                
            }
            LoadSettings();
            _isInitialized = true;
            if (Settings.AutoLaunchXboxAppEnabled)
            {
                Process p = new Process();
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    UseShellExecute = true,
                    FileName = @"shell:appsFolder\Microsoft.GamingApp_8wekyb3d8bbwe!Microsoft.Xbox.App"
                };
                p.StartInfo = startInfo;
                p.Start();
            }
            if (Settings.RegionOverride)
                currentSystemLanguage = "en-GB";
        }

#region Xauth
        public async void XauthWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (m.OpenProcess("XboxPcApp") != Mem.OpenProcessResults.Success)
                {
                    IsAttached = false;
                    Thread.Sleep(1000);
                }
                else
                {
                    IsAttached = true;
                }
                Thread.Sleep(1000);
                XauthWorker.ReportProgress(0);


            }
        }
        public void XauthWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (IsAttached)
            {
                Attached = $"Attached to xbox app ({Mem.GetProcIdFromName("XboxPCApp").ToString()})";
                AttachedColor = new SolidColorBrush(Colors.Green);
                if (IsLoggedIn)
                {
                    if (!GrabbedProfile)
                        GrabProfile();
                    LoggedIn = "Logged In";
                    LoggedInColor = new SolidColorBrush(Colors.Green);
                }
                else
                {
                    GetXAUTH();
                    LoggedIn = "Not Logged In";
                    LoggedInColor = new SolidColorBrush(Colors.Red);
                    if (!XAUTHTested && XAUTH.Length>0)
                    {
                        TestXAUTH();
                    }
                }
            }
            if (Mem.GetProcIdFromName("XboxPCApp") == 0)
            {
                Attached = "Not Attached";
                AttachedColor = new SolidColorBrush(Colors.Red);
            }
        }
        public void XauthWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            if (!XauthWorker.IsBusy)
                XauthWorker.RunWorkerAsync();
        }
        private async void GetXAUTH()
        {
            var XauthScanList = await m.AoBScan("58 42 4C 33 2E 30 20 78 3D", true);
            string[] XauthStrings = new string[XauthScanList.Count()];
            var i = 0;
            foreach (var address in XauthScanList)
            {
                XauthStrings[i] = m.ReadStringMemory(address, length: 10000);
                i++;
            }

            var frequency = new Dictionary<string, int>();
            foreach (var str in XauthStrings)
            {
                if (!frequency.TryAdd(str, 1))
                {
                    frequency[str]++;
                }
            }

            if (XauthStrings.Length == 0)
            {
                return;
            }
            
            var mostCommon = XauthStrings[0];
            var highestFrequency = 0;
            foreach (var pair in frequency.Where(pair => pair.Value > highestFrequency))
            {
                mostCommon = pair.Key;
                highestFrequency = pair.Value;
            }

            if (highestFrequency > 3)
            {
                XAUTH = mostCommon;
            }
        }
        private async void TestXAUTH()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            try
            {
                client.DefaultRequestHeaders.Add("Authorization", XAUTH);
            }
            catch (Exception exception)
            {
                return;
            }
            client.DefaultRequestHeaders.Add("Host", "profile.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            try
            {
                var responseString =
                    await client.GetStringAsync("https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                if (Settings.PrivacyMode)
                {
                    GamerTag = $"Gamertag: Hidden";
                    Xuid = $"XUID: Hidden";
                }
                else
                {
                    GamerTag = $"Gamertag: {Jsonresponse.profileUsers[0].settings[0].value}";
                    Xuid = $"XUID: {Jsonresponse.profileUsers[0].id}";
                }
                
                XUIDOnly = Jsonresponse.profileUsers[0].id;
                IsLoggedIn = true;
                XAUTHTested= true;
                InitComplete = true;
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                {
                    IsLoggedIn = false;
                    XAUTHTested = false;

                }
            }
        }
#endregion

#region Profile
        private async void GrabProfile()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "5");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            client.DefaultRequestHeaders.Add("Host", "peoplehub.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("Authorization", XAUTH);
            try
            {
                var responseString = await client.GetStringAsync(
                    $"https://peoplehub.xboxlive.com/users/me/people/xuids({XUIDOnly})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary");
                var Jsonresponse = (dynamic)(new JObject());
                Jsonresponse = (dynamic)JObject.Parse(responseString);
                if (Settings.PrivacyMode)
                {
                    GamerTag = $"Gamertag: Hidden";
                    Xuid = $"XUID: Hidden";
                    GamerPic = "pack://application:,,,/Assets/cirno.png";
                    GamerScore = $"Gamerscore: Hidden";
                    ProfileRep = $"Reputation: Hidden";
                    AccountTier = $"Tier: Hidden";
                    CurrentlyPlaying = $"Currently Playing: Hidden";
                    ActiveDevice = $"Active Device: Hidden";
                    IsVerified = $"Verified: Hidden";
                    Location = $"Location: Hidden";
                    Tenure = $"Tenure: Hidden";
                    Following = $"Following: Hidden";
                    Followers = $"Followers: Hidden";
                    Gamepass = $"Gamepass: Hidden";
                    Bio = $"Bio: Hidden";
                }
                else
                {
                    GamerTag = $"Gamertag: {Jsonresponse.people[0].gamertag}";
                    Xuid = $"XUID: {Jsonresponse.people[0].xuid}";
                    GamerPic = Jsonresponse.people[0].displayPicRaw;
                    GamerScore = $"Gamerscore: {Jsonresponse.people[0].gamerScore}";
                    ProfileRep = $"Reputation: {Jsonresponse.people[0].xboxOneRep}";
                    AccountTier = $"Tier: {Jsonresponse.people[0].detail.accountTier}";
                    try
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
                        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                        client.DefaultRequestHeaders.Add("accept", "application/json");
                        client.DefaultRequestHeaders.Add("Authorization", XAUTH);
                        client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                        StringContent requestbody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + Jsonresponse.people[0].presenceDetails[0].TitleId + "\"]}");
                        var GameTitleResponse = (dynamic)JObject.Parse(await client.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + XUIDOnly + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestbody).Result.Content.ReadAsStringAsync());
                        CurrentlyPlaying = $"Currently Playing: {GameTitleResponse.titles[0].name} ({Jsonresponse.people[0].presenceDetails[0].TitleId})";
                    }
                    catch
                    {
                        CurrentlyPlaying = $"Currently Playing: Unknown ({Jsonresponse.people[0].presenceDetails[0].TitleId})";
                    }

                    // GPU details
                    try
                    {
                        client.DefaultRequestHeaders.Clear();
                        client.DefaultRequestHeaders.Add("accept", "application/json");
                        client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                        client.DefaultRequestHeaders.Add("Authorization", XAUTH);
                        client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                        var gpu = (dynamic)JObject.Parse(await client.GetAsync("https://xgrant.xboxlive.com/users/xuid(" + XUIDOnly + ")/programInfo?filter=profile,activities,catalog").Result.Content.ReadAsStringAsync());
                        Gamepass = $"Gamepass: {gpu.gamePassMembership}";
                    }
                    catch
                    {
                        Gamepass = $"Gamepass: Unknown";
                    }
                    
                    ActiveDevice = $"Active Device: {Jsonresponse.people[0].presenceDetails[0].Device}";
                    IsVerified = $"Verified: {Jsonresponse.people[0].detail.isVerified}";
                    Location = $"Location: {Jsonresponse.people[0].detail.location}";
                    Tenure = $"Tenure: {Jsonresponse.people[0].detail.tenure}";
                    Following = $"Following: {Jsonresponse.people[0].detail.followingCount}";
                    Followers = $"Followers: {Jsonresponse.people[0].detail.followerCount}";
                    Bio = $"Bio: {Jsonresponse.people[0].detail.bio}";

                    Watermarks.Clear();

                    // Tenure image format, 01..05..10
                    // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/tenure/15.png
                    // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/launch/ba75b64a-9a80-47ea-8c3a-76d3e2ea1422.png
                    // https://dlassets-ssl.xboxlive.com/public/content/ppl/watermarks/launch/xboxoneteam.png
                    var tenureBadge = Jsonresponse.people[0].detail.tenure.ToString("D2");
                    Watermarks.Add(new ImageItem {ImageUrl = $@"{WatermarksUrl}tenure/{tenureBadge}.png"});
                    string[] watermarkNames = Jsonresponse.people[0].detail.watermarks.ToObject<string[]>();
                    foreach (var watermark in watermarkNames) 
                    {
                        Watermarks.Add(new ImageItem { ImageUrl = $@"{WatermarksUrl}launch/{watermark.ToLower()}.png" });
                    }
                }
                GrabbedProfile = true;
                _snackbarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            }
            catch (HttpRequestException ex)
            {
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                {
                    IsLoggedIn = false;
                    XAUTHTested = false;
                    _snackbarService.Show("401 Unauthorized", "Something went wrong. Retrying", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }
                
            }
            
            
        }
#endregion
        
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
}
