using System.IO;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

namespace XAU.Services;

// Le o cache de tokens do Xbox-app (GDK / GamingServices) do disco. Os arquivos em
// LocalState\Auth sao protegidos via DPAPI-NG para o usuario, entao um processo normal
// decifra com NCryptUnprotectSecret (sem memory scan, sem injecao). O token de
// RP=http://xboxlive.com tem capacidade de presenca, que tokens self-minted nao tem.
public static class GdkTokenService
{
    [DllImport("ncrypt.dll")]
    private static extern int NCryptUnprotectSecret(out IntPtr hDescriptor, uint flags, byte[] blob, uint cb,
        IntPtr memPara, IntPtr hWnd, out IntPtr data, out uint len);

    private const uint NCRYPT_SILENT_FLAG = 0x40;

    private static byte[]? Unprotect(byte[] blob)
    {
        if (NCryptUnprotectSecret(out _, NCRYPT_SILENT_FLAG, blob, (uint)blob.Length, IntPtr.Zero, IntPtr.Zero, out var data, out var len) != 0)
            return null;
        var o = new byte[len];
        Marshal.Copy(data, o, 0, (int)len);
        return o;
    }

    public sealed record XToken(string Xbl, string Xuid, string Uhs, string Gamertag, DateTime NotAfter);
    public sealed record UserToken(string Token, DateTime NotAfter);

    // User token enrolled mais recente (RP auth.xboxlive.com). Vive ~dias — usado para
    // re-gerar XSTS (fallback de xauth + events token) sem o app.
    public static UserToken? GetUserToken()
    {
        if (!Directory.Exists(AuthDir)) return null;
        UserToken? best = null;
        foreach (var file in Directory.EnumerateFiles(AuthDir, "*", SearchOption.AllDirectories))
        {
            byte[]? dec;
            try { dec = Unprotect(File.ReadAllBytes(file)); }
            catch { continue; }
            if (dec == null) continue;
            try
            {
                var root = JsonDocument.Parse(Encoding.UTF8.GetString(dec)).RootElement;
                if (!root.TryGetProperty("IdentityType", out var it) || it.GetString() != "Utoken") continue;
                var td = root.GetProperty("TokenData");
                var token = td.GetProperty("Token").GetString();
                var notAfter = td.TryGetProperty("NotAfter", out var na) && na.TryGetDateTime(out var dt) ? dt.ToUniversalTime() : DateTime.MinValue;
                if (notAfter < DateTime.UtcNow || string.IsNullOrEmpty(token)) continue;
                if (best == null || notAfter > best.NotAfter) best = new UserToken(token, notAfter);
            }
            catch { }
        }
        return best;
    }

