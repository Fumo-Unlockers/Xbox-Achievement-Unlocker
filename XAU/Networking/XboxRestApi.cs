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

    private readonly HttpClient _eventBasedClient; // Dumb, but needed for events for now

    private readonly HttpClient _spooferClient;

    // User specifics
    private readonly string _xauth;
    private readonly string _requestedResponseLanguage;

    public XboxRestAPI(string xauth)
    {
        _xauth = SanitizeXauth(xauth);
        _requestedResponseLanguage = HomeViewModel.Settings.RegionOverride ? "en-GB" : System.Globalization.CultureInfo.CurrentCulture.Name;
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

    public static string SanitizeXauthPublic(string xauth) => SanitizeXauth(xauth);

    public static string GetSpoofAuth() =>
        string.IsNullOrWhiteSpace(HomeViewModel.SpoofXAUTH) ? HomeViewModel.XAUTH : HomeViewModel.SpoofXAUTH;

    private static string SanitizeXauth(string xauth)
    {
        if (string.IsNullOrWhiteSpace(xauth))
        {
            return "";
        }

        var trimmed = xauth.Trim();
        var startIndex = trimmed.IndexOf("XBL3.0 x=", StringComparison.Ordinal);
        if (startIndex < 0)
        {
            return trimmed;
        }

        var semicolonIndex = trimmed.IndexOf(';', startIndex);
        if (semicolonIndex < 0)
        {
            return trimmed.Substring(startIndex).Trim();
        }

        var endIndex = semicolonIndex + 1;
        while (endIndex < trimmed.Length)
        {
            var c = trimmed[endIndex];
            if (char.IsWhiteSpace(c) || c == '\0')
            {
                break;
            }

            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '=')
            {
                endIndex++;
                continue;
            }

            break;
        }

        return trimmed.Substring(startIndex, endIndex - startIndex);
    }

    private void SetHeartbeatHeaders()
    {
        _spooferClient.DefaultRequestHeaders.Clear();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
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

    public async Task<GameStatsResponse?> GetGameStatsAsync(string xuid, string titleId)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(titleId))
        {
            // Don't send a request if we don't have the details
            return null;
        }

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
        var httpResponse = await _httpClient
                .PostAsync(BasicXboxAPIUris.UserStatsUrl, new StringContent(JsonConvert.SerializeObject(gameStatsRequest), Encoding.UTF8, HeaderValues.Accept));
        var response = await httpResponse.Content.ReadAsStringAsync();
        return JsonConvert.DeserializeObject<GameStatsResponse>(response);
    }

    private void SetPresenceHeaders()
    {
        _spooferClient.DefaultRequestHeaders.Clear();
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _requestedResponseLanguage);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, _xauth);
    }

    private async Task<SpoofResult> PostSpoofAsync(string url, string requestBody, string apiName)
    {
        var response = await _spooferClient.PostAsync(
            url,
            new StringContent(requestBody, Encoding.UTF8, HeaderValues.Accept));
        var responseBody = await response.Content.ReadAsStringAsync();

#if DEBUG
        Console.WriteLine($"{apiName} {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
#endif

        if (response.IsSuccessStatusCode)
        {
            return SpoofResult.Ok();
        }

        return SpoofResult.Fail($"{apiName} {(int)response.StatusCode} {response.StatusCode}: {responseBody}");
    }

    public async Task<SpoofResult> SendHeartbeatAsync(string xuid, string spoofedTitleId)
    {
        spoofedTitleId = spoofedTitleId.Trim();
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            return SpoofResult.Fail("Missing XUID or Title ID.");
        }

        if (string.IsNullOrWhiteSpace(_xauth))
        {
            return SpoofResult.Fail("Missing XAUTH token. Log in again.");
        }

        if (!ulong.TryParse(spoofedTitleId, out var titleId))
        {
            return SpoofResult.Fail("Title ID must be numeric.");
        }

        SetHeartbeatHeaders();
        var requestBody =
            $"{{\"titles\":[{{\"expiration\":600,\"id\":{titleId},\"state\":\"active\",\"sandbox\":\"RETAIL\"}}]}}";
        return await PostSpoofAsync(
            string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid),
            requestBody,
            "Heartbeat");
    }

    public async Task<SpoofResult> SendHeartbeatMeAsync(string spoofedTitleId)
    {
        spoofedTitleId = spoofedTitleId.Trim();
        if (string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            return SpoofResult.Fail("Missing Title ID.");
        }

        if (string.IsNullOrWhiteSpace(_xauth))
        {
            return SpoofResult.Fail("Missing XAUTH token. Log in again.");
        }

        if (!ulong.TryParse(spoofedTitleId, out var titleId))
        {
            return SpoofResult.Fail("Title ID must be numeric.");
        }

        SetHeartbeatHeaders();
        var requestBody =
            $"{{\"titles\":[{{\"expiration\":600,\"id\":{titleId},\"state\":\"active\",\"sandbox\":\"RETAIL\"}}]}}";
        return await PostSpoofAsync(InterpolatedXboxAPIUrls.HeartbeatMeUrl, requestBody, "Heartbeat (me)");
    }

    public async Task<SpoofResult> SendPresenceAsync(string xuid, string spoofedTitleId)
    {
        spoofedTitleId = spoofedTitleId.Trim();
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            return SpoofResult.Fail("Missing XUID or Title ID.");
        }

        if (!ulong.TryParse(spoofedTitleId, out var titleId))
        {
            return SpoofResult.Fail("Title ID must be numeric.");
        }

        SetPresenceHeaders();
        var presenceRequest = new PresenceTitleRequest()
        {
            id = titleId
        };

        return await PostSpoofAsync(
            string.Format(InterpolatedXboxAPIUrls.PresenceUrl, xuid),
            JsonConvert.SerializeObject(presenceRequest),
            "Presence");
    }

    public async Task<SpoofResult> SendPresenceMeAsync(string spoofedTitleId)
    {
        spoofedTitleId = spoofedTitleId.Trim();
        if (string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            return SpoofResult.Fail("Missing Title ID.");
        }

        if (!ulong.TryParse(spoofedTitleId, out var titleId))
        {
            return SpoofResult.Fail("Title ID must be numeric.");
        }

        SetPresenceHeaders();
        var presenceRequest = new PresenceTitleRequest()
        {
            id = titleId
        };

        return await PostSpoofAsync(
            InterpolatedXboxAPIUrls.PresenceMeUrl,
            JsonConvert.SerializeObject(presenceRequest),
            "Presence (me)");
    }

    public async Task<SpoofResult> SendSpoofAsync(string xuid, string spoofedTitleId)
    {
        if (string.IsNullOrWhiteSpace(_xauth))
        {
            return SpoofResult.Fail("Missing XAUTH token. Log in again.");
        }

        var attempts = new List<SpoofResult>();

        var presence = await SendPresenceAsync(xuid, spoofedTitleId);
        if (presence.Success)
        {
            _ = await SendHeartbeatAsync(xuid, spoofedTitleId);
            return presence;
        }
        attempts.Add(presence);

        var presenceMe = await SendPresenceMeAsync(spoofedTitleId);
        if (presenceMe.Success)
        {
            _ = await SendHeartbeatAsync(xuid, spoofedTitleId);
            return presenceMe;
        }
        attempts.Add(presenceMe);

        var heartbeat = await SendHeartbeatAsync(xuid, spoofedTitleId);
        if (heartbeat.Success)
        {
            return heartbeat;
        }
        attempts.Add(heartbeat);

        var heartbeatMe = await SendHeartbeatMeAsync(spoofedTitleId);
        if (heartbeatMe.Success)
        {
            return heartbeatMe;
        }
        attempts.Add(heartbeatMe);

        return SpoofResult.Fail(string.Join(" | ", attempts.Select(a => a.Error).Where(e => !string.IsNullOrWhiteSpace(e))));
    }

    public async Task StopHeartbeatAsync(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            return;
        }

        SetHeartbeatHeaders();
        await _spooferClient.DeleteAsync(string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid));
        await _spooferClient.DeleteAsync(InterpolatedXboxAPIUrls.HeartbeatMeUrl);

        SetPresenceHeaders();
        await _spooferClient.DeleteAsync(string.Format(InterpolatedXboxAPIUrls.PresenceUrl, xuid));
        await _spooferClient.DeleteAsync(InterpolatedXboxAPIUrls.PresenceMeUrl);
    }

    public async Task<AchievementsResponse?> GetAchievementsForTitleAsync(string xuid, string titleId)
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

        var httpResponse = await _httpClient.GetAsync(string.Format(InterpolatedXboxAPIUrls.QueryAchievementsUrl, xuid, titleId));
        var response = await httpResponse.Content.ReadAsStringAsync();
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
