using System.ComponentModel;
using System.Windows.Media;
using Wpf.Ui.Controls;
using Memory;
using System.Net.Http;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;

namespace XAU.ViewModels.Pages
{
    public partial class DashboardViewModel : ObservableObject
    {
        //attach vars
        [ObservableProperty] private string _attached = "Not Attached";
        [ObservableProperty] private Brush _attachedColor = new SolidColorBrush(Colors.Red);
        [ObservableProperty] private string _loggedIn = "Not Logged In";
        [ObservableProperty] private Brush _loggedInColor = new SolidColorBrush(Colors.Red);

        //profile vars
        [ObservableProperty] private String _gamerPic = "pack://application:,,,/Assets/cirno.png";
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

        //SnackBar
        public DashboardViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
        }
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        [RelayCommand]
        private void RefreshProfile()
        {
            GrabProfile();
        }

        Mem m = new Mem();
        public BackgroundWorker XauthWorker = new BackgroundWorker();
        bool IsAttached = false;
        bool IsLoggedIn = false;
        bool GrabbedProfile = false;
        bool XAUTHTested = false;
        public string XAUTH="";
        public string XUIDOnly;
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        HttpClient client = new HttpClient(handler);

        public async void XauthWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            while (true)
            {
                if (!m.OpenProcess("XboxPcApp"))
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
                Attached = $"Attached to xbox app ({m.GetProcIdFromName("XboxPCApp").ToString()})";
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
            if (m.GetProcIdFromName("XboxPCApp") == 0)
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
            IEnumerable<long> XauthScanList = await m.AoBScan("58 42 4C 33 2E 30 20 78 3D", true, true);
            string[] XauthStrings = new string[XauthScanList.Count()];
            var i = 0;
            foreach (var address in XauthScanList)
            {
                XauthStrings[i] = m.ReadString(address.ToString("X"), length: 10000);
                i++;
            }

            Dictionary<string, int> frequency = new Dictionary<string, int>();
            foreach (string str in XauthStrings)
            {
                if (!frequency.ContainsKey(str))
                {
                    frequency[str] = 1;
                }
                else
                {
                    frequency[str]++;
                }
            }

            string mostCommon = XauthStrings[0];
            int highestFrequency = 0;
            foreach (KeyValuePair<string, int> pair in frequency)
            {
                if (pair.Value > highestFrequency)
                {
                    mostCommon = pair.Key;
                    highestFrequency = pair.Value;
                }
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
                GamerTag = $"Gamertag: {Jsonresponse.profileUsers[0].settings[0].value}";
                Xuid = $"XUID: {Jsonresponse.profileUsers[0].id}";
                XUIDOnly = Jsonresponse.profileUsers[0].id;
                IsLoggedIn = true;
                XAUTHTested= true;
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
                GamerPic = Jsonresponse.people[0].displayPicRaw;
                GamerScore = $"Gamerscore: {Jsonresponse.people[0].gamerScore}";
                ProfileRep = $"Reputation: {Jsonresponse.people[0].xboxOneRep}";
                AccountTier = $"Tier: {Jsonresponse.people[0].detail.accountTier}";
                CurrentlyPlaying = $"Currently Playing: {Jsonresponse.people[0].presenceDetails[0].TitleId}";
                ActiveDevice = $"Active Device: {Jsonresponse.people[0].presenceDetails[0].Device}";
                IsVerified = $"Verified: {Jsonresponse.people[0].detail.isVerified}";
                Location = $"Location: {Jsonresponse.people[0].detail.location}";
                Tenure = $"Tenure: {Jsonresponse.people[0].detail.tenure}";
                Following = $"Following: {Jsonresponse.people[0].detail.followingCount}";
                Followers = $"Followers: {Jsonresponse.people[0].detail.followerCount}";
                Gamepass = $"Gamepass: {Jsonresponse.people[0].detail.hasGamePass}";
                Bio = $"Bio: {Jsonresponse.people[0].detail.bio}";
                GrabbedProfile = true;
                _snackbarService.Show("Success", "Profile information grabbed.", ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
            }
            catch (HttpRequestException ex)
            {
                if ((int)ex.StatusCode == 401)
                {
                    IsLoggedIn = false;
                    XAUTHTested = false;
                    _snackbarService.Show("401 Unauthorised", "Something went wrong. Retrying", ControlAppearance.Success, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                }
                
            }
            
            
        }


    }
}
