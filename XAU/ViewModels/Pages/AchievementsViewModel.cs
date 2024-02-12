using System.Collections.ObjectModel;
using System.Net.Http;
using System.Text;
using System.Windows.Data;
using Newtonsoft.Json.Linq;
using Wpf.Ui.Common;
using Wpf.Ui.Controls;
using XAU.Models;

namespace XAU.ViewModels.Pages;

public partial class AchievementsViewModel : ObservableObject, INavigationAware
{
    [ObservableProperty] 
    private bool _isInitialized;
    
    [ObservableProperty] 
    private string _titleIdOverride ="0";
    
    [ObservableProperty] 
    private bool _unlockAble;
    
    [ObservableProperty] 
    private bool _titleIdEnabled;
    
    [ObservableProperty] 
    private ObservableCollection<Achievement> _achievements = new();
    
    [ObservableProperty] 
    private ObservableCollection<DgAchievement> _dGAchievements = new();
    
    [ObservableProperty] 
    private string _gameInfo = "";
    
    [ObservableProperty] 
    private bool _isUnlockAllEnabled;

    private dynamic _achievementResponse = new JObject();
    private dynamic _gameInfoResponse = new JObject();
    private string _currentSystemLanguage = System.Globalization.CultureInfo.CurrentCulture.Name;
    
    private bool _isTitleIdValid;
    public static string TitleId { get; set; } = "0";
    public static bool NewGame { get; set; }
    public static bool IsSelectedGame360 { get; set; }
    public static bool SpoofingUpdate { get; set; }
    private static readonly bool SpooferEnabled = HomeViewModel.Settings.AutoSpooferEnabled;

