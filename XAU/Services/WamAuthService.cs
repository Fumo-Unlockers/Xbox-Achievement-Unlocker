using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Windows.Security.Authentication.Web.Core;
using Windows.Security.Credentials;

namespace XAU.Services;

// Fluxo: WAM (silencioso) → device.auth → user.auth → XSTS. Tokens device-bound (PoP):
// WRITES levam header Signature ES256; reads nao. Sem injecao/broker/Xbox-app.
public static class WamAuthService
{
    private const string MsaProvider = "https://login.live.com";
    private const string MsaAuthority = "consumers";
    private const string ClientId = "000000004424da1f";
    private const string Scope = "service::user.auth.xboxlive.com::MBI_SSL";
    private const string DeviceUrl = "https://device.auth.xboxlive.com/device/authenticate";
    private const string DeviceRp = "http://auth.xboxlive.com";
    private const string TitleAuthUrl = "https://title.auth.xboxlive.com/title/authenticate";
    private const string UserUrl = "https://user.auth.xboxlive.com/user/authenticate";
    private const string XstsUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";
    private const string XboxRp = "http://xboxlive.com";
    private const string EventsRp = "http://events.xboxlive.com";
    private const string SisuUrl = "https://sisu.xboxlive.com/authorize";
    private const string OAuthAuthorize = "https://login.live.com/oauth20_authorize.srf";
    private const string OAuthToken = "https://login.live.com/oauth20_token.srf";
    private const string OAuthDesktop = "https://login.live.com/oauth20_desktop.srf";
    private const string UserAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/71.0.3578.98 Safari/537.36";

    private static readonly HttpClient Http = new();
    // Client limpo (sem headers padrao) para o device-token assinado.
    private static readonly HttpClient SignedHttp = new();
    private static string? _cachedXblToken, _cachedEventsToken, _cachedXuid, _cachedUhs, _cachedSpoofToken;
    private static DateTime _cachedAt;
    private static readonly TimeSpan Ttl = TimeSpan.FromHours(20);
    private static PopCryptoProvider? _pop;

    public static string? LastError;
    private static readonly string LogPath = Path.Combine(Path.GetTempPath(), "xau_wam_auth.log");
    public static void Diag(string msg)
    {
        LastError = msg;
        try { File.AppendAllText(LogPath, $"{DateTime.UtcNow:HH:mm:ss} {msg}{Environment.NewLine}"); } catch { }
    }

    public static bool IsLoggedIn => !string.IsNullOrEmpty(_cachedXblToken) && DateTime.UtcNow - _cachedAt < Ttl;
    public static string? Xuid => _cachedXuid;
    public static string? Uhs => _cachedUhs;

    // Adota um token gerado externamente (cache GDK do Xbox-app). Nao esta atrelado a
    // nossa chave PoP, entao a assinatura fica desligada (_pop null → sem Signature).
    public static void SetExternalToken(string xbl, string xuid, string uhs)
    {
        _pop = null;
        _cachedXblToken = xbl;
        _cachedSpoofToken = xbl;
        _cachedXuid = xuid;
        _cachedUhs = uhs;
        _cachedAt = DateTime.UtcNow;
        Diag($"external token set (xuid={xuid})");
    }

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
            // 1) Token MSA silencioso via WAM
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

            // 2) Device token (RPS) — atrela a identidade do device ao usuario logado.
            _pop = new PopCryptoProvider();
            var deviceToken = await GetDeviceTokenAsync(msaToken, _pop);
            if (deviceToken == null) { Diag("device token failed"); return false; }

            // 3) User token — compartilhado pelos XSTS main + events.
            var (userToken, uhs) = await GetUserTokenAsync(msaToken);
            if (userToken == null || string.IsNullOrEmpty(uhs)) { Diag("user token failed"); return false; }

