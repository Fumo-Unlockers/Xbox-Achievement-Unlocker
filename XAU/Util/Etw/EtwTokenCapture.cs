using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using XAU.ViewModels.Pages;

namespace XAU.Util.Etw
{
    public static class EtwTokenCapture
    {
        public sealed class QuantumBreakTelemetryEvent
        {
            public string EventName { get; init; } = "";
            public string EventTime { get; init; } = "";
            public string ProgressionData { get; init; } = "";
            public string Properties { get; init; } = "{}";
            public string Measurements { get; init; } = "{}";
            public string SanitizedPayload { get; init; } = "";
        }

        private static readonly string EtwSessionName = "XAU_EventsTokenCapture";
        private static readonly string EtwTempDir = Path.Combine(Path.GetTempPath(), "XAU_ETW");
        private static readonly string EtwEtlPath = Path.Combine(EtwTempDir, "capture.etl");

        private static readonly Regex TicketHeaderRegex = new Regex(
            @"""(\d{5,12})""\s*=\s*""(x:XBL3\.0 x=[^""]{100,})""",
            RegexOptions.Compiled);
        private static readonly Regex BareTokenRegex = new Regex(
            @"x:XBL3\.0 x=[\w;+/=\-\.]{100,}",
            RegexOptions.Compiled);
        private static readonly Regex OneCollectorUrlRegex = new Regex(
            @"v20\.events\.data\.microsoft\.com|OneCollector",
            RegexOptions.Compiled);
        private static readonly Regex SensitiveTelemetryKeyRegex = new Regex(
            @"token|auth|ticket|xuid|user.?id|device.?id|local.?id|session.?id|gamertag|email",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex SensitiveTelemetryValueRegex = new Regex(
            @"XBL3\.0|x:XBL|eyJ[A-Za-z0-9_-]{20,}|^[0-9]{16,}$|^[0-9a-f]{8}-[0-9a-f-]{27,}$",
            RegexOptions.Compiled | RegexOptions.IgnoreCase);

        // Events RP x5t — used to distinguish events tokens from XAUTH tokens.
        // This is the certificate thumbprint for events.xboxlive.com; it appears
        // in the decoded JWE header JSON as "x5t":"9wLGzMJDNz..."
        private const string EventsRpX5t = "9wLGzMJDNz";

        /// <summary>
        /// Checks whether a token was encrypted for the events RP by decoding
        /// the JWE header and verifying the x5t certificate thumbprint.
        /// Token format: "x:XBL3.0 x={hash};{JWE}" or "XBL3.0 x={hash};{JWE}"
        /// </summary>
        public static bool IsEventsRpToken(string token)
        {
            try
            {
                int semiIdx = token.IndexOf(';');
                if (semiIdx < 0) return false;

                string jwe = token.Substring(semiIdx + 1);
                int dotIdx = jwe.IndexOf('.');
                if (dotIdx <= 0) return false;

                string headerB64 = jwe.Substring(0, dotIdx);
                // Base64url → standard Base64
                string padded = headerB64.Replace('-', '+').Replace('_', '/');
                switch (padded.Length % 4)
                {
                    case 2: padded += "=="; break;
                    case 3: padded += "="; break;
                }

                string headerJson = Encoding.UTF8.GetString(Convert.FromBase64String(padded));
                return headerJson.Contains(EventsRpX5t);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// One-shot: start trace, wait, stop, extract, cleanup. Returns token or null.
        /// </summary>
        public static string Capture(int captureSeconds)
        {
            HomeViewModel.EventsLog($"Starting ETW capture for {captureSeconds}s...");

            Cleanup();

            string method = Start();
            if (method == null)
            {
                HomeViewModel.EventsLog("Failed to start ETW trace (not running as admin?)");
                return null;
            }
            HomeViewModel.EventsLog($"ETW started via {method}");

            Thread.Sleep(captureSeconds * 1000);

            Stop(method);

            // Give it a moment to flush
            Thread.Sleep(2000);

            string token = ExtractTokens();

            CleanupFiles();

            return token;
        }

        /// <summary>
        /// Captures a bounded ETW trace and returns sanitized Quantum Break
        /// ProgressionEvent payloads. Raw payloads and identity fields are never returned.
        /// </summary>
        public static List<QuantumBreakTelemetryEvent> CaptureQuantumBreakTelemetry(int captureSeconds)
        {
            Cleanup();

            string method = Start();
            if (method == null)
                throw new InvalidOperationException("Failed to start ETW trace. Run XAU as administrator and try again.");

            try
            {
                Thread.Sleep(captureSeconds * 1000);
                Stop(method);
                method = null;
                Thread.Sleep(2000);
                return ExtractQuantumBreakTelemetryEvents();
            }
            finally
            {
                if (method != null)
                    Stop(method);
                CleanupFiles();
            }
        }

        public static void Cleanup()
        {
            try { RunShellCommand("netsh", "trace stop", 15000); } catch { }
            try { RunShellCommand("logman", $"stop {EtwSessionName} -ets", 10000); } catch { }
        }

        public static string Start()
        {
            Directory.CreateDirectory(EtwTempDir);

            // Try netsh trace (captures most HTTP traffic including WinHTTP)
            var (code, stdout, stderr) = RunShellCommand("netsh",
                $"trace start scenario=InternetClient_dbg capture=no tracefile=\"{EtwEtlPath}\" maxsize=256 overwrite=yes report=disabled",
                15000);
            HomeViewModel.EventsLog($"netsh trace start: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "netsh";

            // Try logman with WinHttp provider
            (code, stdout, stderr) = RunShellCommand("logman",
                $"start {EtwSessionName} -p Microsoft-Windows-WinHttp 0xFFFFFFFF 0xFF -o \"{EtwEtlPath}\" -ets",
                10000);
            HomeViewModel.EventsLog($"logman WinHttp: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "logman-winhttp";

            // Try logman with WinINet provider
            (code, stdout, stderr) = RunShellCommand("logman",
                $"start {EtwSessionName} -p Microsoft-Windows-WinINet 0xFFFFFFFF 0xFF -o \"{EtwEtlPath}\" -ets",
                10000);
            HomeViewModel.EventsLog($"logman WinINet: exit={code}, stdout={stdout.Trim()}, stderr={stderr.Trim()}");
            if (code == 0)
                return "logman-wininet";

            HomeViewModel.EventsLog("All ETW start methods failed");
            return null;
        }

        public static void Stop(string method)
        {
            if (method == "netsh")
            {
                var (code, _, _) = RunShellCommand("netsh", "trace stop", 30000);
                HomeViewModel.EventsLog($"netsh trace stop: exit={code}");
            }
            else if (method != null)
            {
                var (code, _, _) = RunShellCommand("logman", $"stop {EtwSessionName} -ets", 15000);
                HomeViewModel.EventsLog($"logman stop: exit={code}");
            }
        }

        public static string ExtractTokens()
        {
            if (!File.Exists(EtwEtlPath))
            {
                HomeViewModel.EventsLog("ETL file not found");
                return null;
            }

            var fileSize = new FileInfo(EtwEtlPath).Length;
            HomeViewModel.EventsLog($"ETL file size: {fileSize / 1024}KB");

            const int chunkSize = 64 * 1024 * 1024; // 64MB
            const int overlap = 8 * 1024; // 8KB overlap
            var candidates = new List<(string token, int score)>();

            int totalXblHits = 0;

            using (var fs = new FileStream(EtwEtlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
            {
                byte[] buffer = new byte[chunkSize + overlap];
                long position = 0;
                int chunkNum = 0;

                while (position < fs.Length)
                {
                    fs.Position = position;
                    int bytesRead = fs.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0) break;

                    // Work directly from buffer (avoid extra copy)
                    string ascii = Encoding.ASCII.GetString(buffer, 0, bytesRead);

                    // Diagnostic: count raw "XBL3.0" occurrences
                    int xblCount = 0;
                    int searchIdx = 0;
                    while ((searchIdx = ascii.IndexOf("XBL3.0", searchIdx, StringComparison.Ordinal)) >= 0)
                    {
                        xblCount++;
                        searchIdx += 6;
                    }
                    totalXblHits += xblCount;
                    HomeViewModel.EventsLog($"Chunk {chunkNum}: {bytesRead / 1024}KB, XBL3.0 hits={xblCount}, regex searching...");

                    SearchForTokens(ascii, candidates);

                    // UTF-16 → strip null bytes to get ASCII
                    string stripped = StripNullBytes(buffer, bytesRead);
                    SearchForTokens(stripped, candidates);

                    HomeViewModel.EventsLog($"Chunk {chunkNum}: candidates so far={candidates.Count}");

                    position += chunkSize;
                    chunkNum++;
                }
            }

            HomeViewModel.EventsLog($"Total XBL3.0 hits across all chunks: {totalXblHits}");

            if (candidates.Count == 0)
            {
                HomeViewModel.EventsLog($"No regex candidates found (XBL3.0 hits={totalXblHits}). Trying IndexOf fallback...");

                // Fallback: re-read and use IndexOf to extract tokens directly
                if (totalXblHits > 0)
                {
                    using var fs2 = new FileStream(EtwEtlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    byte[] allBytes = new byte[fs2.Length];
                    fs2.Read(allBytes, 0, allBytes.Length);
                    string stripped = StripNullBytes(allBytes, allBytes.Length);

                    string marker = "x:XBL3.0 x=";
                    int idx = 0;
                    int found = 0;
                    while ((idx = stripped.IndexOf(marker, idx, StringComparison.Ordinal)) >= 0)
                    {
                        // Extract up to 4000 chars from this position
                        int maxLen = Math.Min(4000, stripped.Length - idx);
                        string raw = stripped.Substring(idx, maxLen);
                        string token = CleanToken(raw);
                        if (token != null)
                        {
                            int score = ScoreCandidate(stripped, idx, token, null);
                            candidates.Add((token, score));
                            found++;
                            HomeViewModel.EventsLog($"IndexOf fallback found token: len={token.Length}, score={score}");
                        }
                        else
                        {
                            // Log why CleanToken rejected it
                            int endSnip = Math.Min(80, raw.Length);
                            HomeViewModel.EventsLog($"IndexOf hit rejected by CleanToken at pos={idx}, start: {raw.Substring(0, endSnip)}");
                        }
                        idx += marker.Length;
                    }
                    HomeViewModel.EventsLog($"IndexOf fallback: {found} tokens from {totalXblHits} XBL3.0 hits");
                }

                if (candidates.Count == 0)
                {
                    HomeViewModel.EventsLog("No token candidates found in ETL");
                    return null;
                }
            }

            // Sort by score descending
            candidates.Sort((a, b) => b.score.CompareTo(a.score));

            HomeViewModel.EventsLog($"Found {candidates.Count} candidate(s):");
            foreach (var (token, score) in candidates.Take(5))
            {
                int semi = token.IndexOf(';');
                string hash = semi > 0 ? token.Substring(token.IndexOf("x=") + 2, semi - token.IndexOf("x=") - 2) : "?";
                HomeViewModel.EventsLog($"  score={score}, len={token.Length}, hash={hash}");
            }

            // Validate the best candidates
            foreach (var (token, score) in candidates)
            {
                if (IsEventsRpToken(token))
                {
                    HomeViewModel.EventsLog($"Candidate validated (x5t check passed), score={score}, len={token.Length}");
                    return token;
                }
            }

            HomeViewModel.EventsLog("No candidate passed x5t validation");
            return null;
        }

        private static List<QuantumBreakTelemetryEvent> ExtractQuantumBreakTelemetryEvents()
        {
            if (!File.Exists(EtwEtlPath))
                return [];

            const int chunkSize = 16 * 1024 * 1024;
            const int overlap = 256 * 1024;
            var events = new Dictionary<string, QuantumBreakTelemetryEvent>();

            using var fs = new FileStream(EtwEtlPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var buffer = new byte[chunkSize + overlap];
            long position = 0;
            while (position < fs.Length)
            {
                fs.Position = position;
                int bytesRead = fs.Read(buffer, 0, buffer.Length);
                if (bytesRead == 0)
                    break;

                FindQuantumBreakTelemetryEvents(Encoding.ASCII.GetString(buffer, 0, bytesRead), events);
                FindQuantumBreakTelemetryEvents(StripNullBytes(buffer, bytesRead), events);
                position += chunkSize;
            }

            return events.Values.ToList();
        }

        private static void FindQuantumBreakTelemetryEvents(
            string text,
            Dictionary<string, QuantumBreakTelemetryEvent> events)
        {
            const string marker = "Microsoft.XboxLive.T333628240.ProgressionEvent";
            int markerIndex = 0;
            while ((markerIndex = text.IndexOf(marker, markerIndex, StringComparison.Ordinal)) >= 0)
            {
                int searchStart = Math.Max(0, markerIndex - 128 * 1024);
                int objectStart = markerIndex;
                while (objectStart >= searchStart)
                {
                    objectStart = text.LastIndexOf('{', objectStart);
                    if (objectStart < searchStart)
                        break;
                    if (TryReadJsonObject(text, objectStart, out var json) &&
                        TryCreateQuantumBreakTelemetryEvent(json, out var capturedEvent))
                    {
                        events.TryAdd(capturedEvent.SanitizedPayload, capturedEvent);
                        break;
                    }
                    objectStart--;
                }
                markerIndex += marker.Length;
            }
        }

        private static bool TryReadJsonObject(string text, int start, out string json)
        {
            json = "";
            int depth = 0;
            bool inString = false;
            bool escaped = false;
            for (int i = start; i < text.Length; i++)
            {
                char current = text[i];
                if (inString)
                {
                    if (escaped)
                        escaped = false;
                    else if (current == '\\')
                        escaped = true;
                    else if (current == '"')
                        inString = false;
                    continue;
                }

                if (current == '"')
                    inString = true;
                else if (current == '{')
                    depth++;
                else if (current == '}' && --depth == 0)
                {
                    json = text.Substring(start, i - start + 1);
                    return true;
                }
            }
            return false;
        }

        private static bool TryCreateQuantumBreakTelemetryEvent(
            string json,
            out QuantumBreakTelemetryEvent capturedEvent)
        {
            capturedEvent = null;
            try
            {
                var root = JObject.Parse(json);
                if (root["data"]?["baseData"] is not JObject baseData ||
                    baseData["titleId"]?.ToString() != "333628240" ||
                    baseData["name"]?.ToString() != "ProgressionEvent")
                    return false;

                var properties = SanitizeTelemetryToken(baseData["properties"] ?? new JObject());
                var measurements = SanitizeTelemetryToken(baseData["measurements"] ?? new JObject());
                var sanitized = new JObject
                {
                    ["name"] = root["name"],
                    ["time"] = root["time"],
                    ["data"] = new JObject
                    {
                        ["baseType"] = root["data"]?["baseType"],
                        ["baseData"] = new JObject
                        {
                            ["name"] = baseData["name"],
                            ["serviceConfigId"] = baseData["serviceConfigId"],
                            ["titleId"] = baseData["titleId"],
                            ["ver"] = baseData["ver"],
                            ["properties"] = properties,
                            ["measurements"] = measurements
                        }
                    }
                };

                capturedEvent = new QuantumBreakTelemetryEvent
                {
                    EventName = root["name"]?.ToString() ?? baseData["name"]?.ToString() ?? "",
                    EventTime = root["time"]?.ToString() ?? "",
                    ProgressionData = properties["ProgressionData"]?.ToString() ?? "",
                    Properties = properties.ToString(Formatting.None),
                    Measurements = measurements.ToString(Formatting.None),
                    SanitizedPayload = sanitized.ToString(Formatting.Indented)
                };
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static JToken SanitizeTelemetryToken(JToken token)
        {
            if (token is JObject obj)
            {
                var sanitized = new JObject();
                foreach (var property in obj.Properties())
                    sanitized[property.Name] = SensitiveTelemetryKeyRegex.IsMatch(property.Name)
                        ? "[REDACTED]"
                        : SanitizeTelemetryToken(property.Value);
                return sanitized;
            }
            if (token is JArray array)
                return new JArray(array.Select(SanitizeTelemetryToken));
            if (token.Type == JTokenType.String && SensitiveTelemetryValueRegex.IsMatch(token.ToString()))
                return "[REDACTED]";
            return token.DeepClone();
        }

        public static void CleanupFiles()
        {
            try
            {
                if (Directory.Exists(EtwTempDir))
                {
                    foreach (var file in Directory.GetFiles(EtwTempDir))
                    {
                        try { File.Delete(file); } catch { }
                    }
                    try { Directory.Delete(EtwTempDir, true); } catch { }
                }
            }
            catch (Exception ex)
            {
                HomeViewModel.EventsLog($"Cleanup error: {ex.Message}");
            }
        }

        private static (int exitCode, string stdout, string stderr) RunShellCommand(string fileName, string args, int timeoutMs = 30000)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = fileName,
                    Arguments = args,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                };
                using var proc = Process.Start(psi);
                string stdout = proc.StandardOutput.ReadToEnd();
                string stderr = proc.StandardError.ReadToEnd();
                proc.WaitForExit(timeoutMs);
                return (proc.ExitCode, stdout, stderr);
            }
            catch (Exception ex)
            {
                return (-1, "", ex.Message);
            }
        }

        private static void SearchForTokens(string text, List<(string token, int score)> candidates)
        {
            // Search with ticket header pattern (has title ID context)
            foreach (Match match in TicketHeaderRegex.Matches(text))
            {
                string titleId = match.Groups[1].Value;
                string token = CleanToken(match.Groups[2].Value);
                if (token == null) continue;

                int score = ScoreCandidate(text, match.Index, token, titleId);
                candidates.Add((token, score));
            }

            // Search with bare token pattern
            foreach (Match match in BareTokenRegex.Matches(text))
            {
                string token = CleanToken(match.Value);
                if (token == null) continue;

                // Skip if already found via ticket header
                if (candidates.Any(c => c.token == token)) continue;

                int score = ScoreCandidate(text, match.Index, token, null);
                candidates.Add((token, score));
            }
        }

        private static string CleanToken(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return null;

            // Trim at first non-token character
            int end = raw.Length;
            for (int i = 0; i < raw.Length; i++)
            {
                char c = raw[i];
                if (c < 0x20 || c > 0x7E || c == '"' || c == '\'' || c == '<' || c == '>' || c == '{' || c == '}')
                {
                    end = i;
                    break;
                }
            }

            string token = raw.Substring(0, end).TrimEnd();
            if (!token.StartsWith("x:XBL3.0 x=")) return null;
            if (token.Length < 100) return null;
            if (!token.Contains(';')) return null;

            return token;
        }

        private static int ScoreCandidate(string text, int matchIndex, string token, string titleId)
        {
            int score = 0;

            // OneCollector URL proximity (+5000)
            int searchStart = Math.Max(0, matchIndex - 2000);
            int searchLen = Math.Min(4000, text.Length - searchStart);
            string vicinity = text.Substring(searchStart, searchLen);
            if (OneCollectorUrlRegex.IsMatch(vicinity))
                score += 5000;

            // Has ticket ID context (+2000)
            if (!string.IsNullOrEmpty(titleId))
                score += 2000;

            // Proper x:XBL3.0 prefix (+1000)
            if (token.StartsWith("x:XBL3.0 x="))
                score += 1000;

            // Length bonus (longer tokens are more likely complete)
            score += token.Length / 10;

            return score;
        }

        private static string StripNullBytes(byte[] data, int length)
        {
            var sb = new StringBuilder(length / 2);
            for (int i = 0; i < length; i++)
            {
                if (data[i] != 0 && data[i] >= 0x20 && data[i] <= 0x7E)
                    sb.Append((char)data[i]);
            }
            return sb.ToString();
        }
    }
}