    private static readonly HttpClientHandler Handler = new()
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
    };

    private readonly HttpClient _client = new(Handler);

    public AchievementsViewModel(ISnackbarService snackBarService)
    {
        _snackBarService = snackBarService;
    }

    private readonly ISnackbarService _snackBarService;
    private readonly TimeSpan _snackBarDuration = TimeSpan.FromSeconds(2);
    
    public void OnNavigatedTo()
    {
        if (SpooferEnabled)
        {
            switch (HomeViewModel.SpoofingStatus)
            {
                case 0 when GameInfo != "":
                {
                    SpoofGame();
                    break;
                }
                case HomeViewModel.SpooofingStatuses.Spoofing when GameInfo != "":
                {
                    GameInfo = HomeViewModel.SpoofedTitleId == TitleIdOverride
                        ? $"Manually Spoofing {_gameInfoResponse.titles[0].name.ToString()}"
                        : $"Not Spoofing {_gameInfoResponse.titles[0].name.ToString()} (Manually Spoofing a different game)";
                    break;
                }
                case HomeViewModel.SpooofingStatuses.AutoSpoofing:
                default:
                {
                    throw new ArgumentOutOfRangeException();
                }
            }
        }
            
        if (IsInitialized && NewGame)
        {
            RefreshAchievements();
        }

        if (TitleId != "0")
        {
            TitleIdOverride = TitleId;
            TitleId = "0";
        }
        
        if (HomeViewModel.InitComplete && TitleIdOverride == "0")
        {
            TitleIdEnabled = true;
        }

        if (IsInitialized || !HomeViewModel.InitComplete || TitleIdOverride == "0") return;
        InitializeViewModel();
    }

    public void OnNavigatedFrom() { }

    private void InitializeViewModel()
    {
        if (IsSelectedGame360)
        {
            UnlockAble = false;
        }

        LoadGameInfo();
        LoadAchievements();
        if (SpooferEnabled)
        {
            SpoofGame();
        }

        TitleIdEnabled = true;
        IsInitialized = true;
        NewGame = false;
        
        if (!HomeViewModel.Settings.RegionOverride) return;
        _currentSystemLanguage = "en-GB";
    }

    private async void LoadGameInfo()
    {
        if (TitleId != "0")
        {
            TitleIdOverride = TitleId;
            TitleId = "0";
        }
        
        GameInfo = "";
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        var requestBody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + TitleIdOverride + "\"]}");
        _gameInfoResponse = JObject.Parse(await _client
            .PostAsync(
                "https://titlehub.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly +
                ")/titles/batch/decoration/GamePass,Achievement,Stats", requestBody).Result.Content
            .ReadAsStringAsync());
        try
        {
            IsSelectedGame360 = _gameInfoResponse.titles[0].devices.ToString().Contains("Xbox360") || _gameInfoResponse.titles[0].devices.ToString().Contains("Mobile");
            GameInfo = _gameInfoResponse.titles[0].name.ToString();
            _isTitleIdValid = true;
        }
        catch
        {
            GameInfo = "Error";
            _isTitleIdValid = false;
        }
    }

    private async void SpoofGame()
    {
        if (HomeViewModel.SpoofingStatus == HomeViewModel.SpooofingStatuses.Spoofing)
        {
            GameInfo = HomeViewModel.SpoofedTitleId == TitleIdOverride
                ? $"Manually Spoofing {_gameInfoResponse.titles[0].name.ToString()}"
                : $"Not Spoofing {_gameInfoResponse.titles[0].name.ToString()} (Manually Spoofing a different game)";
            return;
        }
        
        HomeViewModel.AutoSpoofedTitleId = TitleIdOverride;
        HomeViewModel.SpoofingStatus = HomeViewModel.SpooofingStatuses.AutoSpoofing;
        GameInfo = $"Auto Spoofing {_gameInfoResponse.titles[0].name.ToString()}";
        await Task.Run(Spoofing);
        if (HomeViewModel.SpoofingStatus == HomeViewModel.SpooofingStatuses.Spoofing)
        {
            GameInfo = HomeViewModel.SpoofedTitleId == HomeViewModel.AutoSpoofedTitleId
                ? $"Manually Spoofing {_gameInfoResponse.titles[0].name.ToString()}"
                : $"Not Spoofing {_gameInfoResponse.titles[0].name.ToString()} (Manually Spoofing a different game)";
        }
        HomeViewModel.AutoSpoofedTitleId = "0";
    }

    private async Task Spoofing()
    {
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        var requestBody =
            new StringContent(
                "{\"titles\":[{\"expiration\":600,\"id\":" + HomeViewModel.AutoSpoofedTitleId +
                ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, "application/json");
        await _client.PostAsync(
            "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly + ")/devices/current",
            requestBody);
        var i = 0;
        Thread.Sleep(1000);
        SpoofingUpdate = false;
        while (!SpoofingUpdate)
        {
            if (i == 300)
            {
                _client.DefaultRequestHeaders.Clear();
                _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
                _client.DefaultRequestHeaders.Add("accept", "application/json");
                _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
                await _client.PostAsync(
                    "https://presence-heartbeat.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly +
                    ")/devices/current", requestBody);
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

    private void LoadAchievements()
    {
        Achievements.Clear();
        DGAchievements.Clear();
        
        if (!_isTitleIdValid)
        {
            return;
        }

        if (!IsSelectedGame360)
        {
            RetrieveAndProcessAchievements();
            return;
        }

        RetrieveAndProcessOtherAchievements();
    }

    private async void RetrieveAndProcessAchievements()
    {
        UnlockAble = true;
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "4");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _achievementResponse = JObject.Parse(await _client
            .GetAsync("https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly +
                      ")/achievements?titleId=" + TitleIdOverride + "&maxItems=1000").Result.Content
            .ReadAsStringAsync());
        try
        {
            if (_achievementResponse.achievements[0].progression.requirements.ToString().Length > 2)
            {
                UnlockAble = _achievementResponse.achievements[0].progression.requirements[0].id == "00000000-0000-0000-0000-000000000000";
            }
        }
        catch
        {
            _snackBarService.Show("Error: No Achievements", "There were no achievements returned from the API", ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }

        ProcessAchievements();
    }

    private void ProcessAchievements()
    {
        for (var i = 0; i < _achievementResponse.achievements.Count; i++)
        {
            //absolutely fucking dogwater event based check
            if (_achievementResponse.achievements[i].progression.requirements.ToString().Length > 2)
            {
                UnlockAble = _achievementResponse.achievements[i].progression.requirements[0].id ==
                             "00000000-0000-0000-0000-000000000000";
            }
            string rewardName;
            string rewardDescription;
            string rewardValue;
            string rewardType;
            string rewardsMediaAsset;
            string rewardsValueType;
            try
            {
                rewardName = _achievementResponse.achievements[i].rewards[0].name.ToString();
                rewardDescription = _achievementResponse.achievements[i].rewards[0].description.ToString();
                rewardValue = _achievementResponse.achievements[i].rewards[0].value.ToString();
                rewardType = _achievementResponse.achievements[i].rewards[0].type.ToString();
                rewardsMediaAsset = _achievementResponse.achievements[i].rewards[0].mediaAsset.ToString();
                rewardsValueType = _achievementResponse.achievements[i].rewards[0].valueType.ToString();
            }
            catch
            {
                rewardName = "N/A";
                rewardDescription = "N/A";
                rewardValue = "N/A";
                rewardType = "N/A";
                rewardsMediaAsset = "N/A";
                rewardsValueType = "N/A";
            }

            Achievements.Add(new Achievement
            {
                Id = _achievementResponse.achievements[i].id.ToString(),
                ServiceConfigId = _achievementResponse.achievements[i].serviceConfigId.ToString(),
                Name = _achievementResponse.achievements[i].name.ToString(),
                TitleAssociationsName = _achievementResponse.achievements[i].titleAssociations[0].name.ToString(),
                TitleAssociationsId = _achievementResponse.achievements[i].titleAssociations[0].id.ToString(),
                ProgressState = _achievementResponse.achievements[i].progressState.ToString(),
                //these are strings because im too lazy to handle them properly right now
                ProgressionRequirementsId = "AchievementResponse.achievements[i].progression.requirements[0].id.ToString()",
                ProgressionRequirementsCurrent = "AchievementResponse.achievements[i].progression.requirements[0].current.ToString()",
                ProgressionRequirementsTarget = "AchievementResponse.achievements[i].progression.requirements[0].target.ToString()",
                ProgressionRequirementsOperationType = "AchievementResponse.achievements[i].progression.requirements[0].operationType.ToString()",
                ProgressionRequirementsValueType = "AchievementResponse.achievements[i].progression.requirements[0].valueType.ToString()",
                ProgressionRequirementsRuleParticipationType = "AchievementResponse.achievements[i].progression.requirements[0].ruleParticipationType.ToString()",
                ProgressionTimeUnlocked = _achievementResponse.achievements[i].progression.timeUnlocked.ToString(),
                MediaAssetsName = _achievementResponse.achievements[i].mediaAssets[0].name.ToString(),
                MediaAssetsType = _achievementResponse.achievements[i].mediaAssets[0].type.ToString(),
                MediaAssetsUrl = _achievementResponse.achievements[i].mediaAssets[0].url.ToString(),
                Platforms = _achievementResponse.achievements[i].platforms.ToObject<List<string>>(),
                IsSecret = _achievementResponse.achievements[i].isSecret.ToString(),
                Description = _achievementResponse.achievements[i].description.ToString(),
                LockedDescription = _achievementResponse.achievements[i].lockedDescription.ToString(),
                ProductId = _achievementResponse.achievements[i].productId.ToString(),
                AchievementType = _achievementResponse.achievements[i].achievementType.ToString(),
                ParticipationType = _achievementResponse.achievements[i].participationType.ToString(),
                TimeWindow = _achievementResponse.achievements[i].timeWindow.ToString(),
                RewardsName = rewardName,
                RewardsDescription = rewardDescription,
                RewardsValue = rewardValue,
                RewardsType = rewardType,
                RewardsMediaAsset = rewardsMediaAsset,
                RewardsValueType = rewardsValueType,
                EstimatedTime = _achievementResponse.achievements[i].estimatedTime.ToString(),
                DeepLink = _achievementResponse.achievements[i].deeplink.ToString(),
                IsRevoked = _achievementResponse.achievements[i].isRevoked.ToString(),
                RarityCurrentCategory = _achievementResponse.achievements[i].rarity.currentCategory.ToString(),
                RarityCurrentPercentage = _achievementResponse.achievements[i].rarity.currentPercentage.ToString()
            });
        }
        foreach (var achievement in Achievements)
        {
            var gamerScore = 0;
            if (achievement.RewardsType == "Gamerscore")
            {
                gamerScore = int.Parse(achievement.RewardsValue!);
            }
            
            DGAchievements.Add(new DgAchievement
            {
                Index = Achievements.IndexOf(achievement),
                Id = int.Parse(achievement.Id!),
                Name = achievement.Name,
                Description = achievement.Description,
                IsSecret = achievement.IsSecret,
                DateUnlocked = DateTime.Parse(achievement.ProgressionTimeUnlocked!),
                GamerScore = gamerScore,
                RarityPercentage = float.Parse(achievement.RarityCurrentPercentage!),
                RarityCategory = achievement.RarityCurrentCategory,
                ProgressState = achievement.ProgressState,
                IsUnlockAble = achievement.ProgressState != "Achieved" && UnlockAble
            });
        }
    }

    private async void RetrieveAndProcessOtherAchievements()
    {
        UnlockAble = false;
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _achievementResponse = JObject.Parse(await _client.GetAsync("https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly + ")/titleachievements?titleId=" + TitleIdOverride + "&maxItems=1000").Result.Content.ReadAsStringAsync());
        if (_achievementResponse.achievements.Count == 0)
        {
            _snackBarService.Show("Error: No Achievements", $"There were no achievements returned from the API", ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
            return;
        }

        ProcessOtherAchievements();
    }

    private void ProcessOtherAchievements()
    {
        //cut down version of the code to display minimal information about 360 achievements
        for (var i = 0; i < _achievementResponse.achievements.Count; i++)
        {
            string rewardValueType;
            try
            {
                rewardValueType = _achievementResponse.achievements[i].rewards[0].valueType.ToString();
            }
            catch
            {
                rewardValueType = "N/A";
            }

            Achievements.Add(new Achievement
            {
                Id = _achievementResponse.achievements[i].id.ToString(),
                ServiceConfigId = "Null",
                Name = _achievementResponse.achievements[i].name.ToString(),
                ProgressState = "Null",
                ProgressionTimeUnlocked = "0001-01-01T00:00:00.0000000Z",
                IsSecret = _achievementResponse.achievements[i].isSecret.ToString(),
                Description = _achievementResponse.achievements[i].description.ToString(),
                RewardsName = "Null",
                RewardsDescription = "Null",
                RewardsValue = _achievementResponse.achievements[i].gamerscore.ToString(),
                RewardsType = "Gamerscore",
                RewardsMediaAsset = "Null",
                RewardsValueType = rewardValueType,
                RarityCurrentCategory = _achievementResponse.achievements[i].rarity.currentCategory.ToString(),
                RarityCurrentPercentage = _achievementResponse.achievements[i].rarity.currentPercentage.ToString()
            });
        }
        foreach (var achievement in Achievements)
        {
            var gamerScore = 0;
            if (achievement.RewardsType == "Gamerscore")
            {
                gamerScore = int.Parse(achievement.RewardsValue!);
            }
            DGAchievements.Add(new DgAchievement
            {
                Index = Achievements.IndexOf(achievement),
                Id = int.Parse(achievement.Id!),
                Name = achievement.Name,
                Description = achievement.Description,
                IsSecret = achievement.IsSecret,
                DateUnlocked = DateTime.Parse(achievement.ProgressionTimeUnlocked!),
                GamerScore = gamerScore,
                RarityPercentage = float.Parse(achievement.RarityCurrentPercentage!),
                RarityCategory = achievement.RarityCurrentCategory,
                ProgressState = achievement.ProgressState,
                IsUnlockAble = achievement.ProgressState != "Achieved" && UnlockAble
            });
        }

        if (IsSelectedGame360)
        {
            _snackBarService.Show("Warning: Unsupported Game", "This tool does not support Xbox 360 titles", ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Warning24), _snackBarDuration);
        }
        else if (!UnlockAble)
        {
            _snackBarService.Show("Warning: Unsupported Game", "This tool does not support Event Based titles", ControlAppearance.Caution,
                new SymbolIcon(SymbolRegular.Warning24), _snackBarDuration);
        }

        IsUnlockAllEnabled = HomeViewModel.Settings.UnlockAllEnabled && UnlockAble is true and true;
    }
    
    public async void UnlockAchievement(int achievementIndex)
    {
        var requestBody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + _achievementResponse.achievements[0].serviceConfigId + "\",\"titleId\":\"" + _achievementResponse.achievements[0].titleAssociations[0].id + "\",\"userId\":\"" + HomeViewModel.XUidOnly + "\",\"achievements\":[{\"id\":\"" + DGAchievements[achievementIndex].Id + "\",\"percentComplete\":\"100\"}]}";
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
        if (HomeViewModel.Settings.FakeSignatureEnabled)
            _client.DefaultRequestHeaders.Add("Signature", "RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==");
        var convertedBody = new StringContent(requestBody, Encoding.UTF8, "application/json");
        try
        {
            await _client.PostAsync(
                "https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly + ")/achievements/" +
                _achievementResponse.achievements[0].serviceConfigId + "/update", convertedBody);
            _snackBarService.Show("Achievement Unlocked", $"{DGAchievements[achievementIndex].Name} has been unlocked",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackBarDuration);
            DGAchievements[achievementIndex].IsUnlockAble = false;
            DGAchievements[achievementIndex].ProgressState = "Achieved";
            DGAchievements[achievementIndex].DateUnlocked = DateTime.Now;
            CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
        }
        catch (HttpRequestException)
        {
            _snackBarService.Show("Error: Achievement Not Unlocked",
                $"{DGAchievements[achievementIndex].Name} was not unlocked", ControlAppearance.Danger,
                new SymbolIcon(SymbolRegular.ErrorCircle24), _snackBarDuration);
        }
    }

    [RelayCommand]
    private async Task UnlockAll()
    {
        var requestBody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" +
                          _achievementResponse.achievements[0].serviceConfigId + "\",\"titleId\":\"" +
                          _achievementResponse.achievements[0].titleAssociations[0].id + "\",\"userId\":\"" +
                          HomeViewModel.XUidOnly + "\",\"achievements\":[";
        _client.DefaultRequestHeaders.Clear();
        _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
        _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
        _client.DefaultRequestHeaders.Add("accept", "application/json");
        _client.DefaultRequestHeaders.Add("accept-language", _currentSystemLanguage);
        _client.DefaultRequestHeaders.Add("Authorization", HomeViewModel.XAuth);
        _client.DefaultRequestHeaders.Add("Host", "achievements.xboxlive.com");
        _client.DefaultRequestHeaders.Add("Connection", "Keep-Alive");
        _client.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
        
        if (HomeViewModel.Settings.FakeSignatureEnabled)
        {
            _client.DefaultRequestHeaders.Add("Signature", "RGFtbklHb3R0YU1ha2VUaGlzU3RyaW5nU3VwZXJMb25nSHVoLkRvbnRFdmVuS25vd1doYXRTaG91bGRCZUhlcmVEcmFmZlN0cmluZw==");
        }

        foreach (var achievement in Achievements)
        {
            if (achievement.ProgressState == "Achieved") continue;
            requestBody += "{\"id\":\"" +achievement.Id + "\",\"percentComplete\":\"100\"},";
        }
        
        requestBody = requestBody.Remove(requestBody.Length - 1)+ "]}";
        var convertedBody = new StringContent(requestBody, Encoding.UTF8, "application/json");
        try
        {
            await _client.PostAsync(
                "https://achievements.xboxlive.com/users/xuid(" + HomeViewModel.XUidOnly + ")/achievements/" +
                _achievementResponse.achievements[0].serviceConfigId + "/update", convertedBody);
            _snackBarService.Show("All Achievements Unlocked", "All Achievements for this game have been unlocked",
                ControlAppearance.Success, new SymbolIcon(SymbolRegular.Checkmark24), _snackBarDuration);
            
            foreach (var achievement in DGAchievements)
            {
                if (achievement.ProgressState == "Achieved") continue;
                achievement.IsUnlockAble = false;
                achievement.ProgressState = "Achieved";
                achievement.DateUnlocked = DateTime.Now;
            }
            CollectionViewSource.GetDefaultView(DGAchievements).Refresh();
        }
        catch 
        {
            // ignored
        }
    }

    [RelayCommand]
    private void RefreshAchievements()
    {
        LoadGameInfo();
        LoadAchievements();
        NewGame = false;
        if (!SpooferEnabled) return;
        SpoofGame();
    }
}