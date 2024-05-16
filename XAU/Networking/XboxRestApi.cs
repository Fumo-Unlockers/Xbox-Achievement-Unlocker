using System.Net;
using System.Net.Http;
using System.Text;
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
}