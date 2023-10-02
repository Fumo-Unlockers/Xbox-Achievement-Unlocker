using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Printing;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.ViewModels.Windows;
using XAU.Views.Windows;
using static XAU.ViewModels.Pages.AchievementsViewModel;

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
        [ObservableProperty] public string _gameInfo = "Not Spoofing: ";
        [ObservableProperty] private bool _isUnlockAllEnabled = false;
        public static bool SpooferEnabled = false;
        public static string TitleID="0";
        private bool IsTitleIDValid = false;
        public static bool NewGame = false;
        public static bool IsSelectedGame360;
        private dynamic AchievementResponse = (dynamic)(new JObject());
        private dynamic GameInfoResponse = (dynamic)(new JObject());
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;

        static HttpClientHandler handler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
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
            GameInfo = "Not Spoofing: ";
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
                GameInfo = "Not Spoofing: " + GameInfoResponse.titles[0].name.ToString();
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

        }

        private async void LoadAchievements()
        {
            Achievements.Clear();
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
                            rewardsname = AchievementResponse.achievements[i].rewards[0].name.ToString(),
                            rewardsdescription = AchievementResponse.achievements[i].rewards[0].description.ToString(),
                            rewardsvalue = AchievementResponse.achievements[i].rewards[0].value.ToString(),
                            rewardstype = AchievementResponse.achievements[i].rewards[0].type.ToString(),
                            rewardsmediaAsset = AchievementResponse.achievements[i].rewards[0].mediaAsset.ToString(),
                            rewardsvalueType = AchievementResponse.achievements[i].rewards[0].valueType.ToString(),
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
                    DGAchievements.Add(new DGAchievement()
                    {
                        Index = Achievements.IndexOf(achievement),
                        ID = int.Parse(achievement.id),
                        Name = achievement.name,
                        Description = achievement.description,
                        IsSecret = achievement.isSecret,
                        DateUnlocked = DateTime.Parse(achievement.progressiontimeUnlocked),
                        Gamerscore = int.Parse(achievement.rewardsvalue),
                        RarityPercentage = float.Parse(achievement.raritycurrentPercentage),
                        RarityCategory = achievement.raritycurrentCategory,
                        ProgressState = achievement.progressState,
                        IsUnlockable = achievement.progressState != "Achieved" && Unlockable
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
            }

            if (IsSelectedGame360)
            {
                _snackbarService.Show("Warning: Unsupported Game", $"This tool does not support Xbox 360 titles", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }
            else if (!Unlockable)
            {
                _snackbarService.Show("Warning: Unsupported Game", $"This tool does not support Event Based titles", ControlAppearance.Caution,
                    new SymbolIcon(SymbolRegular.Warning24), _snackbarDuration);
            }

            if (HomeViewModel.Settings.UnlockAllEnabled && Unlockable)
                IsUnlockAllEnabled = Unlockable;
            else
                IsUnlockAllEnabled = false;
        }

        public async void UnlockAchievement(int AchievementIndex)
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

        [RelayCommand]
        public async void RefreshAchievements()
        {
            Achievements.Clear();
            LoadGameInfo();
            LoadAchievements();
            if (SpooferEnabled)
                SpoofGame();
            NewGame = false;

        }
    }
}
