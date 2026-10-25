using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;

public class XboxRestAPI
{
    private readonly HttpClient _httpClient;

    public string? LastAchievementsRequestUrl { get; private set; }
    public string? LastAchievementsResponseJson { get; private set; }

    private readonly HttpClient _eventBasedClient; // Dumb, but needed for events for now

    private readonly HttpClient _spooferClient;

    // User specifics
    private readonly string _xauth;
    private readonly string _requestedResponseLanguage;

    public XboxRestAPI(string xauth)
    {
        _xauth = xauth;
        _requestedResponseLanguage = HomeViewModel.Settings.RegionOverride ? "en-GB" : System.Globalization.CultureInfo.CurrentCulture.Name;
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
        _spooferClient = new HttpClient(handler);

        var eventsHandler = new HttpClientHandler()
        {
            AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
        };
        _eventBasedClient = new HttpClient(eventsHandler);
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _requestedResponseLanguage);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);


#if DEBUG
        Console.WriteLine("Headers in _httpClient:");
        foreach (var header in _httpClient.DefaultRequestHeaders)
        {
            if (header.Key == "Authorization") continue;
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
#endif
    }

    private void SetDefaultSpooferHeaders()
    {
        _spooferClient.DefaultRequestHeaders.Clear();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _requestedResponseLanguage);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);

#if DEBUG
        Console.WriteLine("Headers in _spooferClient:");
        foreach (var header in _spooferClient.DefaultRequestHeaders)
        {
            if (header.Key == "Authorization") continue;
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
#endif
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

#if DEBUG
        Console.WriteLine("Headers in _eventBasedClient:");
        foreach (var header in _eventBasedClient.DefaultRequestHeaders)
        {
            if (header.Key == "authxtoken") continue;
            Console.WriteLine($"{header.Key}: {string.Join(", ", header.Value)}");
        }
#endif
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
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(titleId))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        var gameTitleRequest = new GameTitleRequest()
        {
            Pfns = null,
            TitleIds = new List<string>() { titleId }
        };

        var gameTitleHttpResponse = await _httpClient.PostAsync(string.Format(InterpolatedXboxAPIUrls.TitleUrl, xuid), new StringContent(JsonConvert.SerializeObject(gameTitleRequest), Encoding.UTF8, HeaderValues.Accept));
        var gameTitleResponse = await gameTitleHttpResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameTitle>(gameTitleResponse);
    }

    public async Task<Gamepass?> GetGamepassMembershipAsync(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        SetDefaultHeaders();
        var gpuHttpResponse = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.GamepassMembershipUrl, xuid));
        var gpuResponse = await gpuHttpResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<Gamepass>(gpuResponse);
    }

    public async Task<TitlesList?> GetGamesListAsync(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.TitleHub);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var responseString = await _httpClient.GetStringAsync(string.Format(InterpolatedXboxAPIUrls.TitlesUrl, xuid));
        return JsonConvert.DeserializeObject<TitlesList>(responseString);
    }

    public async Task<JObject?> GetGamertagProfileAsync(string gamertag)
    {
        if (string.IsNullOrWhiteSpace(gamertag))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Profile);

        string url = string.Format(InterpolatedXboxAPIUrls.GamertagSearch, gamertag);
        var response = await _httpClient.GetAsync(url);
        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JObject.Parse(jsonResponse);
    }

    public Task<GameStatsResponse?> GetGameStatsAsync(string xuid, string titleId)
    {
        return GetGameStatsAsync(xuid, titleId, new[] { "MinutesPlayed" });
    }

    public async Task<GameStatsResponse?> GetGameStatsAsync(string xuid, string titleId, IEnumerable<string> statNames)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(titleId))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        var requestedStats = statNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Select(name => new GameStat { Name = name, TitleId = titleId })
            .ToList();
        if (requestedStats.Count == 0)
            return null;

        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion2);

        var gameStatsRequest = new GameStatsRequest()
        {
            Xuids = new List<string>() { xuid },
            Stats = requestedStats
        };
#if DEBUG
        System.Diagnostics.Debug.WriteLine($"[UserStats] Request URL: POST {BasicXboxAPIUris.UserStatsUrl}");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Request titleId={titleId}, statNames=[{string.Join(", ", requestedStats.Select(stat => stat.Name))}]");
#endif
        var httpResponse = await _httpClient
                .PostAsync(BasicXboxAPIUris.UserStatsUrl, new StringContent(JsonConvert.SerializeObject(gameStatsRequest), Encoding.UTF8, HeaderValues.Accept));
        var response = await httpResponse.Content.ReadAsStringAsync();
        var gameStats = JsonConvert.DeserializeObject<GameStatsResponse>(response);
#if DEBUG
        LogGameStatsResponse(httpResponse, response, gameStats);