            // 3b) Title token (opcional) — da ao XSTS main o title claim exigido por
            //     WRITES de achievement + spoof de presenca. Segue sem ele em caso de falha.
            var titleToken = await GetTitleTokenAsync(msaToken, deviceToken, _pop);
            Diag(titleToken != null ? "title token OK" : "title token failed (continuing without)");

            // 4) xauth principal = XSTS device-bound para xboxlive.com, com o title claim.
            var (mainXsts, xuid, _) = await GetXstsAsync(userToken, deviceToken, XboxRp, titleToken);
            if (mainXsts == null) { Diag("main XSTS failed"); return false; }
            var xbl = $"XBL3.0 x={uhs};{mainXsts}";

            // 5) Events token — XSTS para events.xboxlive.com.
            var (eventsXsts, _, _) = await GetXstsAsync(userToken, deviceToken, EventsRp);

            // Cache. O spoofer reusa o xauth principal (nao ha token de presenca separado).
            _cachedXblToken = xbl;
            _cachedSpoofToken = xbl;
            _cachedEventsToken = eventsXsts != null ? $"x:XBL3.0 x={uhs};{eventsXsts}" : null;
            _cachedXuid = xuid;
            _cachedUhs = uhs;
            _cachedAt = DateTime.UtcNow;
            Diag("login OK");
            return true;
        }
        catch (Exception e) { Diag("LoginAsync exception: " + e.Message); return false; }
    }

    // Login OAuth interativo → SISU. Autoriza device + title + user juntos e retorna
    // token com title claim. Deve rodar na UI thread (abre janela WebView2).
    public static async Task<bool> LoginWithSisuAsync()
    {
        try
        {
            // 1) OAuth interativo — o token que o SISU aceita.
            var code = await GetOAuthCodeAsync();
            if (string.IsNullOrEmpty(code)) { Diag("sisu: no oauth code"); return false; }
            var accessToken = await ExchangeCodeAsync(code);
            if (accessToken == null) { Diag("sisu: token exchange failed"); return false; }

            // 2) Device token PoP anonimo (exigido pelo SISU).
            _pop = new PopCryptoProvider();
            var deviceToken = await GetPopDeviceTokenAsync(_pop);
            if (deviceToken == null) { Diag("sisu: device token failed"); return false; }

            // 3) SISU authorize (RP xboxlive.com) — xauth principal + tokens device/title/user.
            var sisu = await SisuAuthorizeAsync(accessToken, deviceToken, _pop);
            if (sisu.xbl == null || sisu.uhs == null) { Diag("sisu authorize failed"); return false; }

            _cachedXblToken = sisu.xbl;
            _cachedSpoofToken = sisu.xbl;
            _cachedXuid = sisu.xuid;
            _cachedUhs = sisu.uhs;

            // 4) Token com presenca: XSTS dos tokens user+title+device do SISU para
            //    xboxlive.com. Se gerar, usa ele no spoofer.
            if (!string.IsNullOrEmpty(sisu.userToken))
            {
                var (pres, _, presUhs) = await GetXstsAsync(sisu.userToken!, deviceToken, XboxRp, sisu.titleToken);
                if (pres != null && presUhs != null) _cachedSpoofToken = $"XBL3.0 x={presUhs};{pres}";

                // 5) Events token.
                var (ev, _, evUhs) = await GetXstsAsync(sisu.userToken!, deviceToken, EventsRp);
                _cachedEventsToken = ev != null ? $"x:XBL3.0 x={evUhs};{ev}" : null;
            }

            _cachedAt = DateTime.UtcNow;
            Diag($"login OK (SISU/OAuth) | title={(sisu.titleToken != null ? "yes" : "no")}");
            return true;
        }
        catch (Exception e) { Diag("LoginWithSisu exception: " + e.Message); return false; }
    }

    // EXPERIMENTO: autoriza pelo provider WAM do Xbox Identity (xsts.auth.xboxlive.com),
    // mesmo componente do Xbox app, que ja tem o enrollment do device desta maquina.
    public static async Task<bool> LoginWithXboxProviderAsync()
    {
        try
        {
            var provider = await WebAuthenticationCoreManager.FindAccountProviderAsync("https://xsts.auth.xboxlive.com");
            if (provider == null) { Diag("xbox provider: not found"); return false; }
            Diag($"xbox provider: {provider.Id} / {provider.DisplayName}");

            // Conta MSA para anexar (o provider Xbox se apoia em MSA).
            WebAccount? account = null;
            var msaProvider = await WebAuthenticationCoreManager.FindAccountProviderAsync(MsaProvider, MsaAuthority);
            if (msaProvider != null)
            {
                var find = await WebAuthenticationCoreManager.FindAllAccountsAsync(msaProvider, ClientId);
                account = find?.Accounts?.FirstOrDefault();
            }

            WebTokenRequest MakeReq(string target)
            {
                // Sem clientId — WAM rejeita clientId de app externo; o provider deriva.
                var r = new WebTokenRequest(provider);
                r.Properties.Add("Url", target);      // exigido pelo provider
                r.Properties.Add("Target", target);
                r.Properties.Add("Policy", "RETAIL");
                return r;
            }

            bool Use(WebTokenRequestResult result, string how)
            {
                Diag($"xbox {how} status={result.ResponseStatus}");
                if (result.ResponseStatus != WebTokenRequestStatus.Success)
                {
                    if (result.ResponseError != null)
                        Diag($"  xbox err {result.ResponseError.ErrorCode}: {result.ResponseError.ErrorMessage}");
                    return false;
                }
                var rd = result.ResponseData[0];
                foreach (var kv in rd.Properties) Diag($"  xbox prop {kv.Key}={Trunc(kv.Value)}");
                Diag($"  xbox token: {Trunc(rd.Token)}");
                var token = rd.Token;
                string xbl = token.StartsWith("XBL3.0") ? token
                    : (rd.Properties.TryGetValue("UserHash", out var uh) || rd.Properties.TryGetValue("Uhs", out uh)) ? $"XBL3.0 x={uh};{token}"
                    : $"XBL3.0 x=-;{token}";
                _cachedXblToken = xbl;
                _cachedSpoofToken = xbl;
                if (rd.Properties.TryGetValue("XboxUserId", out var xid)) _cachedXuid = xid;
                _cachedAt = DateTime.UtcNow;
                Diag("login OK (Xbox WAM provider)");
                return true;
            }

            // 1) Silencioso — funciona so se ja existe um grant.
            foreach (var target in new[] { "http://xboxlive.com", "xboxlive.com" })
            {
                var result = account != null
                    ? await WebAuthenticationCoreManager.GetTokenSilentlyAsync(MakeReq(target), account)
                    : await WebAuthenticationCoreManager.GetTokenSilentlyAsync(MakeReq(target));
                if (Use(result, $"silent target={target}")) return true;
            }

            // 2) Interativo — cria o consent/grant inicial. WAM desktop precisa do handle
            //    da janela pai (o RequestTokenAsync simples lanca excecao).
            try
            {
                var hwnd = IntPtr.Zero;
                var win = System.Windows.Application.Current?.MainWindow;
                if (win != null) hwnd = new System.Windows.Interop.WindowInteropHelper(win).Handle;
                Diag($"xbox interactive: hwnd={hwnd}");
                var result = await WamInterop.RequestTokenForWindowAsync(hwnd, MakeReq("http://xboxlive.com"));
                if (Use(result, "interactive")) return true;
            }
            catch (Exception ie) { Diag("xbox interactive threw: " + ie.GetType().Name + ": " + ie.Message); }

            return false;
        }
        catch (Exception e) { Diag("xbox provider exception: " + e.Message); return false; }
    }

    private static async Task<string?> GetOAuthCodeAsync()
    {
        var authUrl = OAuthAuthorize + "?" +
            $"client_id={Uri.EscapeDataString(ClientId)}" +
            $"&scope={Uri.EscapeDataString(Scope)}" +
            "&response_type=code" +
            $"&redirect_uri={Uri.EscapeDataString(OAuthDesktop)}" +
            "&response_mode=query&prompt=select_account";
        var window = new OAuthLoginWindow(authUrl, OAuthDesktop);
        return await window.GetCodeAsync();
    }

    private static async Task<string?> ExchangeCodeAsync(string code)
    {
        var form = new Dictionary<string, string>
        {
            ["client_id"] = ClientId,
            ["code"] = code,
            ["grant_type"] = "authorization_code",
            ["redirect_uri"] = OAuthDesktop,
            ["scope"] = Scope
        };
        var req = new HttpRequestMessage(HttpMethod.Post, OAuthToken) { Content = new FormUrlEncodedContent(form) };
        req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
        using var resp = await SignedHttp.SendAsync(req);
        var rb = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) { Diag($"oauth token {(int)resp.StatusCode}: {Trunc(rb)}"); return null; }
        return JsonDocument.Parse(rb).RootElement.GetProperty("access_token").GetString();
    }

    // Device token ProofOfPossession anonimo — o tipo exigido pelo SISU.
    private static async Task<string?> GetPopDeviceTokenAsync(PopCryptoProvider pop)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new
            {
                AuthMethod = "ProofOfPossession",
                Id = "{" + Guid.NewGuid() + "}",
                DeviceType = "Win32",
                SerialNumber = "{" + Guid.NewGuid() + "}",
                Version = "0.0.0",
                ProofKey = pop.ProofKey
            },
            RelyingParty = DeviceRp,
            TokenType = "JWT"
        });
        var req = new HttpRequestMessage(HttpMethod.Post, DeviceUrl) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        req.Headers.Add("Signature", pop.SignRequest("POST", DeviceUrl, "", body));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var resp = await SignedHttp.SendAsync(req);
        var rb = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) { Diag($"pop device {(int)resp.StatusCode}: {Trunc(rb)}"); return null; }
        return JsonDocument.Parse(rb).RootElement.GetProperty("Token").GetString();
    }

    // SISU /authorize — assinado, sem x-xbl-contract-version. Retorna o authorization
    // token e os tokens device/title/user subjacentes.
    private static async Task<(string? xbl, string? xuid, string? uhs, string? userToken, string? titleToken)> SisuAuthorizeAsync(string accessToken, string deviceToken, PopCryptoProvider pop)
    {
        try
        {
            var body = JsonSerializer.Serialize(new
            {
                AccessToken = "t=" + accessToken,
                AppId = ClientId,
                DeviceToken = deviceToken,
                Sandbox = "RETAIL",
                UseModernGamertag = true,
                SiteName = "user.auth.xboxlive.com",
                RelyingParty = XboxRp,
                ProofKey = pop.ProofKey
            });
            var req = new HttpRequestMessage(HttpMethod.Post, SisuUrl) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
            req.Headers.Add("Signature", pop.SignRequest("POST", SisuUrl, "", body));
            req.Headers.TryAddWithoutValidation("User-Agent", UserAgent);
            req.Headers.Add("Accept", "application/json");
            using var resp = await SignedHttp.SendAsync(req);
            var rb = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode) { Diag($"SISU {(int)resp.StatusCode}: {Trunc(rb)}"); return (null, null, null, null, null); }
            var root = JsonDocument.Parse(rb).RootElement;
            var auth = root.GetProperty("AuthorizationToken");
            var token = auth.GetProperty("Token").GetString();
            var xui = auth.GetProperty("DisplayClaims").GetProperty("xui")[0];
            var uhs = xui.GetProperty("uhs").GetString();
            var xuid = xui.TryGetProperty("xid", out var x) ? x.GetString() : null;
            var userToken = root.TryGetProperty("UserToken", out var ut) && ut.TryGetProperty("Token", out var utt) ? utt.GetString() : null;
            var titleToken = root.TryGetProperty("TitleToken", out var tt) && tt.TryGetProperty("Token", out var ttt) ? ttt.GetString() : null;
            return ($"XBL3.0 x={uhs};{token}", xuid, uhs, userToken, titleToken);
        }
        catch (Exception e) { Diag("SISU exception: " + e.Message); return (null, null, null, null, null); }
    }

    public static string? GetXblToken() => IsLoggedIn ? _cachedXblToken : null;
    public static string? GetEventsToken() => IsLoggedIn ? _cachedEventsToken : null;
    // Token usado pelo spoofer (atualmente o xauth principal).
    public static string? GetSpoofToken() => IsLoggedIn ? (_cachedSpoofToken ?? _cachedXblToken) : null;

    public static string? SignRequest(string method, string uri, string body) =>
        _pop?.SignRequest(method, uri, _cachedXblToken ?? "", body);

    // O campo token da assinatura precisa bater com o header Authorization enviado pelo spoofer.
    public static string? SignSpoofRequest(string method, string uri, string body) =>
        _pop?.SignRequest(method, uri, GetSpoofToken() ?? "", body);

    // Device token: AuthMethod RPS, atrelado ao ticket MSA do usuario e a chave PoP.
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
        using var resp = await SignedHttp.SendAsync(req);
        var rb = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) { Diag($"device {(int)resp.StatusCode}: {Trunc(rb)}"); return null; }
        return JsonDocument.Parse(rb).RootElement.GetProperty("Token").GetString();
    }

    // Title token via title.auth (RPS + device token, assinado). Carrega title claim
    // para o XSTS autorizar writes de achievement. Opcional: null em caso de falha.
    private static async Task<string?> GetTitleTokenAsync(string msaToken, string deviceToken, PopCryptoProvider pop)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { AuthMethod = "RPS", DeviceToken = deviceToken, RpsTicket = "t=" + msaToken, SiteName = "user.auth.xboxlive.com", ProofKey = pop.ProofKey },
            RelyingParty = DeviceRp,
            TokenType = "JWT"
        });
        var req = new HttpRequestMessage(HttpMethod.Post, TitleAuthUrl) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        req.Headers.Add("Signature", pop.SignRequest("POST", TitleAuthUrl, "", body));
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var resp = await SignedHttp.SendAsync(req);
        var rb = await resp.Content.ReadAsStringAsync();
        if (!resp.IsSuccessStatusCode) { Diag($"title.auth {(int)resp.StatusCode}: {Trunc(rb)}"); return null; }
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

    private static async Task<(string? token, string? xuid, string? uhs)> GetXstsAsync(string userToken, string deviceToken, string rp, string? titleToken = null)
    {
        object props = string.IsNullOrEmpty(titleToken)
            ? new { SandboxId = "RETAIL", UserTokens = new[] { userToken }, DeviceToken = deviceToken }
            : new { SandboxId = "RETAIL", UserTokens = new[] { userToken }, DeviceToken = deviceToken, TitleToken = titleToken };
        var body = JsonSerializer.Serialize(new { Properties = props, RelyingParty = rp, TokenType = "JWT" });
        var (code, resp) = await PostAsync(XstsUrl, body);
        if (code != 200) { Diag($"XSTS({rp}) {code}: {Trunc(resp)}"); return (null, null, null); }
        var d = JsonDocument.Parse(resp).RootElement;
        var xui = d.GetProperty("DisplayClaims").GetProperty("xui")[0];
        return (d.GetProperty("Token").GetString(),
                xui.TryGetProperty("xid", out var x) ? x.GetString() : null,
                xui.TryGetProperty("uhs", out var u) ? u.GetString() : null);
    }

    private static string Trunc(string s) => s.Length > 400 ? s.Substring(0, 400) : s;

    private static async Task<(int code, string body)> PostAsync(string url, string json)
    {
        using var resp = await Http.PostAsync(url, new StringContent(json, Encoding.UTF8, "application/json"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }
}

// Proof-of-possession EC P-256: par de chaves efemero, ProofKey JWK, assinatura ES256.
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
