using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace XAU.Services.Spoofer
{
    /// <summary>One presence/heartbeat attempt and its raw outcome.</summary>
    public sealed class SpoofAttemptResult
    {
        public string Name { get; init; } = "";
        public string Url { get; init; } = "";
        public int StatusCode { get; init; }
        public string ResponseBody { get; init; } = "";
        public string Error { get; init; } = "";
        public bool Success => StatusCode >= 200 && StatusCode < 300;
    }

    /// <summary>Aggregate outcome of one spoof chain run.</summary>
    public sealed class SpoofChainResult
    {
        public List<SpoofAttemptResult> Attempts { get; init; } = new();
        public bool Success => Attempts.Any(a => a.Success);
        public SpoofAttemptResult? Winning => Attempts.FirstOrDefault(a => a.Success);
    }

    /// <summary>
    /// Presence/heartbeat spoofer, UI-independent so it is testable from a console harness.
    /// One instance per run; the token is supplied by the caller (memory scan, OAuth, or paste).
    ///
    /// Chain order per Fumo PR #519: userpresence xuid -> userpresence /me ->
    /// presence-heartbeat xuid -> presence-heartbeat /me. A 401 aborts the chain (token
    /// rejection is endpoint-independent; the caller should re-auth), everything else falls through.
    /// </summary>
    public sealed class PresenceSpoofClient
    {
        public const string TitleIdEso = "1285166951";

        public const string HeartbeatUrl = "https://presence-heartbeat.xboxlive.com/users/xuid({0})/devices/current/";
        public const string HeartbeatMeUrl = "https://presence-heartbeat.xboxlive.com/users/me/devices/current/";
        public const string PresenceUrl = "https://userpresence.xboxlive.com/users/xuid({0})/devices/current/titles/current";
        public const string PresenceMeUrl = "https://userpresence.xboxlive.com/users/me/devices/current/titles/current";
        public const string ProfileUrl = "https://profile.xboxlive.com/users/me/profile/settings?settings=Gamertag";

        private readonly HttpClient _client;

        public PresenceSpoofClient(HttpClient? client = null)
        {
            _client = client ?? new HttpClient(new HttpClientHandler()
            {
                AutomaticDecompression = System.Net.DecompressionMethods.GZip | System.Net.DecompressionMethods.Deflate
            });
        }

        /// <summary>Trims an XBL3.0 token to exactly "XBL3.0 x=&lt;hex&gt;;&lt;payload&gt;".</summary>
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

        /// <summary>Heartbeat body: numeric title id, 600s expiry, active state, RETAIL sandbox.</summary>
        public static string BuildHeartbeatBody(string spoofedTitleId) =>
            "{\"titles\":[{\"expiration\":600,\"id\":" + ulong.Parse(spoofedTitleId.Trim()) + ",\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}";

        public static string BuildPresenceBody(string spoofedTitleId) =>
            "{\"id\":" + ulong.Parse(spoofedTitleId.Trim()) + "}";

        private void SetHeaders(string url, string xauth)
        {
            _client.DefaultRequestHeaders.Clear();
            _client.DefaultRequestHeaders.Add("Authorization", SanitizeXauth(xauth));
            _client.DefaultRequestHeaders.Add("accept-language", "en-GB");
            _client.DefaultRequestHeaders.Add("Accept-Encoding", "gzip, deflate");
            _client.DefaultRequestHeaders.Add("accept", "application/json");
            _client.DefaultRequestHeaders.Add("x-xbl-contract-version", "3");
            _client.DefaultRequestHeaders.Add("Host", new Uri(url).Host);
        }

        /// <summary>Resolves the caller's XUID from the profile API using the given token.</summary>
        public async Task<string?> ResolveXuidAsync(string xauth, CancellationToken ct = default)
        {
            const string url = ProfileUrl;
            SetHeaders(url, xauth);
            using var response = await _client.GetAsync(url, ct);
            string body = await response.Content.ReadAsStringAsync(ct);
            if (!response.IsSuccessStatusCode)
                return null;
            // Minimal parse without dragging JSON libs in: "id":"2533274..."
            const string marker = "\"id\":\"";
            int i = body.IndexOf(marker, StringComparison.Ordinal);
            if (i < 0)
                return null;
            int s = i + marker.Length;
            int e = body.IndexOf('"', s);
            return e < 0 ? null : body.Substring(s, e - s);
        }

        /// <summary>Runs the full fallback chain once. Never throws for HTTP failures; returns per-attempt detail.</summary>
        public async Task<SpoofChainResult> RunChainAsync(string xauth, string xuid, string spoofedTitleId, CancellationToken ct = default)
        {
            var result = new SpoofChainResult();
            string heartbeatBody = BuildHeartbeatBody(spoofedTitleId);
            string presenceBody = BuildPresenceBody(spoofedTitleId);
            var attempts = new (string url, string body, string name)[]
            {
                (string.Format(PresenceUrl, xuid), presenceBody, "Presence (xuid)"),
                (PresenceMeUrl, presenceBody, "Presence (me)"),
                (string.Format(HeartbeatUrl, xuid), heartbeatBody, "Heartbeat (xuid)"),
                (HeartbeatMeUrl, heartbeatBody, "Heartbeat (me)"),
            };
            foreach (var (url, body, name) in attempts)
            {
                try
                {
                    SetHeaders(url, xauth);
                    using var content = new StringContent(body, Encoding.UTF8, "application/json");
                    using var response = await _client.PostAsync(url, content, ct);
                    string responseBody = await response.Content.ReadAsStringAsync(ct);
                    result.Attempts.Add(new SpoofAttemptResult
                    {
                        Name = name, Url = url,
                        StatusCode = (int)response.StatusCode,
                        ResponseBody = responseBody
                    });
                    if (response.IsSuccessStatusCode)
                        return result;
                    if (response.StatusCode == System.Net.HttpStatusCode.Unauthorized)
                        return result; // token rejection: no point trying other URIs
                }
                catch (OperationCanceledException) { throw; }
                catch (Exception ex)
                {
                    result.Attempts.Add(new SpoofAttemptResult
                    {
                        Name = name, Url = url, Error = $"{ex.GetType().Name}: {ex.Message}"
                    });
                }
            }
            return result;
        }

        /// <summary>Best-effort removal across every URI the spoof may have used.</summary>
        public async Task StopAsync(string xauth, string xuid, CancellationToken ct = default)
        {
            var targets = new (string url, string name)[]
            {
                (string.Format(HeartbeatUrl, xuid), "Heartbeat (xuid)"),
                (HeartbeatMeUrl, "Heartbeat (me)"),
                (string.Format(PresenceUrl, xuid), "Presence (xuid)"),
                (PresenceMeUrl, "Presence (me)"),
            };
            foreach (var (url, name) in targets)
            {
                try
                {
                    SetHeaders(url, xauth);
                    using var response = await _client.DeleteAsync(url, ct);
                    // 404 = variant was never set; not an error.
                }
                catch { /* best effort */ }
            }
        }
    }
}
