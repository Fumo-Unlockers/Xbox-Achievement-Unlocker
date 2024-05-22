using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class XboxRestAPI
{
    private readonly HttpClient _httpClient;

    private readonly HttpClient _eventBasedClient; // Dumb, but needed for events for now

    private readonly HttpClient _spooferClient;

    // User specifics
    private readonly string _xauth;
    private readonly string _currentSystemLanguage;

    public XboxRestAPI(string xauth, string currentSystemLanguage)
    {
        _xauth = xauth;
        _currentSystemLanguage = currentSystemLanguage;
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
        _spooferClient = new HttpClient(handler);

        var insecureEventsHandler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            //This is an absolutely terrible idea but the stupid fucking events API just cries about SSL errors
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _eventBasedClient = new HttpClient(insecureEventsHandler);
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _currentSystemLanguage);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
    }

    private void SetDefaultSpooferHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _currentSystemLanguage);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
    }


    private void SetDefaultEventBasedHeaders()
    {
        _eventBasedClient.DefaultRequestHeaders.Clear();
        _eventBasedClient.DefaultRequestHeaders.Add("user-agent", "MSDW");
        _eventBasedClient.DefaultRequestHeaders.Add("cache-control", "no-cache");
        _eventBasedClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _eventBasedClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _eventBasedClient.DefaultRequestHeaders.Add("reliability-mode", "standard");
        _eventBasedClient.DefaultRequestHeaders.Add("client-version", "EUTC-Windows-C++-no-10.0.22621.3296.amd64fre.ni_release.220506-1250-no");
        _eventBasedClient.DefaultRequestHeaders.Add("apikey", "0890af88a9ed4cc886a14f5e174a2827-9de66c5e-f867-43a8-a7b8-e0ddd481cca4-7548,95c1f21d6cb047a09e7b423c1cb2222e-9965f07b-54fa-498e-9727-9e8d24dec39e-7027");
        _eventBasedClient.DefaultRequestHeaders.Add("Client-Id", "NO_AUTH");
        _eventBasedClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Telemetry);
        _eventBasedClient.DefaultRequestHeaders.Add(HeaderNames.Connection, "close");
        ;
        var authxtoken = Regex.Replace(_xauth, @"XBL3\.0 x=\d+;", "XBL3.0 x=-;");
        _eventBasedClient.DefaultRequestHeaders.Add("authxtoken", authxtoken);

    }

    public async Task<BasicProfile?> GetBasicProfileAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Profile);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var response = await _httpClient.GetStringAsync(BasicXboxAPIUris.GamertagUrl);
        return JsonConvert.DeserializeObject<BasicProfile>(response);
    }

    public async Task<Profile?> GetProfileAsync(string xuid)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion5);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.PeopleHub);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var responseString = await _httpClient.GetStringAsync(string.Format(InterpolatedXboxAPIUrls.ProfileUrl, xuid));
        return JsonConvert.DeserializeObject<Profile>(responseString);
    }

    public async Task<GameTitle?> GetGameTitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        var gameTitleRequest = new GameTitleRequest()
        {
            Pfns = null,
            TitleIds = new List<string>() { titleId }
        };
        var gameTitleResponse = await _httpClient.PostAsync(string.Format(InterpolatedXboxAPIUrls.TitleUrl, xuid), new StringContent(JsonConvert.SerializeObject(gameTitleRequest), Encoding.UTF8, HeaderValues.Accept)).Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameTitle>(gameTitleResponse);
    }

    public async Task<Gamepass?> GetGamepassMembershipAsync(string xuid)
    {
        SetDefaultHeaders();
        var gpuResponse = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.GamepassMembershipUrl, xuid)).Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Gamepass>(gpuResponse);
    }

    public async Task<TitlesList?> GetGamesListAsync(string xuid)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.TitleHub);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var responseString = await _httpClient.GetStringAsync(string.Format(InterpolatedXboxAPIUrls.TitlesUrl, xuid));
        return JsonConvert.DeserializeObject<TitlesList>(responseString);
    }

    public async Task<GameStatsResponse?> GetGameStatsAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);

        var stat = new GameStat()
        {
            TitleId = titleId
        };
        var gameStatsRequest = new GameStatsRequest()
        {
            Xuids = new List<string>() { xuid },
            Stats = new List<GameStat>() { stat }
        };
        var response = await _httpClient
                .PostAsync(BasicXboxAPIUris.UserStatsUrl, new StringContent(JsonConvert.SerializeObject(gameStatsRequest), Encoding.UTF8, HeaderValues.Accept)).Result.Content
                .ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameStatsResponse>(response);
    }

    public async Task SendHeartbeatAsync(string xuid, string spoofedTitleId)
    {
        SetDefaultSpooferHeaders();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        var heartbeatRequest = new HeartbeatRequest()
        {
            titles = new List<TitleRequest>()
            {
                new TitleRequest()
                {
                    id = spoofedTitleId
                }
            }
        };
        await _spooferClient.PostAsync(
        string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid),
        new StringContent(JsonConvert.SerializeObject(heartbeatRequest), Encoding.UTF8, HeaderValues.Accept));
    }

    public async Task StopHeartbeatAsync(string xuid)
    {
        SetDefaultSpooferHeaders();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        await _httpClient.DeleteAsync(string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid));
    }

    public async Task<AchievementsResponse?> GetAchievementsForTitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion4);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

        var response = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.QueryAchievementsUrl, xuid, titleId)).Result.Content.ReadAsStringAsync();
        var achievements = JsonConvert.DeserializeObject<AchievementsResponse>(response);
        return achievements;
    }

    public async Task<Xbox360AchievementResponse?> GetAchievementsFor360TitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var response = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.QueryAchievements360Url, xuid, titleId)).Result.Content.ReadAsStringAsync();
        var achievements = JsonConvert.DeserializeObject<Xbox360AchievementResponse>(response);
        return achievements;
    }

    public async Task UnlockTitleBasedAchievementAsync(string serviceConfigId, string titleId, string xuid, string achievementId, bool useFakeSignature = false)
    {
        await UnlockAllTitleBasedAchievementAsync(serviceConfigId, titleId, xuid, new List<string>() { achievementId }, useFakeSignature);
    }

    public async Task UnlockAllTitleBasedAchievementAsync(string serviceConfigId, string titleId, string xuid, List<string> achievementIds, bool useFakeSignature = false)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");

        if (useFakeSignature)
        {
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Signature, HeaderValues.Signature);
        }

        // Split the requests into 50 achievements each. Anything over 100 seems to BadRequest. TODO: look into
        // headers and see if we can send long data or w/e
        const int chunkSize = 50;
        for (int i = 0; i < achievementIds.Count; i += chunkSize)
        {
            var chunk = achievementIds.Skip(i).Take(chunkSize).ToList();

            var unlockRequest = new UnlockTitleBasedAchievementRequest
            {
                titleId = titleId,
                serviceConfigId = serviceConfigId,
                userId = xuid,
                achievements = chunk.Select(id => new AchievementsArrayEntry { id = id, percentComplete = "100" }).ToList()
            };

            var unlockBodyStr = JsonConvert.SerializeObject(unlockRequest);
            var bodyconverted = new StringContent(unlockBodyStr, Encoding.UTF8, HeaderValues.Accept);

            var response = await _httpClient.PostAsync(
                string.Format(InterpolatedXboxAPIUrls.UpdateAchievementsUrl, xuid, serviceConfigId), bodyconverted);
            if (response.StatusCode != HttpStatusCode.OK)
            {
                throw new HttpRequestException($"Failed to unlock achievement(s) for title {titleId} with status code {response.StatusCode}");
            }
        }
    }

    // TODO: see if we can handle the actual request body building
    public async Task UnlockEventBasedAchievement(string eventsToken, StringContent requestBody)
    {
        SetDefaultEventBasedHeaders();
        _eventBasedClient.DefaultRequestHeaders.Add("tickets", $"\"1\"=\"{eventsToken}\"");
        await _eventBasedClient.PostAsync(BasicXboxAPIUris.TelemetryUrl, requestBody);
    }

    public async Task<GamePassProducts?> GetTitleIdsFromGamePass(string prodId)
    {
        SetDefaultHeaders();
        GamepassProductsRequest gamepassProducts = new GamepassProductsRequest()
        {
            Products = new List<string>() { prodId }
        };
        var titleIDsResponse = await _httpClient.PostAsync(
                    BasicXboxAPIUris.GamepassCatalogUrl,
                    new StringContent(JsonConvert.SerializeObject(gamepassProducts))).Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GamePassProducts>(titleIDsResponse);
    }
}
