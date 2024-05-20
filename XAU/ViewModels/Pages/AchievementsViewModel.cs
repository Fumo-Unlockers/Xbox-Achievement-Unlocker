using System.Collections.ObjectModel;
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

namespace XAU.ViewModels.Pages
{
    public partial class AchievementsViewModel : ObservableObject, INavigationAware
    {
        [ObservableProperty] private bool _isInitialized = false;
        [ObservableProperty] private string _titleIDOverride = "0";
        [ObservableProperty] private bool _unlockable = false;
        [ObservableProperty] private bool _titleIDEnabled = false;
        [ObservableProperty] private ObservableCollection<AchievementEntryResponse> _achievements = new ObservableCollection<AchievementEntryResponse>();
        [ObservableProperty] private ObservableCollection<DGAchievement> _dGAchievements = new ObservableCollection<DGAchievement>();
        [ObservableProperty] public string _gameInfo = "";
        [ObservableProperty] private string _gameName = "";
        [ObservableProperty] private bool _isUnlockAllEnabled = false;
        [ObservableProperty] private string _searchText = "";
        public static bool SpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;
        public static string TitleID = "0";
        private bool IsTitleIDValid = false;
        public static bool NewGame = false;
        public static bool IsSelectedGame360;
        private AchievementsResponse AchievementResponse = new AchievementsResponse();
        private GameTitle GameInfoResponse = new GameTitle();
        string currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
        // TODO: this needs to be updated if language changes
        private Lazy<XboxRestAPI> _xboxRestAPI = new Lazy<XboxRestAPI>(() => new XboxRestAPI(HomeViewModel.XAUTH, System.Globalization.CultureInfo.CurrentCulture.Name));

        public static bool SpoofingUpdate = false;
        private bool IsFiltered = false;
        private bool IsEventBased = false;
        private dynamic EventsData = (dynamic)(new JObject());
        public static string EventsToken;

        public AchievementsViewModel(ISnackbarService snackbarService)
        {
            _snackbarService = snackbarService;
            _contentDialogService = new ContentDialogService();
        }

        private readonly IContentDialogService _contentDialogService;
        private readonly ISnackbarService _snackbarService;
        private TimeSpan _snackbarDuration = TimeSpan.FromSeconds(2);

