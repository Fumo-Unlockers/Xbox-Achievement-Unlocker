using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace XAU.Services;

internal static partial class StartupDiagnostics
{
    private const long MaxLogBytes = 64 * 1024;
    private static readonly object Sync = new();
    private static readonly string LogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
        "XAU",
        "startup_diagnostics.log");

    public static void Begin()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            lock (Sync)
            {
                File.WriteAllText(
                    LogPath,
                    $"{DateTime.UtcNow:O} [START] XAU {Environment.Version}; {Environment.OSVersion.Version}{Environment.NewLine}");
            }
        }
        catch
        {
            // Diagnostics must never prevent the app from starting.
        }
    }

    public static void Write(string message) => WriteLine("INFO", Sanitize(message));

    public static void WriteException(string phase, Exception exception)
    {
        var details = new List<string>();
        for (var current = exception; current != null; current = current.InnerException!)
        {
            details.Add($"{current.GetType().FullName} (0x{current.HResult:X8}): {Sanitize(current.Message)}");
        }

        WriteLine("ERROR", $"{phase}: {string.Join(" -> ", details)}");
    }

    private static void WriteLine(string level, string message)
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(LogPath)!);
            lock (Sync)
            {
                var line = $"{DateTime.UtcNow:O} [{level}] {message}{Environment.NewLine}";
                var currentLength = File.Exists(LogPath) ? new FileInfo(LogPath).Length : 0;
                if (currentLength + Encoding.UTF8.GetByteCount(line) <= MaxLogBytes)
                {
                    File.AppendAllText(LogPath, line);
                }
            }
        }
        catch
        {
            // Diagnostics must never become a second startup failure.
        }
    }

    private static string Sanitize(string value)
    {
        var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        if (!string.IsNullOrWhiteSpace(userProfile))
        {
            value = value.Replace(userProfile, "%USERPROFILE%", StringComparison.OrdinalIgnoreCase);
        }

        value = AuthorizationHeaderRegex().Replace(value, "$1=<redacted>");
        value = XblTokenRegex().Replace(value, "XBL3.0 x=<redacted>");
        value = JwtRegex().Replace(value, "<jwt-redacted>");
        value = SensitiveValueRegex().Replace(value, "$1=<redacted>");
        return value.Length <= 2000 ? value : value[..2000] + " <truncated>";
    }

    [GeneratedRegex("(?i)\\b(authorization|x-authorization)\\b\\s*[:=]\\s*[^\\r\\n]+")]
    private static partial Regex AuthorizationHeaderRegex();

    [GeneratedRegex("(?i)\\b(authorization|x-authorization|signature|token|xauth|xuid|device(?:id)?|user(?:id)?)\\b\\s*[:=]\\s*[^\\s,;]+")]
    private static partial Regex SensitiveValueRegex();

    [GeneratedRegex("(?i)XBL3\\.0\\s+x=[^\\s\\\"']+")]
    private static partial Regex XblTokenRegex();

    [GeneratedRegex("\\beyJ[A-Za-z0-9_-]{20,}\\.[A-Za-z0-9_-]{10,}(?:\\.[A-Za-z0-9_-]{10,})?\\b")]
    private static partial Regex JwtRegex();
}
