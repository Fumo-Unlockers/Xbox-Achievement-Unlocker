using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using XAU.Services.Spoofer;
using XAU.Util.Diagnostics;
using XAU.Util.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.ViewModels.Pages;
using XAU.ViewModels.Windows;

public class XboxRestAPI
{
    internal readonly HttpClient _httpClient;

    internal readonly HttpClient _eventBasedClient; // Dumb, but needed for events for now

    internal readonly HttpClient _spooferClient;

    // Dedicated, UNAUTHENTICATED client used only by CheckServiceHealthAsync() to liveness-probe
    // the Xbox Live endpoints. Kept separate so a status ping never disturbs the authed clients'
    // default headers.
    internal readonly HttpClient _probeClient;
    internal TimeSpan ProbeTimeout { get; set; } = TimeSpan.FromSeconds(5);

    // User specifics
    private readonly string _requestedResponseLanguage;

    public XboxRestAPI(string xauth)
    {
        // The constructor retains its original signature for existing callers. Authorization must
        // always come from the active session; a captured constructor token would survive logout.
        _requestedResponseLanguage = ResolveAcceptLanguage(HomeViewModel.Settings.RegionOverride,
            System.Globalization.CultureInfo.CurrentCulture.Name);
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

        var probeHandler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _probeClient = new HttpClient(probeHandler);
    }

    private struct ProbeTarget
    {
        public string Name;
        public string Url;
        public ProbeTarget(string name, string url) { Name = name; Url = url; }
    }

    // Unauthenticated liveness probes. Root GETs are not the authenticated operations used
    // for stats or presence; their response is only a reachability signal.
    private static readonly ProbeTarget[] ServiceProbeTargets = new[]
    {
        new ProbeTarget("profile", "https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag"),
        new ProbeTarget("achievements", "https://achievements.xboxlive.com/"),
        new ProbeTarget("userstats", "https://userstats.xboxlive.com/"),
        new ProbeTarget("titlehub", "https://titlehub.xboxlive.com/"),
        new ProbeTarget("presence", "https://presence-heartbeat.xboxlive.com/"),
    };

    /// <summary>
    /// Liveness-probes the endpoints above without authentication. A 401/403 means the
    /// endpoint answered but says nothing about this user's token or the heartbeat POST.
    /// A 5xx or no response may be a service, proxy, or local-network problem.
    /// </summary>
    public async Task<XblServiceHealthReport> CheckServiceHealthAsync()
    {
        var results = new List<XblServiceProbeResult>();
        foreach (var target in ServiceProbeTargets)
        {
            bool reached = false;
            int code = 0;
            string detail = null;
            try
            {
                using var timeout = new CancellationTokenSource(ProbeTimeout);
                using var response = await _probeClient.GetAsync(target.Url, timeout.Token);
                reached = true;
                code = (int)response.StatusCode;
            }
            catch (HttpRequestException ex)
            {
                // A status code means the endpoint DID answer over HTTP -> it is reachable.
                // StatusCode is NULLABLE on HttpRequestException (null = no HTTP response at all),
                // so guard the cast instead of throwing from inside the catch.
                int c = (int?)ex.StatusCode ?? 0;
                reached = c != 0;
                code = c;
                detail = (ex.StatusCode?.ToString() ?? "<no status>") + " " + ex.Message;
            }
            catch (Exception ex)
            {
                reached = false;
                code = 0;
                detail = ex.GetType().Name + ": " + ex.Message;
            }

            var verdict = XblServiceHealth.Classify(reached, code);
            results.Add(new XblServiceProbeResult(target.Name, target.Url, reached, code, verdict, detail));
            DiagLog.Write($"[XBLSTATUS] {target.Name}: {verdict} code={code} {detail}");
        }

        return new XblServiceHealthReport(
            results, XblServiceHealth.Summarize(results), XblServiceHealth.LooksLikeServiceOutage(results));
    }