#endif
        return gameStats;
    }

    public async Task<List<Stat>> GetGameStatsByScidAsync(
        string xuid,
        string titleId,
        string scid,
        IEnumerable<string> statNames,
        bool includeValueMetadata)
    {
        if (string.IsNullOrWhiteSpace(xuid) ||
            string.IsNullOrWhiteSpace(titleId) ||
            string.IsNullOrWhiteSpace(scid))
            return [];

        var requestedNames = statNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (requestedNames.Count == 0)
            return [];

        var namesPath = string.Join(",", requestedNames.Select(Uri.EscapeDataString));
        var requestUrl =
            $"https://userstats.xboxlive.com/users/xuid({Uri.EscapeDataString(xuid)})/scids/{Uri.EscapeDataString(scid)}/stats/{namesPath}" +
            (includeValueMetadata ? "?include=valuemetadata" : "");

        SetDefaultHeaders();
        if (includeValueMetadata)
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);

#if DEBUG
        var sanitizedUrl =
            $"https://userstats.xboxlive.com/users/xuid({{xuid}})/scids/{scid}/stats/{namesPath}" +
            (includeValueMetadata ? "?include=valuemetadata" : "");
        System.Diagnostics.Debug.WriteLine($"[UserStats SCID] Request URL: GET {sanitizedUrl}");
#endif

        var httpResponse = await _httpClient.GetAsync(requestUrl);
        var response = await httpResponse.Content.ReadAsStringAsync();
        if (httpResponse.StatusCode == HttpStatusCode.NotFound)
            return [];
        httpResponse.EnsureSuccessStatusCode();
        var root = JObject.Parse(response);
        var stats = root["user"]?["stats"]?
            .Children<JObject>()
            .Select(stat => new Stat
            {
                Scid = scid,
                TitleId = titleId,
                Name = stat["statname"]?.ToString(),
                Type = stat["type"]?.ToString(),
                Value = stat["value"]?.ToString(),
                ValueMetadata = stat["valuemetadata"]?.ToString()
            })
            .ToList() ?? [];

#if DEBUG
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats SCID] Response: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}, " +
            $"structure=user:Object,user.stats:Array,stats={stats.Count}");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats SCID] Returned stat names ({stats.Count}): [{string.Join(", ", stats.Select(stat => stat.Name ?? "<unnamed>").Distinct(StringComparer.OrdinalIgnoreCase))}]");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats SCID] Value metadata present: {stats.Count(stat => !string.IsNullOrWhiteSpace(stat.ValueMetadata))}/{stats.Count}");
#endif
        return stats;
    }

#if DEBUG
    private static void LogGameStatsResponse(
        HttpResponseMessage httpResponse,
        string response,
        GameStatsResponse? gameStats)
    {
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Response: {(int)httpResponse.StatusCode} {httpResponse.StatusCode}, " +
            $"contentType={httpResponse.Content.Headers.ContentType?.MediaType ?? "<none>"}");

        try
        {
            var root = JObject.Parse(response);
            System.Diagnostics.Debug.WriteLine(
                $"[UserStats] Response top-level structure: {string.Join(", ", root.Properties().Select(property => $"{property.Name}:{property.Value.Type}"))}");
        }
        catch (JsonException ex)
        {
            System.Diagnostics.Debug.WriteLine($"[UserStats] Response structure unavailable: {ex.Message}");
        }

        var collections = gameStats?.StatListsCollection ?? [];
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Response collections: groups={gameStats?.Groups.Count ?? 0}, statListsCollection={collections.Count}");
        for (var index = 0; index < collections.Count; index++)
        {
            var collection = collections[index];
            System.Diagnostics.Debug.WriteLine(
                $"[UserStats] Collection[{index}]: arrangeByField={collection.ArrangeByField ?? "<none>"}, " +
                $"arrangeByFieldIdPresent={!string.IsNullOrWhiteSpace(collection.ArrangeByFieldId)}, stats={collection.Stats.Count}");
        }

        var stats = collections.SelectMany(collection => collection.Stats).ToList();
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Returned stat names ({stats.Count}): [{string.Join(", ", stats.Select(stat => stat.Name ?? "<unnamed>").Distinct(StringComparer.OrdinalIgnoreCase))}]");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Returned title IDs: [{string.Join(", ", stats.Select(stat => stat.TitleId).Where(id => !string.IsNullOrWhiteSpace(id)).Distinct())}]");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Returned SCIDs: [{string.Join(", ", stats.Select(stat => stat.Scid).Where(scid => !string.IsNullOrWhiteSpace(scid)).Distinct(StringComparer.OrdinalIgnoreCase))}]");
        System.Diagnostics.Debug.WriteLine(
            $"[UserStats] Returned stat types: [{string.Join(", ", stats.Select(stat => stat.Type).Where(type => !string.IsNullOrWhiteSpace(type)).Distinct(StringComparer.OrdinalIgnoreCase))}]");
    }
