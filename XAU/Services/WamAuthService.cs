using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace XAU.Services;

// Replaces XboxAuthNet, the memory scanner, and the ETW events-token hack.
// Flow: WAM (silent) → device.auth (RPS) → user.auth → xsts → device-bound XBL3.0 + events token.
// No injection, no broker, no Xbox app dependency. Pure WAM + standard Xbox Live REST.
public static class WamAuthService
{
    private const string MsaProvider = "https://login.live.com";
    private const string MsaAuthority = "consumers";
    private const string ClientId = "000000004424da1f";
    private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";
    private const string DeviceUrl = "https://device.auth.xboxlive.com/device/authenticate";
    private const string DeviceRp = "http://auth.xboxlive.com";
    private const string UserUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string XboxRp = "http://xboxlive.com";
    private const string EventsRp = "http://events.xboxlive.com";

    private static readonly HttpClient Http = new();
    private static string? _cachedXblToken, _cachedEventsToken, _cachedXuid, _cachedUhs;
    private static DateTime _cachedAt;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(20);
    private static PopCryptoProvider? _pop;  // saved for request signing

    public static bool IsLoggedIn => !string.IsNullOrEmpty(_cachedXblToken) && DateTime.UtcNow - _cachedAt < Ttl;
    public static string? Xuid => _cachedXuid;
    public static string? Uhs => _cachedUhs;

    public static async Task<List<(string UserName, WebAccount Account)>> GetAccountsAsync()
    {
        var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(MsaProvider, MsaAuthority);
        if (provider == null) return new();
        var find = await WebAuthenticationCoreManager.FindAllAccountsAsync(provider, ClientId);
        if (find?.Accounts == null) return new();
        return find.Accounts.Select(a => (a.UserName, a)).ToList();
    }

    public static async Task<bool> LoginAsync(WebAccount account)
    {
        try
        {
            // 1) WAM silent MSA token
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync(MsaProvider, MsaAuthority);
            var request = new WebTokenRequest(provider, Scope, ClientId);
            var result = await WebAuthenticationCoreManager.GetTokenSilentlyAsync(request, account);
            string? msaToken = null;
            foreach (var rd in result.ResponseData)
                if (!string.IsNullOrEmpty(rd.Token)) { msaToken = rd.Token; break; }
            if (msaToken == null) return false;

            Http.DefaultRequestHeaders.Clear();
            Http.DefaultRequestHeaders.Add("x-xbl-contract-version", "2");
            Http.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

            // 2) Device token (broker's PC flow: RPS + MSA ticket + Windows version + PoP)
            _pop = new PopCryptoProvider();
            var deviceToken = await GetDeviceTokenAsync(msaToken, _pop);
            if (deviceToken == null) return false;

            // 3) User token
            var (userToken, uhs) = await GetUserTokenAsync(msaToken);
            if (userToken == null) return false;

            // 4) XSTS for general RP (device-bound)
            var (xsts, xuid) = await GetXstsAsync(userToken, deviceToken, XboxRp);
            if (xsts == null) return false;

            // 5) XSTS for events RP
            var (eventsXsts, _) = await GetXstsAsync(userToken, deviceToken, EventsRp);

            // Cache
            _cachedXblToken = $"XBL3.0 x={uhs};{xsts}";
            _cachedEventsToken = eventsXsts != null ? $"x:XBL3.0 x={uhs};{eventsXsts}" : null;
            _cachedXuid = xuid;
            _cachedUhs = uhs;
            _cachedAt = DateTime.UtcNow;
            return true;
        }
        catch { return false; }
    }

    public static string? GetXblToken() => IsLoggedIn ? _cachedXblToken : null;
    public static string? GetEventsToken() => IsLoggedIn ? _cachedEventsToken : null;

    public static string? SignRequest(string method, string uri, string body) =>
        _pop?.SignRequest(method, uri, _cachedXblToken ?? "", body);