    internal static string ResolveAcceptLanguage(bool regionOverride, string? cultureName) =>
        regionOverride || string.IsNullOrWhiteSpace(cultureName) ? "en-GB" : cultureName;

    internal string CurrentXauth => HomeViewModel.XAUTH;

    internal void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        var xauth = CurrentXauth;
        if (!string.IsNullOrWhiteSpace(xauth))
            _httpClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, xauth);
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

    internal void SetDefaultSpooferHeaders()
    {
        _spooferClient.DefaultRequestHeaders.Clear();
        var xauth = CurrentXauth;
        if (!string.IsNullOrWhiteSpace(xauth))
            _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, xauth);
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

    internal void SetDefaultEventBasedHeaders()
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
        var authxtoken = Regex.Replace(CurrentXauth ?? "", @"XBL3\.0 x=\d+;", "XBL3.0 x=-;");
        if (!string.IsNullOrWhiteSpace(authxtoken))
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

    /// <summary>
    /// Trims an XBL3.0 token to exactly "XBL3.0 x=&lt;hex&gt;;&lt;payload&gt;". The memory-scan token
    /// can carry trailing whitespace/NULs or garbage after the payload; profile/userstats accept
    /// that, but presence/heartbeat reject it (HTTP 403). Pure; unit-testable.
    /// </summary>
    public static string SanitizeXauth(string? xauth)
    {
        if (string.IsNullOrWhiteSpace(xauth))
            return "";
        var trimmed = xauth.Trim();
        int start = trimmed.IndexOf("XBL3.0 x=", StringComparison.Ordinal);
        if (start < 0)
            return trimmed;
        int semi = trimmed.IndexOf(';', start);
        if (semi < 0)
            return trimmed.Substring(start).Trim();
        int end = semi + 1;
        while (end < trimmed.Length)
        {
            char c = trimmed[end];
            if (char.IsWhiteSpace(c) || c == '\0')
                break;
            if (char.IsLetterOrDigit(c) || c is '-' or '_' or '.' or '=')
            {
                end++;
                continue;
            }
            break;
        }
        return trimmed.Substring(start, end - start);
    }

    private string SpooferAuth => SanitizeXauth(CurrentXauth);

    private void SetSpoofHeaders(string url, string? xauth = null)
    {
        _spooferClient.DefaultRequestHeaders.Clear();
        var token = xauth ?? SpooferAuth;
        if (!string.IsNullOrWhiteSpace(token))
            _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Authorization, token);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.AcceptLanguage, _requestedResponseLanguage);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.ContractVersion, HeaderValues.ContractVersion3);
        _spooferClient.DefaultRequestHeaders.Add(HeaderNames.Host, new Uri(url).Host);
    }

    /// <summary>Heartbeat body: numeric title id, 600s expiry, active state, RETAIL sandbox.</summary>
    internal static string BuildHeartbeatBody(string spoofedTitleId) =>
        "{\"titles\":[{\"expiration\":600,\"id\":" + ulong.Parse(spoofedTitleId.Trim()) + ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}";

    private async Task PostSpoofWithTokenOrThrowAsync(string url, string requestBody, string apiName, string xauth, CancellationToken ct)
    {
        SetSpoofHeaders(url, SanitizeXauth(xauth));
        using var content = new StringContent(requestBody, Encoding.UTF8, HeaderValues.Accept);
        using var response = await _spooferClient.PostAsync(url, content, ct);
        if (!response.IsSuccessStatusCode)
        {
            throw new HttpRequestException(
                $"{apiName} failed: {(int)response.StatusCode} {response.StatusCode}",
                null, response.StatusCode);
        }
    }

    public async Task SendHeartbeatAsync(string xuid, string spoofedTitleId, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(xuid) || string.IsNullOrWhiteSpace(spoofedTitleId))
        {
            // Don't send a request if we don't have the details
            return;
        }

        spoofedTitleId = spoofedTitleId.Trim();
        try
        {
            await RunSpoofChainAsync(SpooferAuth, xuid, spoofedTitleId, cancellationToken);
            return;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (HttpRequestException ex)
        {
            // A 401 is token rejection, not an endpoint quirk: fall back to auth recovery
            // immediately instead of retrying with a different token.
            if (ex.StatusCode == HttpStatusCode.Unauthorized)
                throw;
        }

        // Hybrid spoofing (OAuth login): OAuth/SISU-minted tokens lack the title/device claims
        // presence-heartbeat requires (403 Forbidden) while the Xbox app's own memory-scanned
        // token carries them and is accepted. When OAuth mode is active and the chain failed
        // with the primary token, retry once with a freshly scanned app token.
        if (!HomeViewModel.Settings.OAuthLogin)
            throw new HttpRequestException("Heartbeat failed: presence rejected the primary token", null, HttpStatusCode.Forbidden);

        var memToken = await GetMemorySpoofTokenAsync(cancellationToken);
        if (string.IsNullOrWhiteSpace(memToken) || memToken == SpooferAuth)
            throw new HttpRequestException("Heartbeat failed: presence rejected the OAuth token and no usable app token was found in Xbox app memory", null, HttpStatusCode.Forbidden);

        DiagLog.Write("[SPOOF] primary (OAuth) token rejected by presence; retrying with memory-scanned Xbox app token");
        await RunSpoofChainAsync(memToken, xuid, spoofedTitleId, cancellationToken);
    }

    private async Task RunSpoofChainAsync(string xauth, string xuid, string spoofedTitleId, CancellationToken ct)
    {
        var errors = new List<string>();
        string heartbeatBody = BuildHeartbeatBody(spoofedTitleId);
        string presenceBody = "{\"id\":" + ulong.Parse(spoofedTitleId) + "}";

        // Presence/heartbeat reject requests that profile and userstats accept (HTTP 403 even with a
        // token that just authenticated the profile API). The service accepts the same operation at
        // several URIs; try them in the order the reference fix (Fumo PR #519) established:
        // userpresence (xuid, then /me), then presence-heartbeat (xuid, then /me).
        var attempts = new (string url, string body, string name)[]
        {
            (string.Format(InterpolatedXboxAPIUrls.PresenceUrl, xuid), presenceBody, "Presence"),
            (InterpolatedXboxAPIUrls.PresenceMeUrl, presenceBody, "Presence (me)"),
            (string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid), heartbeatBody, "Heartbeat"),
            (InterpolatedXboxAPIUrls.HeartbeatMeUrl, heartbeatBody, "Heartbeat (me)"),
        };
        foreach (var (url, body, name) in attempts)
        {
            try
            {
                await PostSpoofWithTokenOrThrowAsync(url, body, name, xauth, ct);
                return;
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (HttpRequestException ex)
            {
                // A 401 is token rejection, not an endpoint quirk: fall back to auth recovery
                // immediately instead of hammering the other presence URIs with the same bad token.
                if (ex.StatusCode == HttpStatusCode.Unauthorized)
                    throw;
                errors.Add($"{name}: {ex.Message}");
            }
        }
        throw new HttpRequestException("Heartbeat failed: " + string.Join(" | ", errors), null, HttpStatusCode.Forbidden);
    }

    private string? _spoofMemToken;
    private DateTime _spoofMemTokenAt = DateTime.MinValue;

    /// <summary>
    /// Scans the Xbox app processes for an XBL3.0 token and returns the first candidate the
    /// profile API accepts (reads are frequently truncated/garbage-tailed, so each candidate is
    /// refereed). Cached for 5 minutes; null when the Xbox app is not running or nothing passes.
    /// </summary>
    private async Task<string?> GetMemorySpoofTokenAsync(CancellationToken ct)
    {
        if (_spoofMemToken != null && DateTime.UtcNow - _spoofMemTokenAt < TimeSpan.FromMinutes(5))
            return _spoofMemToken;

        foreach (var proc in new[] { "XboxPcApp", "XboxPcAppFT" })
        {
            var scanner = new XauTokenScanner(proc);
            if (!scanner.Attach())
                continue;
            List<(string token, int frequency, bool plausible)> candidates;
            try
            {
                candidates = await scanner.ScanAsync(keepRaw: true);
            }
            catch (Exception ex)
            {
                DiagLog.Write($"[SPOOF] memory token scan ({proc}) failed: {ex.GetType().Name}: {ex.Message}");
                continue;
            }
            foreach (var (raw, _, _) in candidates)
            {
                var candidate = SanitizeXauth(raw);
                if (string.IsNullOrWhiteSpace(candidate))
                    continue;
                if (await ProfileAuthenticatesAsync(candidate, ct))
                {
                    _spoofMemToken = candidate;
                    _spoofMemTokenAt = DateTime.UtcNow;
                    DiagLog.Write($"[SPOOF] memory token acquired from {proc} (len={candidate.Length})");
                    return _spoofMemToken;
                }
            }
        }
        return null;
    }

    /// <summary>True when the profile API accepts the given token (candidate referee).</summary>
    private async Task<bool> ProfileAuthenticatesAsync(string xauth, CancellationToken ct)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get,
                "https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag");
            req.Headers.Add("Authorization", xauth);
            req.Headers.Add("Accept-Language", _requestedResponseLanguage);
            req.Headers.Add("Accept-Encoding", HeaderValues.AcceptEncoding);
            req.Headers.Add("Accept", HeaderValues.Accept);
            req.Headers.Add("x-xbl-contract-version", "2");
            using var resp = await _httpClient.SendAsync(req, ct);
            return resp.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task StopHeartbeatAsync(string xuid)
    {
        if (string.IsNullOrWhiteSpace(xuid))
        {
            // Don't send a request if we don't have the details
            return;
        }

        // Best-effort removal across every URI the spoof may have used; a failure on one
        // (e.g. 404 because that variant was never set) must not abort cleanup of the rest.
        // Hybrid (OAuth login): presence rejects OAuth-minted tokens on DELETE too, so retry
        // the removals with the memory-scanned app token when the primary token is rejected.
        _removalRejected = false;
        var targets = new (string url, string name)[]
        {
            (string.Format(InterpolatedXboxAPIUrls.HeartbeatUrl, xuid), "Heartbeat"),
            (InterpolatedXboxAPIUrls.HeartbeatMeUrl, "Heartbeat (me)"),
            (string.Format(InterpolatedXboxAPIUrls.PresenceUrl, xuid), "Presence"),
            (InterpolatedXboxAPIUrls.PresenceMeUrl, "Presence (me)"),
        };
        foreach (var (url, name) in targets)
        {
            await TryRemovePresenceAsync(url, name, SpooferAuth);
        }
        if (HomeViewModel.Settings.OAuthLogin && _removalRejected)
        {
            var memToken = await GetMemorySpoofTokenAsync(CancellationToken.None);
            if (!string.IsNullOrWhiteSpace(memToken) && memToken != SpooferAuth)
            {
                DiagLog.Write("[SPOOF] presence removal rejected the OAuth token; retrying with memory-scanned Xbox app token");
                foreach (var (url, name) in targets)
                    await TryRemovePresenceAsync(url, name, memToken);
            }
        }
    }

    private bool _removalRejected;

    private async Task TryRemovePresenceAsync(string url, string name, string xauth)
    {
        try
        {
            SetSpoofHeaders(url, xauth);
            using var response = await _spooferClient.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
                return;
            if ((int)response.StatusCode != 404)
            {
                _removalRejected = _removalRejected || response.StatusCode is HttpStatusCode.Forbidden or HttpStatusCode.Unauthorized;
                DiagLog.Write($"[SPOOF] {name} removal returned {(int)response.StatusCode} {response.StatusCode}");
            }
        }
        catch (Exception ex)
        {
            DiagLog.Write($"[SPOOF] {name} removal failed: {ex.GetType().Name}: {ex.Message}");
        }
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
