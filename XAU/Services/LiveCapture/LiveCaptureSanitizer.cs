using System.Text.RegularExpressions;

namespace XAU.Services.LiveCapture;

internal static partial class LiveCaptureSanitizer
{
    public static string Sanitize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return "";

        var sanitized = JsonSensitiveValueRegex().Replace(value, "\"$1\":\"<redacted>\"");
        sanitized = AuthorizationRegex().Replace(sanitized, "$1=<redacted>");
        sanitized = XblTokenRegex().Replace(sanitized, "XBL3.0 x=<redacted>");
        sanitized = JwtRegex().Replace(sanitized, "<jwt-redacted>");
        sanitized = SensitiveValueRegex().Replace(sanitized, "$1=<redacted>");
        return sanitized.Length <= 2000 ? sanitized : sanitized[..2000] + " <truncated>";
    }

    [GeneratedRegex("(?i)\\b(authorization|x-authorization)\\b\\s*[:=]\\s*[^\\r\\n]+")]
    private static partial Regex AuthorizationRegex();

    [GeneratedRegex("(?i)\\b(token|xauth|xuid|device(?:id)?|user(?:id)?|signature)\\b\\s*[:=]\\s*[^\\s,;]+")]
    private static partial Regex SensitiveValueRegex();

    [GeneratedRegex("(?i)[\"'](authorization|x-authorization|token|xauth|xuid|device(?:id)?|user(?:id)?|signature)[\"']\\s*:\\s*[\"'][^\"']*[\"']")]
    private static partial Regex JsonSensitiveValueRegex();

    [GeneratedRegex("(?i)XBL3\\.0\\s+x=[^\\s\\\"']+")]
    private static partial Regex XblTokenRegex();

    [GeneratedRegex("\\beyJ[A-Za-z0-9_-]{20,}\\.[A-Za-z0-9_-]{10,}(?:\\.[A-Za-z0-9_-]{10,})?\\b")]
    private static partial Regex JwtRegex();
}