        public class DGAchievement
        {
            public int Index { get; set; }
            public int ID { get; set; }
            public string? Name { get; set; }
            public string? Description { get; set; }
            public string? IsSecret { get; set; }
            public DateTime DateUnlocked { get; set; }
            public int Gamerscore { get; set; }
            public float RarityPercentage { get; set; }
            public string? RarityCategory { get; set; }
            public string? ProgressState { get; set; }
            public bool IsUnlockable { get; set; }
        }
        public async void OnNavigatedTo()
        {
            if (SpooferEnabled)
            {
                if (HomeViewModel.SpoofingStatus == 1 && !(GameInfo == ""))
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
                else if (HomeViewModel.SpoofingStatus == 0 && !(GameInfo == ""))
                {
                    SpoofGame();
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

        private async void InitializeViewModel()
        {
            if (IsSelectedGame360)
                Unlockable = false;
            await LoadGameInfo();
            await LoadAchievements();
            if (SpooferEnabled)
                SpoofGame();
            TitleIDEnabled = true;
            IsInitialized = true;
            NewGame = false;
            if (HomeViewModel.Settings.RegionOverride)
                currentSystemLanguage = "en-GB";
        }


        private async Task LoadGameInfo()
        {
            if (TitleID != "0")
            {
                TitleIDOverride = TitleID;
                TitleID = "0";
            }
            GameInfo = "";
            GameInfoResponse = await _xboxRestAPI.Value.GetGameTitleAsync(HomeViewModel.XUIDOnly, TitleIDOverride);
            try
            {
                IsSelectedGame360 = GameInfoResponse.Titles[0].Devices.Contains("Xbox360") || GameInfoResponse.Titles[0].Devices.Contains("Mobile");
                GameInfo = GameInfoResponse.Titles[0].Name;
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
                GameName = GameInfoResponse.Titles[0].Name;
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

                    Achievements.Add(new AchievementEntryResponse()
                    {
                        id = AchievementResponse.achievements[i].id,
                        serviceConfigId = AchievementResponse.achievements[i].serviceConfigId,
                        name = AchievementResponse.achievements[i].name,
                        titleAssociationsname = AchievementResponse.achievements[i].titleAssociations[0].name,
                        titleAssociationsid = AchievementResponse.achievements[i].titleAssociations[0].id,
                        progressState = AchievementResponse.achievements[i].progressState,
                        //these are strings because im too lazy to handle them properly right now
                        progressionrequirementsid = "AchievementResponse.achievements[i].progression.requirements[0].id",
                        progressionrequirementscurrent = "AchievementResponse.achievements[i].progression.requirements[0].current",
                        progressionrequirementstarget = "AchievementResponse.achievements[i].progression.requirements[0].target",
                        progressionrequirementsoperationType = "AchievementResponse.achievements[i].progression.requirements[0].operationType",
                        progressionrequirementsvalueType = "AchievementResponse.achievements[i].progression.requirements[0].valueType",
                        progressionrequirementsruleParticipationType = "AchievementResponse.achievements[i].progression.requirements[0].ruleParticipationType",
                        progressiontimeUnlocked = AchievementResponse.achievements[i].progression.timeUnlocked,
                        mediaAssetsname = AchievementResponse.achievements[i].mediaAssets[0].name,
                        mediaAssetstype = AchievementResponse.achievements[i].mediaAssets[0].type,
                        mediaAssetsurl = AchievementResponse.achievements[i].mediaAssets[0].url,
                        platforms = AchievementResponse.achievements[i].platforms,
                        isSecret = AchievementResponse.achievements[i].isSecret,
                        description = AchievementResponse.achievements[i].description,
                        lockedDescription = AchievementResponse.achievements[i].lockedDescription,
                        productId = AchievementResponse.achievements[i].productId,
                        achievementType = AchievementResponse.achievements[i].achievementType,
                        participationType = AchievementResponse.achievements[i].participationType,
                        timeWindow = AchievementResponse.achievements[i].timeWindow,
                        rewardsname = rewardnameplaceholder,
                        rewardsdescription = rewarddescriptionplaceholder,
                        rewardsvalue = rewardvalueplaceholder,
                        rewardstype = rewardtypeplaceholder,
                        rewardsmediaAsset = rewardmediaAssetplaceholder,
                        rewardsvalueType = rewardvalueTypeplaceholder,
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
                    if (achievement.rewardstype == StringConstants.Gamerscore)
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
                        IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                    });
                }
            }
            else
            {
                Unlockable = false;
                AchievementResponse = await _xboxRestAPI.Value.GetAchievementsFor360TitleAsync(HomeViewModel.XUIDOnly, TitleIDOverride);
                if (AchievementResponse?.achievements.Count == 0)
                {
                    _snackbarService.Show("Error: No Achievements", $"There were no achievements returned from the API", ControlAppearance.Danger,
                                               new SymbolIcon(SymbolRegular.ErrorCircle24), _snackbarDuration);
                    return;
                }
                //cut down version of the code to display minimal information about 360 achievements
                for (int i = 0; i < AchievementResponse?.achievements.Count; i++)
                {
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

                    Achievements.Add(new AchievementEntryResponse()
                    {
                        id = AchievementResponse.achievements[i].id,
                        serviceConfigId = "Null",
                        name = AchievementResponse.achievements[i].name,
                        progressState = "Null",
                        progressiontimeUnlocked = "0001-01-01T00:00:00.0000000Z",
                        isSecret = AchievementResponse.achievements[i].isSecret,
                        description = AchievementResponse.achievements[i].description,
                        rewardsname = "Null",
                        rewardsdescription = "Null",
                        rewardsvalue = AchievementResponse.achievements[i].gamerscore.ToString(),
                        rewardstype = "Gamerscore",
                        rewardsmediaAsset = "Null",
                        rewardsvalueType = rewardvalueTypeplaceholder,
                        raritycurrentCategory = AchievementResponse.achievements[i].rarity.currentCategory,
                        raritycurrentPercentage = AchievementResponse.achievements[i].rarity.currentPercentage
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
                        IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable
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
                        if (EventsData.Achievements.ContainsKey(achievement.ID) && achievement.ProgressState != StringConstants.Achieved)
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
                try
                {
                    await _xboxRestAPI.Value.UnlockTitleBasedAchievementAsync(AchievementResponse.achievements[0].serviceConfigId, AchievementResponse.achievements[0].titleAssociations[0].id, HomeViewModel.XUIDOnly, DGAchievements[AchievementIndex].ID.ToString(), HomeViewModel.Settings.FakeSignatureEnabled);

                    _snackbarService.Show("Achievement Unlocked", $"{DGAchievements[AchievementIndex].Name} has been unlocked",
                        ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackbarDuration);
                    DGAchievements[AchievementIndex].IsUnlockable = false;
                    DGAchievements[AchievementIndex].ProgressState = StringConstants.Achieved;
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

                // TODO: move this over to the rest api?
                var requestbody = File.ReadAllText(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments) + $"\\XAU\\Events\\{TitleIDOverride}.json");
                DateTime timestamp = DateTime.UtcNow;
                foreach (var i in EventsData.Achievements[DGAchievements[AchievementIndex].ID])
                {
                    var ReplacementData = i.Value;
                    switch (ReplacementData.ReplacementType)
                    {
                        case "Replace":
                            {
                                requestbody = requestbody.Replace(ReplacementData.Target, ReplacementData.Replacement);
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

        [RelayCommand]
        public async Task UnlockAll()
        {
            var lockedAchievementIds = Achievements.Where(o => o.progressState != StringConstants.Achieved).Select(o => o.id).ToList();
            try
            {
                await _xboxRestAPI.Value.UnlockAllTitleBasedAchievementAsync(serviceConfigId: AchievementResponse.achievements[0].serviceConfigId,
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
            await LoadGameInfo();
            await LoadAchievements();
            NewGame = false;
            if (SpooferEnabled)
                SpoofGame();
        }

        [RelayCommand]
        public async Task SearchAndFilterAchievements()
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
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        });
                    }
                    else
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == StringConstants.Gamerscore)
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
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        });
                    }
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
                        if (achievement.rewardstype == StringConstants.Gamerscore)
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
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        });
                    }
                    else
                    {
                        var gamerscore = 0;
                        if (achievement.rewardstype == StringConstants.Gamerscore)
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
                            IsUnlockable = achievement.progressState != StringConstants.Achieved && Unlockable && !IsEventBased
                        });
                    }
                }
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
            IsFiltered = true;
            await Task.CompletedTask;
        }
    }
}