    private static async Task<string?> GetDeviceTokenAsync(string msaToken, PopCryptoProvider pop)
    {
        var v = Environment.OSVersion.Version;
        string winVer = $"{v.Major}.{v.Minor}.{v.Build}.{v.Revision}";
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = "t=" + msaToken, Version = winVer, ProofKey = pop.ProofKey },
            RelyingParty = DeviceRp,
            TokenType = "JWT"
        });
        var req = new HttpRequestMessage(HttpMethod.Post, DeviceUrl) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        req.Headers.Add("Signature", pop.SignRequest("POST", DeviceUrl, "", body));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var resp = await Http.SendAsync(req);
        if (!resp.IsSuccessStatusCode) return null;
        var rb = await resp.Content.ReadAsStringAsync();
        return JsonDocument.Parse(rb).RootElement.GetProperty("Token").GetString();
    }

    private static async Task<(string? token, string uhs)> GetUserTokenAsync(string msaToken)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { AuthMethod = "RPS", SiteName = "user.auth.xboxlive.com", RpsTicket = "t=" + msaToken },
            RelyingParty = "http://auth.xboxlive.com",
            TokenType = "JWT"
        });
        var (code, resp) = await PostAsync(UserUrl, body);
        if (code != 200) return (null, "");
        var d = JsonDocument.Parse(resp).RootElement;
        return (d.GetProperty("Token").GetString(), d.GetProperty("DisplayClaims").GetProperty("xui")[0].GetProperty("uhs").GetString() ?? "");
    }

    private static async Task<(string? token, string? xuid)> GetXstsAsync(string userToken, string deviceToken, string rp)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { userToken }, DeviceToken = deviceToken },
            RelyingParty = rp,
            TokenType = "JWT"
        });
        var (code, resp) = await PostAsync(XstsUrl, body);
        if (code != 200) return (null, null);
        var d = JsonDocument.Parse(resp).RootElement;
        var xui = d.GetProperty("DisplayClaims").GetProperty("xui")[0];
        return (d.GetProperty("Token").GetString(), xui.TryGetProperty("xid", out var x) ? x.GetString() : null);
    }

    private static async Task<(int code, string body)> PostAsync(string url, string json)
    {
        using var resp = await Http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }
}

// EC P-256 proof-of-possession: ephemeral keypair, JWK ProofKey, ES256 signature.
sealed class PopCryptoProvider
{
    private readonly ECDsa _signer = ECDsa.Create(ECCurve.NamedCurves.nistP256);
    private object? _proofKey;
    public object ProofKey => _proofKey ??= BuildProofKey();
    private object BuildProofKey()
    {
        var p = _signer.ExportParameters(false);
        return new { kty = "EC", crv = "P-256", alg = "ES256", use = "sig", x = B64Url(p.Q.X), y = B64Url(p.Q.Y) };
    }
    public string SignRequest(string method, string reqUri, string token, string body)
    {
        var winTs = ((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 11644473600ul) * 10000000ul;
        var pathQuery = new Uri(reqUri).PathAndQuery;
        var strs = Encoding.ASCII.GetBytes($"{method}\0{pathQuery}\0{token}\0{body}\0");
        var payload = new byte[4 + 1 + 8 + 1 + strs.Length];
        BeInt(1).CopyTo(payload, 0); payload[4] = 0; BeULong(winTs).CopyTo(payload, 5); payload[13] = 0; strs.CopyTo(payload, 14);
        var sig = _signer.SignData(payload, HashAlgorithmName.SHA256);
        var header = new byte[12 + sig.Length];
        BeInt(1).CopyTo(header, 0); BeULong(winTs).CopyTo(header, 4); sig.CopyTo(header, 12);
        return Convert.ToBase64String(header);
    }
    private static byte[] BeInt(int v) { var b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); return b; }
    private static byte[] BeULong(ulong v) { var b = BitConverter.GetBytes(v); if (BitConverter.IsLittleEndian) Array.Reverse(b); return b; }
    private static string B64Url(byte[] d) => Convert.ToBase64String(d).Replace('+', '-').Replace('/', '_').TrimEnd('=');
}
