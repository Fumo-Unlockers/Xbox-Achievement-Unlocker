using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.ViewModels.Windows;
using XAU.Views.Windows;
using static XAU.ViewModels.Pages.AchievementsViewModel;
using System.Text.RegularExpressions;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Runtime.InteropServices;
using System.Text.Unicode;

namespace XAU.ViewModels.Pages
{
    public partial class AchievementsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _titleIDOverride ="0";
        [ObservableProperty] private bool _unlockable = false;
        [ObservableProperty] private bool _titleIDEnabled = false;
        [ObservableProperty] private ObservableCollection<Achievement> _achievements = new ObservableCollection<Achievement>();
        [ObservableProperty] private ObservableCollection<DGAchievement> _dGAchievements = new ObservableCollection<DGAchievement>();
        [ObservableProperty] public string _gameInfo = "";
        [ObservableProperty] private string _gameName = "";
        [ObservableProperty] private bool _isUnlockAllEnabled = false;
        [ObservableProperty] private string _searchText = "";
        public static bool SpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
        public static string TitleID="0";
        private bool IsTitleIDValid = false;
        public static bool NewGame = false;
        public static bool IsSelectedGame360;
        private dynamic AchievementResponse = (dynamic)(new JObject());
        private dynamic GameInfoResponse = (dynamic)(new JObject());
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        public static bool SpoofingUpdate = false;
        private bool IsFiltered = false;
        private bool IsEventBased = false;
        private dynamic EventsData = (dynamic)(new JObject());
        public static string EventsToken;

        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            //This is an absolutely terrible idea but the stupid fucking events API just cries about SSL errors
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
    };
        HttpClient client = new HttpClient(handler);


        public AchievementsViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);



        public class Achievement
        {
            public string id { get; set; }
            public string serviceConfigId { get; set; }
            public string name { get; set; }
            public string titleAssociationsname { get; set; }
            public string titleAssociationsid { get; set; }
            public string progressState { get; set; }
            public string progressionrequirementsid { get; set; }
            public string progressionrequirementscurrent { get; set; }
            public string progressionrequirementstarget { get; set; }
            public string progressionrequirementsoperationType { get; set; }
            public string progressionrequirementsvalueType { get; set; }
            public string progressionrequirementsruleParticipationType { get; set; }
            public string progressiontimeUnlocked { get; set; }
            public string mediaAssetsname { get; set; }
            public string mediaAssetstype { get; set; }
            public string mediaAssetsurl { get; set; }
            public List<string> platforms { get; set; }
            public string isSecret { get; set; }
            public string description { get; set; }
            public string lockedDescription { get; set; }
            public string productId { get; set; }
            public string achievementType { get; set; }
            public string participationType { get; set; }
            public string timeWindow { get; set; }
            public string rewardsname { get; set; }
            public string rewardsdescription { get; set; }
            public string rewardsvalue { get; set; }
            public string rewardstype { get; set; }
            public string rewardsmediaAsset { get; set; }
            public string rewardsvalueType { get; set; }
            public string estimatedTime { get; set; }
            public string deeplink { get; set; }
            public string isRevoked { get; set; }
            public string raritycurrentCategory { get; set; }
            public string raritycurrentPercentage { get; set; }

        }

        public class DGAchievement
        {
            public int Index { get; set; }
            public int ID { get; set; }
            public string Name { get; set; }
            public string Description { get; set; }
            public string IsSecret { get; set; }
            public DateTime DateUnlocked { get; set; }
            public int Gamerscore { get; set; }
            public float RarityPercentage { get; set; }
            public string RarityCategory { get; set; }
            public string ProgressState { get; set; }
            public bool IsUnlockable { get; set; }
        }
        public void OnNavigatedTo()
        {
            if (SpooferEnabled)
            {
                if (HomeViewModel.SpoofingStatus == 1 && !(GameInfo == ""))
                {
                    if (HomeViewModel.SpoofedTitleID == TitleIDOverride)
                    {
                        GameInfo = "Manually Spoofing";
                        GameName = GameInfoResponse.titles[0].name.ToString();
                    }
                    else
                    {
                        GameInfo = "Spoofing Another Game";
                        GameName = GameInfoResponse.titles[0].name.ToString();
                    }
                        
                }
                else if (HomeViewModel.SpoofingStatus == 0 && !(GameInfo == ""))
                {
                    SpoofGame();
                }
            }
            
            if (IsInitialized&&NewGame)
                RefreshAchievements();
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

        private void InitializeViewModel()
        {
            if (IsSelectedGame360)
                Unlockable = false;
            LoadGameInfo();
            LoadAchievements();
            if (SpooferEnabled)
                SpoofGame();
            TitleIDEnabled = true;
            IsInitialized = true;
            NewGame = false;
            if (HomeViewModel.Settings.RegionOverride)
                currentSystemLanguage = "en-GB";
        }


        private async void LoadGameInfo()
        {
            if (TitleID != "0")
            {
                TitleIDOverride = TitleID;
                TitleID = "0";
            }
            GameInfo = "";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            StringContent requestbody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + TitleIDOverride + "\"]}");
            GameInfoResponse = (dynamic)JObject.Parse(await client.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestbody).Result.Content.ReadAsStringAsync());
            try
            {
                IsSelectedGame360 = GameInfoResponse.titles[0].devices.ToString().Contains("Xbox360") || GameInfoResponse.titles[0].devices.ToString().Contains("Mobile");
                GameInfo = GameInfoResponse.titles[0].name.ToString();
                IsTitleIDValid = true;
            }
            catch
            {
                GameInfo = "Error";
                IsTitleIDValid = false;
                return;
            }

        }

        private async void SpoofGame()
        {
            if (HomeViewModel.SpoofingStatus == 1)
            {
                if (HomeViewModel.SpoofedTitleID == TitleIDOverride)
                {
                    GameInfo = "Manually Spoofing";
                    GameName = GameInfoResponse.titles[0].name.ToString();
                }
                else
                {
                    GameInfo = "Spoofing Another Game";
                    GameName = GameInfoResponse.titles[0].name.ToString();
                }
            }
            else
            {
                HomeViewModel.AutoSpoofedTitleID = TitleIDOverride;
                HomeViewModel.SpoofingStatus = 2;
                GameInfo = "Auto Spoofing";
                GameName = GameInfoResponse.titles[0].name.ToString();
                await Task.Run(() => Spoofing());
                if (HomeViewModel.SpoofingStatus == 1)
                {
                    if (HomeViewModel.SpoofedTitleID == HomeViewModel.AutoSpoofedTitleID)
                    {
                        GameInfo = "Manually Spoofing";
                        GameName = GameInfoResponse.titles[0].name.ToString();
                    }
                    else
                    {
                        GameInfo = "Spoofing Another Game";
                        GameName = GameInfoResponse.titles[0].name.ToString();
                    }
                }
                HomeViewModel.AutoSpoofedTitleID = "0";
            }
            

        }

        public async Task Spoofing()
        {
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            var requestbody =
                new StringContent(
                    "{\"titles\":[{\"expiration\":600,\"id\":" + HomeViewModel.AutoSpoofedTitleID +
                    ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
            await client.PostAsync(
                "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/devices/current",
                requestbody);
            var i = 0;
            Thread.Sleep(1000);
            SpoofingUpdate = false;
            while (!SpoofingUpdate)
            {
                if (i == 300)
                {
                    client.DefaultRequestHeaders.Clear();
                    client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
                    client.DefaultRequestHeaders.Add("accept", "application/json");
                    client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
                    await client.PostAsync(
                        "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly +
                        ")/devices/current", requestbody);
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

        private async void LoadAchievements()
        {
            Achievements.Clear();
            DGAchievements.Clear();
            if (!IsTitleIDValid)
                return;
            if (!IsSelectedGame360)
            {
                Unlockable=true;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "4");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
                client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
                client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                AchievementResponse = (dynamic)JObject.Parse(await client.GetAsync("https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/achievements?titleId=" + TitleIDOverride + "&maxItems=1000").Result.Content.ReadAsStringAsync());
                try
                {
                    if (AchievementResponse.achievements[0].progression.requirements.ToString().Length > 2)
                    {
                        if (AchievementResponse.achievements[0].progression.requirements[0].id !=
                            "00000000-0000-0000-0000-000000000000")
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
                    if (AchievementResponse.achievements[i].progression.requirements.ToString().Length > 2)
                    {
                        if (AchievementResponse.achievements[i].progression.requirements[0].id !=
                            "00000000-0000-0000-0000-000000000000")
                        {
                            Unlockable = false;
                            IsEventBased = true;
                        }
                        else
                        {
                            Unlockable = true;
                            IsEventBased = false;
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
                        rewardnameplaceholder = AchievementResponse.achievements[i].rewards[0].name.ToString();
                        rewarddescriptionplaceholder = AchievementResponse.achievements[i].rewards[0].description.ToString();
                        rewardvalueplaceholder = AchievementResponse.achievements[i].rewards[0].value.ToString();
                        rewardtypeplaceholder = AchievementResponse.achievements[i].rewards[0].type.ToString();
                        rewardmediaAssetplaceholder = AchievementResponse.achievements[i].rewards[0].mediaAsset.ToString();
                        rewardvalueTypeplaceholder = AchievementResponse.achievements[i].rewards[0].valueType.ToString();
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

                    Achievements.Add( new Achievement()
                        {
                            id = AchievementResponse.achievements[i].id.ToString(),
                            serviceConfigId = AchievementResponse.achievements[i].serviceConfigId.ToString(),
                            name = AchievementResponse.achievements[i].name.ToString(),
                            titleAssociationsname = AchievementResponse.achievements[i].titleAssociations[0].name.ToString(),
                            titleAssociationsid = AchievementResponse.achievements[i].titleAssociations[0].id.ToString(),
                            progressState = AchievementResponse.achievements[i].progressState.ToString(),
                            //these are strings because im too lazy to handle them properly right now
                            progressionrequirementsid = "AchievementResponse.achievements[i].progression.requirements[0].id.ToString()",
                            progressionrequirementscurrent = "AchievementResponse.achievements[i].progression.requirements[0].current.ToString()",
                            progressionrequirementstarget = "AchievementResponse.achievements[i].progression.requirements[0].target.ToString()",
                            progressionrequirementsoperationType = "AchievementResponse.achievements[i].progression.requirements[0].operationType.ToString()",
                            progressionrequirementsvalueType = "AchievementResponse.achievements[i].progression.requirements[0].valueType.ToString()",
                            progressionrequirementsruleParticipationType = "AchievementResponse.achievements[i].progression.requirements[0].ruleParticipationType.ToString()",
                            progressiontimeUnlocked = AchievementResponse.achievements[i].progression.timeUnlocked.ToString(),
                            mediaAssetsname = AchievementResponse.achievements[i].mediaAssets[0].name.ToString(),
                            mediaAssetstype = AchievementResponse.achievements[i].mediaAssets[0].type.ToString(),
                            mediaAssetsurl = AchievementResponse.achievements[i].mediaAssets[0].url.ToString(),
                            platforms = AchievementResponse.achievements[i].platforms.ToObject<List<string>>(),
                            isSecret = AchievementResponse.achievements[i].isSecret.ToString(),
                            description = AchievementResponse.achievements[i].description.ToString(),
                            lockedDescription = AchievementResponse.achievements[i].lockedDescription.ToString(),
                            productId = AchievementResponse.achievements[i].productId.ToString(),
                            achievementType = AchievementResponse.achievements[i].achievementType.ToString(),
                            participationType = AchievementResponse.achievements[i].participationType.ToString(),
                            timeWindow = AchievementResponse.achievements[i].timeWindow.ToString(),
                            rewardsname = rewardnameplaceholder,
                            rewardsdescription = rewarddescriptionplaceholder,
                            rewardsvalue = rewardvalueplaceholder,
                            rewardstype = rewardtypeplaceholder,
                            rewardsmediaAsset = rewardmediaAssetplaceholder,
                            rewardsvalueType = rewardvalueTypeplaceholder,
                            estimatedTime = AchievementResponse.achievements[i].estimatedTime.ToString(),
                            deeplink = AchievementResponse.achievements[i].deeplink.ToString(),
                            isRevoked = AchievementResponse.achievements[i].isRevoked.ToString(),
                            raritycurrentCategory = AchievementResponse.achievements[i].rarity.currentCategory.ToString(),
                            raritycurrentPercentage = AchievementResponse.achievements[i].rarity.currentPercentage.ToString()
                        }
                    );
                }
                foreach (var achievement in Achievements)
                {
                    var gamerscore = 0;
                    if (achievement.rewardstype == "Gamerscore")
                    {
                        gamerscore = int.Parse(achievement.rewardsvalue);
                    }
                    DGAchievements.Add(new DGAchievement()
                    {
                        Index = Achievements.IndexOf(achievement),
                        ID = int.Parse(achievement.id),
                        Name = achievement.name,
                        Description = achievement.description,
                        IsSecret = achievement.isSecret,
                        DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                        Gamerscore = gamerscore,
                        RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                        RarityCategory = achievement.raritycurrentCategory,
                        ProgressState = achievement.progressState,
                        IsUnlockable = achievement.progressState != "Achieved" && Unlockable && !IsEventBased
                    });
                }
            }
            else
            {
                Unlockable = false;
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
                client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
                client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                AchievementResponse = (dynamic)JObject.Parse(await client.GetAsync("https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/titleachievements?titleId=" + TitleIDOverride + "&maxItems=1000").Result.Content.ReadAsStringAsync());
                if (AchievementResponse.achievements.Count == 0)
                {
                    _snackbarService.Show("Error: No Achievements", $"There were no achievements returned from the API", ControlAppearance.Danger,
                                               new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }
                //cut down version of the code to display minimal information about 360 achievements
                for (int i = 0; i < AchievementResponse.achievements.Count; i++)
                {
                    var rewardnameplaceholder = "";
                    var rewarddescriptionplaceholder = "";
                    var rewardvalueplaceholder = "";
                    var rewardtypeplaceholder = "";
                    var rewardmediaAssetplaceholder = "";
                    var rewardvalueTypeplaceholder = "";
                    try
                    {
                        rewardnameplaceholder = AchievementResponse.achievements[i].rewards[0].name.ToString();
                        rewarddescriptionplaceholder = AchievementResponse.achievements[i].rewards[0].description.ToString();
                        rewardvalueplaceholder = AchievementResponse.achievements[i].rewards[0].value.ToString();
                        rewardtypeplaceholder = AchievementResponse.achievements[i].rewards[0].type.ToString();
                        rewardmediaAssetplaceholder = AchievementResponse.achievements[i].rewards[0].mediaAsset.ToString();
                        rewardvalueTypeplaceholder = AchievementResponse.achievements[i].rewards[0].valueType.ToString();
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

                    Achievements.Add(new Achievement()
                    {
                        id = AchievementResponse.achievements[i].id.ToString(),
                        serviceConfigId = "Null",
                        name = AchievementResponse.achievements[i].name.ToString(),
                        progressState = "Null",
                        progressiontimeUnlocked = "0001-01-01T00:00:00.0000000Z",
                        isSecret = AchievementResponse.achievements[i].isSecret.ToString(),
                        description = AchievementResponse.achievements[i].description.ToString(),
                        rewardsname = "Null",
                        rewardsdescription = "Null",
                        rewardsvalue = AchievementResponse.achievements[i].gamerscore.ToString(),
                        rewardstype = "Gamerscore",
                        rewardsmediaAsset = "Null",
                        rewardsvalueType = rewardvalueTypeplaceholder,
                        raritycurrentCategory = AchievementResponse.achievements[i].rarity.currentCategory.ToString(),
                        raritycurrentPercentage = AchievementResponse.achievements[i].rarity.currentPercentage.ToString()
                    }
                    );
                }
                foreach (var achievement in Achievements)
                {
                    var gamerscore = 0;
                    if (achievement.rewardstype == "Gamerscore")
                    {
                        gamerscore = int.Parse(achievement.rewardsvalue);
                    }
                    DGAchievements.Add(new DGAchievement()
                    {
                        Index = Achievements.IndexOf(achievement),
                        ID = int.Parse(achievement.id),
                        Name = achievement.name,
                        Description = achievement.description,
                        IsSecret = achievement.isSecret,
                        DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                        Gamerscore = gamerscore,
                        RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                        RarityCategory = achievement.raritycurrentCategory,
                        ProgressState = achievement.progressState,
                        IsUnlockable = achievement.progressState != "Achieved" && Unlockable
                    });
                }
            }

            if (IsSelectedGame360)
            {
                _snackbarService.Show("Warning: Unsupported Game", $"This tool does not support Xbox 360 titles", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
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
                    EventsData = (dynamic)(JObject)data[TitleIDOverride];
                    foreach (var achievement in DGAchievements)
                    {
                        if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != "Achieved")
                        {
                            achievement.IsUnlockable = true;
                        }
                    }
                }
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

        public async void UnlockAchievement(int AchievementIndex)
        {
            if (!IsEventBased)
            {
                var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + AchievementResponse.achievements[0].serviceConfigId + "\",\"titleId\":\"" + AchievementResponse.achievements[0].titleAssociations[0].id + "\",\"userId\":\"" + HomeViewModel.XUIDOnly + "\",\"achievements\":[{\"id\":\"" + DGAchievements[AchievementIndex].ID + "\",\"percentComplete\":\"100\"}]}";
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
                client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
                client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
                client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
                client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
                client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
                if (HomeViewModel.Settings.FakeSignatureEnabled)
                    client.DefaultRequestHeaders.Add("Signature", "RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==");
                var bodyconverted = new StringContent(requestbody, Encoding.UTF8, "application/json");
                try
                {
                    await client.PostAsync(
                        "https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/achievements/" +
                        AchievementResponse.achievements[0].serviceConfigId + "/update", bodyconverted);
                    _snackbarService.Show("Achievement Unlocked", $"{DGAchievements[AchievementIndex].Name} has been unlocked",
                        ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                    DGAchievements[AchievementIndex].IsUnlockable = false;
                    DGAchievements[AchievementIndex].ProgressState = "Achieved";
                    DGAchievements[AchievementIndex].DateUnlocked = DateTime.Now;
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
                if (EventsToken == null)
                {
                    _snackbarService.Show("Error: No Events Token", "No events token was set", ControlAppearance.Danger,
                                               new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }
                var authxtoken = HomeViewModel.XAUTH;
                authxtoken = Regex.Replace(authxtoken, @"XBL3\.0 x=\d+;", "XBL3.0 x=-;");
                client.DefaultRequestHeaders.Clear();
                client.DefaultRequestHeaders.Add("user-agent", "MSDW");
                client.DefaultRequestHeaders.Add("cache-control","no-cache");
                client.DefaultRequestHeaders.Add("accept", "application/json");
                client.DefaultRequestHeaders.Add("accept-encoding","gzip, deflate");
                client.DefaultRequestHeaders.Add("reliability-mode","standard");
                client.DefaultRequestHeaders.Add("client-version","EUTC-Windows-C++-no-10.0.22621.3296.amd64fre.ni_release.220506-1250-no");
                client.DefaultRequestHeaders.Add("apikey","0890af88a9ed4cc886a14f5e174a2827-9de66c5e-f867-43a8-a7b8-e0ddd481cca4-7548,95c1f21d6cb047a09e7b423c1cb2222e-9965f07b-54fa-498e-9727-9e8d24dec39e-7027");
                client.DefaultRequestHeaders.Add("authxtoken", authxtoken);
                client.DefaultRequestHeaders.Add("tickets", $"\"1\"=\"{EventsToken}\"");
                client.DefaultRequestHeaders.Add("Client-Id", "NO_AUTH");
                client.DefaultRequestHeaders.Add("Host", "v20.events.data.microsoft.com");
                client.DefaultRequestHeaders.Add("Connection", "close"); 
                var requestbody = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\XAU\\Events\\{TitleIDOverride}.json");
                DateTime timestamp = DateTime.UtcNow;
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
                    await client.PostAsync(@"https://v20.events.data.microsoft.com/OneCollector/1.0/", bodyconverted);
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

        [RelayCommand]
        public async void UnlockAll()
        {
            var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" +
                              AchievementResponse.achievements[0].serviceConfigId + "\",\"titleId\":\"" +
                              AchievementResponse.achievements[0].titleAssociations[0].id + "\",\"userId\":\"" +
                              HomeViewModel.XUIDOnly + "\",\"achievements\":[";
            client.DefaultRequestHeaders.Clear();
            client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            client.DefaultRequestHeaders.Add("accept", "application/json");
            client.DefaultRequestHeaders.Add("accept-language", currentSystemLanguage);
            client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAUTH);
            client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
            client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
            client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
            if (HomeViewModel.Settings.FakeSignatureEnabled)
                client.DefaultRequestHeaders.Add("Signature", "RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==");
            foreach (Achievement achievement in Achievements)
            {
                if (achievement.progressState != "Achieved")
                {
                    requestbody += "{\"id\":\"" +achievement.id + "\",\"percentComplete\":\"100\"},";
                }
            }
            requestbody = requestbody.Remove(requestbody.Length - 1)+ "]}";
            var bodyconverted = new StringContent(requestbody, Encoding.UTF8, "application/json");
            try
            {
                await client.PostAsync(
                    "https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUIDOnly + ")/achievements/" +
                    AchievementResponse.achievements[0].serviceConfigId + "/update", bodyconverted);
                _snackbarService.Show("All Achievements Unlocked", $"All Achievements for this game have been unlocked",
                    ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                var unlocktime = DateTime.Now;
                foreach (DGAchievement achievement in DGAchievements)
                {
                    
                    if (achievement.ProgressState != "Achieved")
                    {
                        achievement.IsUnlockable = false;
                        achievement.ProgressState = "Achieved";
                        achievement.DateUnlocked = unlocktime;
                    }
                }
                CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
            }
            catch 
            {

            }
        }

        [RelayCommand]
        public async void RefreshAchievements()
        {
            LoadGameInfo();
            LoadAchievements();
            NewGame = false;
            if (SpooferEnabled)
                SpoofGame();
        }

        [RelayCommand]
        public void SearchAndFilterAchievements()
        {
            if (IsEventBased)
            {
                string DataPath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + "\\XAU\\Events\\Data.json";
                var data = JObject.Parse(File.ReadAllText(DataPath));
                JArray SupportedGamesJ = (JArray)data["SupportedTitleIDs"];
                List<int> SupportedGames = SupportedGamesJ.ToObject<List<int>>();
                if (SupportedGames.Contains(int.Parse(TitleIDOverride)))
                {
                    Unlockable = true;
                    EventsData = (dynamic)(JObject)data[TitleIDOverride];

                }
            }
            
            CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
            if (DGAchievements.Count == 0)
            {
                _snackbarService.Show("Error", $"No Achievements Loaded", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
            if (SearchText.Length == 0 && IsFiltered == false)
            {
                _snackbarService.Show("Error", $"Please Enter Query Text", ControlAppearance.Danger, new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                return;
            }
            if (SearchText.Length == 0 && IsFiltered)
            {
                DGAchievements.Clear();
                foreach (var achievement in Achievements)
                {
                    if (!IsSelectedGame360)
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == "Gamerscore")
                        {
                            gamerscore = int.Parse(achievement.rewardsvalue);
                        }
                        DGAchievements.Add(new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != "Achieved" && Unlockable && !IsEventBased
                        });
                    }
                    else
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == "Gamerscore")
                        {
                            gamerscore = int.Parse(achievement.rewardsvalue);
                        }
                        DGAchievements.Add(new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != "Achieved" && Unlockable && !IsEventBased
                        });
                    }
                }
                if (IsEventBased && Unlockable)
                {
                    foreach (var achievement in DGAchievements)
                    {
                        if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != "Achieved")
                        {
                            achievement.IsUnlockable = true;
                        }
                    }
                    CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
                }
                IsFiltered = false;
                return;
            }
            DGAchievements.Clear();
            foreach (var achievement in Achievements)
            {
                if (achievement.name.ToLower().Contains(SearchText.ToLower()) || achievement.description.ToLower().Contains(SearchText.ToLower()))
                {
                    if (!IsSelectedGame360)
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == "Gamerscore")
                        {
                            gamerscore = int.Parse(achievement.rewardsvalue);
                        }
                        DGAchievements.Add(new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != "Achieved" && Unlockable && !IsEventBased
                        });
                    }
                    else
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == "Gamerscore")
                        {
                            gamerscore = int.Parse(achievement.rewardsvalue);
                        }
                        DGAchievements.Add(new DGAchievement()
                        {
                            Index = DGAchievements.Count,
                            ID = int.Parse(achievement.id),
                            Name = achievement.name,
                            Description = achievement.description,
                            IsSecret = achievement.isSecret,
                            DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                            Gamerscore = gamerscore,
                            RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                            RarityCategory = achievement.raritycurrentCategory,
                            ProgressState = achievement.progressState,
                            IsUnlockable = achievement.progressState != "Achieved" && Unlockable && !IsEventBased
                        });
                    }
                }
            }

            if (IsEventBased && Unlockable)
            {
                foreach (var achievement in DGAchievements)
                {
                    if (EventsData.Achievements.ContainsKey(achievement.ID.ToString()) && achievement.ProgressState != "Achieved")
                    {
                        achievement.IsUnlockable = true;
                    }
                }
                CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
            }
            IsFiltered = true;
        }
    }
}