#endif

    public async Task SendHeartbeatAsync(string xuid, string spoofedTitleId)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            // Don't send a request if we don't have the details
            return;
        }

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
        if (string.IsNullOrWhiteSpace(xuid))
        {
            // Don't send a request if we don't have the details
            return;
        }

        SetDefaultSpooferHeaders();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        await _spooferClient.DeleteAsync(string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid));
    }

    public async Task<AchievementsResponse?> GetAchievementsForTitleAsync(string xuid, string titleId)
        => await GetAchievementsForTitleCoreAsync(xuid, titleId, CancellationToken.None, false);

    public async Task<AchievementsResponse?> GetAchievementsForTitleAsync(
        string xuid,
        string titleId,
        CancellationToken cancellationToken)
        => await GetAchievementsForTitleCoreAsync(xuid, titleId, cancellationToken, true);

    private async Task<AchievementsResponse?> GetAchievementsForTitleCoreAsync(
        string xuid,
        string titleId,
        CancellationToken cancellationToken,
        bool ensureSuccess)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(titleId))
        {
            // Don't send a request if we don't have the details
            return null;
        }
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion4);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);

        LastAchievementsRequestUrl = string.Format(InterpolatedXboxAPIUrls.QueryAchievementsUrl, xuid, titleId);
        var httpResponse = await _httpClient.GetAsync(LastAchievementsRequestUrl, cancellationToken);
        var response = await httpResponse.Content.ReadAsStringAsync(cancellationToken);
        LastAchievementsResponseJson = response;
        if (ensureSuccess)
            httpResponse.EnsureSuccessStatusCode();
        var achievements = JsonConvert.DeserializeObject<AchievementsResponse>(response);
        return achievements;
    }

    public async Task<Xbox360AchievementResponse?> GetAchievementsFor360TitleAsync(string xuid, string titleId)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(titleId))
        {
            // Don't send a request if we don't have the details
            return null;
        }
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.Achievements);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Connection, HeaderValues.KeepAlive);
        var httpResponse = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.QueryAchievements360Url, xuid, titleId));
        var response = await httpResponse.Content.ReadAsStringAsync();
        var achievements = JsonConvert.DeserializeObject<Xbox360AchievementResponse>(response);
        return achievements;
    }

    public async Task UnlockTitleBasedAchievementAsync(string serviceConfigId, string titleId, string xuid, string achievementId, bool useFakeSignature = false)
    {
        // only unlock the specified achievement
        await UnlockTitleBasedAchievementsAsync(serviceConfigId, titleId, xuid, new List<string>() { achievementId }, useFakeSignature);
    }

    public async Task UnlockTitleBasedAchievementsAsync(string serviceConfigId, string titleId, string xuid, List<string> achievementIds, bool useFakeSignature = false)
    {
        if (string.IsNullOrWhiteSpace(serviceConfigId) || string.IsNullOrWhiteSpace(titleId) || string.IsNullOrWhiteSpace(xuid) || achievementIds.Count == 0)
        {
            // Don't send a request if we don't have the details
            return;
        }

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
        if (string.IsNullOrWhiteSpace(eventsToken))
        {
            // Don't send a request if we don't have the details
            return;
        }

        SetDefaultEventBasedHeaders();
        _eventBasedClient.DefaultRequestHeaders.Add("tickets", $"\"1\"=\"{eventsToken}\"");
        var response = await _eventBasedClient.PostAsync(BasicXboxAPIUris.TelemetryUrl, requestBody);
        var responseBody = await response.Content.ReadAsStringAsync();
        HomeViewModel.EventsLog($"POST {BasicXboxAPIUris.TelemetryUrl} => {(int)response.StatusCode} {response.StatusCode}");
        HomeViewModel.EventsLog($"Response: {responseBody}");
        if (!response.IsSuccessStatusCode)
        {
            HomeViewModel.EventsLog("Response headers:");
            foreach (var header in response.Headers)
                HomeViewModel.EventsLog($"  {header.Key}: {string.Join(", ", header.Value)}");
        }
    }

    public async Task<GamePassProducts?> GetTitleIdsFromGamePass(string prodId)
    {
        if (string.IsNullOrWhiteSpace(prodId))
        {
            // Don't send a request if we don't have the details
            return null;
        }

        SetDefaultHeaders();
        GamepassProductsRequest gamepassProducts = new GamepassProductsRequest()
        {
            Products = new List<string>() { prodId }
        };
        var titleIDsHttpResponse = await _httpClient.PostAsync(
                    BasicXboxAPIUris.GamepassCatalogUrl,
                    new StringContent(JsonConvert.SerializeObject(gamepassProducts)));
        var titleIDsResponse = await titleIDsHttpResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GamePassProducts>(titleIDsResponse);
    }
}
