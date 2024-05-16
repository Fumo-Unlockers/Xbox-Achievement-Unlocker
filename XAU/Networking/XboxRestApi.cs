using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class XboxRestAPI
{
    private readonly HttpClient _httpClient;

    private readonly HttpClient _httpClient2; // Dumb, but needed for events for now

    // User specifics
    private readonly string _xauth;
    private readonly string _currentSystemLanguage;

    public XboxRestAPI(string xauth, string currentSystemLanguage)
    {
        _xauth = xauth;
        _currentSystemLanguage = currentSystemLanguage;
        // This is a placeholder for the Xbox REST API
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);

        var insecureEventsHandler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate,
            //This is an absolutely terrible idea but the stupid fucking events API just cries about SSL errors
            ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
        };
        _httpClient2 = new HttpClient(insecureEventsHandler);
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _currentSystemLanguage);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
    }

    private void SetDefaultEventBasedHeaders()
    {
        _httpClient2.DefaultRequestHeaders.Clear();
        _httpClient2.DefaultRequestHeaders.Add("user-agent", "MSDW");
        _httpClient2.DefaultRequestHeaders.Add("cache-control", "no-cache");
        _httpClient2.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _httpClient2.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient2.DefaultRequestHeaders.Add("reliability-mode", "standard");
        _httpClient2.DefaultRequestHeaders.Add("client-version", "EUTC-Windows-C++-no-10.0.22621.3296.amd64fre.ni_release.220506-1250-no");
        _httpClient2.DefaultRequestHeaders.Add("apikey", "0890af88a9ed4cc886a14f5e174a2827-9de66c5e-f867-43a8-a7b8-e0ddd481cca4-7548,95c1f21d6cb047a09e7b423c1cb2222e-9965f07b-54fa-498e-9727-9e8d24dec39e-7027");
        _httpClient2.DefaultRequestHeaders.Add("Client-Id", "NO_AUTH");
        _httpClient2.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Telemetry);
        _httpClient2.DefaultRequestHeaders.Add(HeaderNames.Connection, "close");
        ;
        var authxtoken = Regex.Replace(_xauth, @"XBL3\.0 x=\d+;", "XBL3.0 x=-;");
        _httpClient2.DefaultRequestHeaders.Add("authxtoken", authxtoken);

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

        var responseString = await _httpClient.GetStringAsync(
            $"https://peoplehub.xboxlive.com/users/me/people/xuids({xuid})/decoration/detail,preferredColor,presenceDetail,multiplayerSummary");
        return JsonConvert.DeserializeObject<Profile>(responseString);
    }

    public async Task<GameTitle?> GetGameTitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);

        // TODO: request as a model
        StringContent requestbody = new StringContent("{\"pfns\":null,\"titleIds\":[\"" + titleId + "\"]}");
        var gameTitleResponse = await _httpClient.PostAsync("https://titlehub.xboxlive.com/users/xuid(" + xuid + ")/titles/batch/decoration/GamePass,Achievement,Stats", requestbody).Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameTitle>(gameTitleResponse);
    }

    public async Task<Gamepass?> GetGamepassMembershipAsync(string xuid)
    {
        SetDefaultHeaders();
        var gpuResponse = await _httpClient.GetAsync("https://xgrant.xboxlive.com/users/xuid(" + xuid + ")/programInfo?filter=profile,activities,catalog").Result.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Gamepass>(gpuResponse);
    }

    public async Task<TitlesList?> GetGamesListAsync(string xuid)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.TitleHub);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var responseString = await _httpClient.GetStringAsync("https://titlehub.xboxlive.com/users/xuid(" + xuid + ")/titles/titleHistory/decoration/Achievement,scid?maxItems=10000");
        return JsonConvert.DeserializeObject<TitlesList>(responseString);
    }

    public async Task<GameStats?> GetGameStatsAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);

        StringContent requestbody = new StringContent($"{{\"arrangebyfield\":\"xuid\",\"xuids\":[\"{xuid}\"],\"stats\":[{{\"name\":\"MinutesPlayed\",\"titleId\":\"{titleId}\"}}]}}");

        var response = await _httpClient
                .PostAsync("https://userstats.xboxlive.com/batch", requestbody).Result.Content
                .ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameStats>(response);
    }

    public async Task SendHeartbeatAsync(string xuid, string spoofedTitleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        var requestbody =
            new StringContent(
                "{\"titles\":[{\"expiration\":600,\"id\":" + spoofedTitleId +
                ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}", encoding: Encoding.UTF8, HeaderValues.Accept);
        await _httpClient.PostAsync(
        "https://presence-heartbeat.xboxlive.com/users/xuid(" + xuid + ")/devices/current",
        requestbody);
    }

    public async Task StopHeartbeatAsync(string xuid)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        await _httpClient.DeleteAsync("https://presence-heartbeat.xboxlive.com/users/xuid(" + xuid + ")/devices/current");
    }

    // TODO actual typing
    public async Task<dynamic> GetAchievementsForTitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion4);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var response = (dynamic)JObject.Parse(await _httpClient.GetAsync("https://achievements.xboxlive.com/users/xuid(" + xuid + ")/achievements?titleId=" + titleId + "&maxItems=1000").Result.Content.ReadAsStringAsync());
        return response;
    }

    public async Task<dynamic> GetAchievementsFor360TitleAsync(string xuid, string titleId)
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var response = (dynamic)JObject.Parse(await _httpClient.GetAsync("https://achievements.xboxlive.com/users/xuid(" + xuid + ")/achievements?titleId=" + titleId + "&maxItems=1000").Result.Content.ReadAsStringAsync());
        return response;
    }

    public async Task UnlockTitleBasedAchievementAsync(string serviceConfigId, string titleId, string xuid, string achievementId, bool useFakeSignature = false)
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

        var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + serviceConfigId + "\",\"titleId\":\"" + titleId + "\",\"userId\":\"" + xuid + "\",\"achievements\":[{\"id\":\"" + achievementId + "\",\"percentComplete\":\"100\"}]}";
        var bodyconverted = new StringContent(requestbody, Encoding.UTF8, HeaderValues.Accept);

        await _httpClient.PostAsync(
                                "https://achievements.xboxlive.com/users/xuid(" + xuid + ")/achievements/" +
                                serviceConfigId + "/update", bodyconverted);

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

        var requestbody = "{\"action\":\"progressUpdate\",\"serviceConfigId\":\"" + serviceConfigId + "\",\"titleId\":\"" + titleId + "\",\"userId\":\"" + xuid + "\",\"achievements\":[";

        // TODO: eventually reduce 2n loops (since we do this in the caller too)
        foreach (string id in achievementIds)
        {
            requestbody += "{\"id\":\"" + id + "\",\"percentComplete\":\"100\"},";
        }

        requestbody = requestbody.Remove(requestbody.Length - 1) + "]}";
        var bodyconverted = new StringContent(requestbody, Encoding.UTF8, HeaderValues.Accept);

        await _httpClient.PostAsync(
                                "https://achievements.xboxlive.com/users/xuid(" + xuid + ")/achievements/" +
                                serviceConfigId + "/update", bodyconverted);

    }

    // TODO: see if we can handle the actual request body building
    public async Task UnlockEventBasedAchievement(string eventsToken, StringContent requestBody)
    {
        SetDefaultEventBasedHeaders();
        _httpClient2.DefaultRequestHeaders.Add("tickets", $"\"1\"=\"{eventsToken}\"");
        await _httpClient2.PostAsync(@"https://v20.events.data.microsoft.com/OneCollector/1.0/", requestBody);
    }

    // TODO: don't return dynamic....
    public async Task<dynamic> GetTitleIdsFromGamePass(string prodId)
    {
        SetDefaultHeaders();
        var productIdsConverted = new StringContent("{\"Products\":[\"" + prodId + "\"]}");
        var titleIDsResponse = await _httpClient.PostAsync(
                    "https://catalog.gamepass.com/products?market=GB&language=en-GB&hydration=PCHome",
                    productIdsConverted).Result.Content.ReadAsStringAsync();
        return titleIDsResponse;
    }
}