    private static string AuthDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "Packages", "Microsoft.GamingServices_8wekyb3d8bbwe", "LocalState", "Auth");

    public static bool CacheExists => Directory.Exists(AuthDir);

    private static readonly HttpClient Http = new();

    // Events token de uma conta, gerado do user token em cache (Utoken). O Utoken nao
    // tem xid, entao cada candidato e identificado gerando um XSTS xboxlive.com e
    // casando o xid com o alvo; depois gera o XSTS events.xboxlive.com do mesmo token.
    public static async Task<string?> MintEventsTokenAsync(string xuid, string uhs)
    {
        foreach (var ut in GetValidUserTokens())
        {
            try
            {
                var (c1, b1) = await PostXstsAsync(ut, "http://xboxlive.com");
                if (c1 != 200) continue;
                var xui = JsonDocument.Parse(b1).RootElement.GetProperty("DisplayClaims").GetProperty("xui")[0];
                var xid = xui.TryGetProperty("xid", out var x) ? x.GetString() : null;
                if (xid != xuid) continue;

                var (c2, b2) = await PostXstsAsync(ut, "http://events.xboxlive.com");
                if (c2 != 200) return null;
                var tok = JsonDocument.Parse(b2).RootElement.GetProperty("Token").GetString();
                return string.IsNullOrEmpty(tok) ? null : $"x:XBL3.0 x={uhs};{tok}";
            }
            catch { }
        }
        return null;
    }

    private static async Task<(int code, string body)> PostXstsAsync(string userToken, string rp)
    {
        var body = JsonSerializer.Serialize(new
        {
            Properties = new { SandboxId = "RETAIL", UserTokens = new[] { userToken } },
            RelyingParty = rp,
            TokenType = "JWT"
        });
        using var resp = await Http.PostAsync("https://xsts.auth.xboxlive.com/xsts/authorize",
            new StringContent(body, Encoding.UTF8, "application/json"));
        return ((int)resp.StatusCode, await resp.Content.ReadAsStringAsync());
    }

    // Todos os user tokens validos em cache (Utoken, RP auth.xboxlive.com), um por conta.
    private static List<string> GetValidUserTokens()
    {
        var list = new List<string>();
        if (!Directory.Exists(AuthDir)) return list;
        foreach (var file in Directory.EnumerateFiles(AuthDir, "*", SearchOption.AllDirectories))
        {
            byte[]? dec;
            try { dec = Unprotect(File.ReadAllBytes(file)); }
            catch { continue; }
            if (dec == null) continue;
            try
            {
                var root = JsonDocument.Parse(Encoding.UTF8.GetString(dec)).RootElement;
                if (!root.TryGetProperty("IdentityType", out var it) || it.GetString() != "Utoken") continue;
                if (!root.TryGetProperty("RelyingParty", out var rp) || rp.GetString() != "http://auth.xboxlive.com") continue;
                var td = root.GetProperty("TokenData");
                var notAfter = td.TryGetProperty("NotAfter", out var na) && na.TryGetDateTime(out var dt) ? dt.ToUniversalTime() : DateTime.MinValue;
                if (notAfter < DateTime.UtcNow) continue;
                var tok = td.GetProperty("Token").GetString();
                if (!string.IsNullOrEmpty(tok)) list.Add(tok!);
            }
            catch { }
        }
        return list;
    }

    // Todos os XSTS validos (RP xboxlive.com) em cache do Xbox app, mais recentes primeiro.
    // Um por conta logada.
    public static List<XToken> GetXboxLiveTokens()
    {
        var result = new List<XToken>();
        if (!Directory.Exists(AuthDir)) return result;

        foreach (var file in Directory.EnumerateFiles(AuthDir, "*", SearchOption.AllDirectories))
        {
            byte[]? dec;
            try { dec = Unprotect(File.ReadAllBytes(file)); }
            catch { continue; }
            if (dec == null) continue;

            try
            {
                var root = JsonDocument.Parse(Encoding.UTF8.GetString(dec)).RootElement;
                if (!root.TryGetProperty("IdentityType", out var it) || it.GetString() != "Xtoken") continue;
                if (!root.TryGetProperty("RelyingParty", out var rpEl) || rpEl.GetString() != "http://xboxlive.com") continue;
                if (!root.TryGetProperty("TokenData", out var td)) continue;

                var token = td.GetProperty("Token").GetString();
                var notAfter = td.TryGetProperty("NotAfter", out var na) && na.TryGetDateTime(out var dt) ? dt.ToUniversalTime() : DateTime.MinValue;
                if (notAfter < DateTime.UtcNow) continue;

                var xui = td.GetProperty("DisplayClaims").GetProperty("xui")[0];
                var uhs = xui.TryGetProperty("uhs", out var u) ? u.GetString() ?? "" : "";
                var xid = xui.TryGetProperty("xid", out var x) ? x.GetString() ?? "0" : "0";
                var gtg = xui.TryGetProperty("gtg", out var g) ? g.GetString() ?? "" : "";
                if (xid == "0" || string.IsNullOrEmpty(token) || string.IsNullOrEmpty(uhs)) continue;

                result.Add(new XToken($"XBL3.0 x={uhs};{token}", xid, uhs, gtg, notAfter));
            }
            catch { }
        }

        // Distinto por xuid, token mais recente por conta.
        return result
            .GroupBy(t => t.Xuid)
            .Select(grp => grp.OrderByDescending(t => t.NotAfter).First())
            .OrderByDescending(t => t.NotAfter)
            .ToList();
    }
}
