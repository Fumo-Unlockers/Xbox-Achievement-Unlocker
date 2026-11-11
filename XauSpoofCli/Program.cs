using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using XAU.Services.Spoofer;

namespace XauSpoofCli
{
    /// <summary>
    /// Headless spoofer CLI. Shares PresenceSpoofClient/XauTokenScanner with the GUI app.
    ///
    ///   scan                         attach to the Xbox app and print token candidates (freq, length)
    ///   probe [--title ID]           run one full presence/heartbeat chain, print status+body per URI
    ///                                [--token X | env XAU_XAUTH]  token; default: scan the Xbox app
    ///                                [--xuid N]                   default: resolve from profile API
    ///   run   --title ID             headless spoof: chain every 300s until Ctrl+C, then DELETE stop
    ///                                [--interval S] heartbeat interval (default 300)
    ///
    /// Examples:
    ///   XauSpoofCli probe
    ///   XauSpoofCli probe --title 1285166951
    ///   XauSpoofCli run --title 1285166951 --interval 300
    /// </summary>
    internal static class Program
    {
        private static async Task<int> Main(string[] args)
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 2;
            }

            string cmd = args[0].ToLowerInvariant();
            string? title = GetOption(args, "--title") ?? PresenceSpoofClient.TitleIdEso;
            string? tokenArg = GetOption(args, "--token") ?? Environment.GetEnvironmentVariable("XAU_XAUTH");
            string? xuidArg = GetOption(args, "--xuid");

            var cts = new CancellationTokenSource();
            Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

            try
            {
                switch (cmd)
                {
                    case "scan": return await CmdScan();
                    case "probe": return await CmdProbe(tokenArg, xuidArg, title, cts.Token);
                    case "run": return await CmdRun(tokenArg, xuidArg, title, cts.Token);
                    case "stop": return await CmdStop(tokenArg, xuidArg, cts.Token);
                    default:
                        PrintUsage();
                        return 2;
                }
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("[cancelled]");
                return 130;
            }
        }

        private static async Task<int> CmdScan()
        {
            foreach (var proc in new[] { "XboxPcAppFT", "XboxPcApp" })
            {
                var scanner = new XauTokenScanner(proc);
                if (!scanner.Attach())
                {
                    Console.WriteLine($"{proc}: not running");
                    continue;
                }
                Console.WriteLine($"attached: {scanner.AttachedProcess}");
                var candidates = await scanner.ScanAsync(keepRaw: true);
                if (candidates.Count == 0)
                {
                    Console.WriteLine("  no XBL3.0 x= candidates");
                    continue;
                }
                foreach (var (token, freq, plausible) in candidates.Take(10))
                    Console.WriteLine($"  freq={freq} len={token.Length} plausible={plausible} token={Trunc(token, 260)}");
            }
            return 0;
        }

        private static async Task<int> CmdProbe(string? tokenArg, string? xuidArg, string title, CancellationToken ct)
        {
            var (client, token, xuid) = await SetupAsync(tokenArg, xuidArg, ct);
            if (token == null || xuid == null) return 1;

            Console.WriteLine($"probe: xuid={xuid} title={title} tokenLen={PresenceSpoofClient.SanitizeXauth(token).Length}");
            var result = await client.RunChainAsync(token, xuid, title, ct);
            foreach (var a in result.Attempts)
            {
                string status = a.Success ? "OK" : $"HTTP {a.StatusCode}{(a.Error.Length > 0 ? " " + a.Error : "")}";
                Console.WriteLine($"{a.Name,-18} {status}");
                Console.WriteLine($"    url:  {a.Url}");
                if (!string.IsNullOrEmpty(a.ResponseBody))
                    Console.WriteLine($"    body: {Trunc(a.ResponseBody, 500)}");
            }
            Console.WriteLine(result.Success ? $"SUCCESS via {result.Winning!.Name}" : "ALL ATTEMPTS FAILED");
            return result.Success ? 0 : 1;
        }

        private static async Task<int> CmdStop(string? tokenArg, string? xuidArg, CancellationToken ct)
        {
            var (client, token, xuid) = await SetupAsync(tokenArg, xuidArg, ct);
            if (token == null || xuid == null) return 1;
            await client.StopAsync(token, xuid, ct);
            Console.WriteLine("stop sent (all presence/heartbeat variants cleared)");
            return 0;
        }

        private static async Task<int> CmdRun(string? tokenArg, string? xuidArg, string title, CancellationToken ct)
        {
            var (client, token, xuid) = await SetupAsync(tokenArg, xuidArg, ct);
            if (token == null || xuid == null) return 1;

            Console.WriteLine($"headless spoof: xuid={xuid} title={title} start={DateTime.Now:HH:mm:ss}");
            var startedUtc = DateTime.UtcNow;
            while (!ct.IsCancellationRequested)
            {
                var result = await client.RunChainAsync(token, xuid, title, ct);
                if (result.Success)
                {
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} heartbeat OK via {result.Winning!.Name} (next in 300s)");
                }
                else
                {
                    foreach (var a in result.Attempts)
                        Console.WriteLine($"{DateTime.Now:HH:mm:ss} {a.Name}: HTTP {a.StatusCode} {Trunc(a.ResponseBody, 300)}");
                    Console.WriteLine($"{DateTime.Now:HH:mm:ss} chain failed; aborting");
                    break;
                }
                try { await Task.Delay(TimeSpan.FromSeconds(300), ct); }
                catch (OperationCanceledException) { }
            }
            Console.WriteLine($"spoof ended at {DateTime.Now:HH:mm:ss} (ran {(DateTime.UtcNow - startedUtc).TotalMinutes:F1} min); sending stop...");
            await client.StopAsync(token, xuid, CancellationToken.None);
            Console.WriteLine("stop sent");
            return 0;
        }

        private static async Task<(PresenceSpoofClient client, string? token, string? xuid)> SetupAsync(
            string? tokenArg, string? xuidArg, CancellationToken ct)
        {
            var client = new PresenceSpoofClient();
            string token = PresenceSpoofClient.SanitizeXauth(tokenArg);
            string? xuid = xuidArg;
            if (string.IsNullOrEmpty(token))
            {
                Console.WriteLine("no --token / XAU_XAUTH given; scanning the Xbox app memory...");
                var candidates = new List<(string token, int frequency, bool plausible)>();
                XauTokenScanner? scanner = null;
                foreach (var proc in new[] { "XboxPcAppFT", "XboxPcApp" })
                {
                    var s = new XauTokenScanner(proc);
                    if (!s.Attach())
                        continue;
                    scanner = s;
                    Console.WriteLine($"attached: {s.AttachedProcess}");
                    candidates.AddRange(await s.ScanAsync(keepRaw: true));
                }
                if (scanner == null)
                {
                    Console.WriteLine("ERROR: could not attach to XboxPcApp / XboxPcAppFT");
                    return (client, null, null);
                }
                candidates = candidates.OrderByDescending(c => (c.plausible, c.frequency)).ToList();
                if (candidates.Count == 0)
                {
                    Console.WriteLine("ERROR: no XBL3.0 x= candidates in Xbox app memory");
                    return (client, null, null);
                }
                Console.WriteLine($"candidates: {string.Join(", ", candidates.Select(c => $"len={c.token.Length} freq={c.frequency} pl={c.plausible}"))}");
                // Reads can be truncated or carry trailing memory garbage; sanitize each raw
                // candidate and validate against the profile API, keeping the first that
                // authenticates (same referee the GUI uses via TestXAUTH).
                xuid = xuidArg;
                if (string.IsNullOrWhiteSpace(xuid))
                {
                    foreach (var c in candidates)
                    {
                        var sanitized = PresenceSpoofClient.SanitizeXauth(c.token);
                        if (sanitized != c.token)
                            Console.WriteLine($"  (sanitized len {c.token.Length} -> {sanitized.Length})");
                        Console.WriteLine($"resolving xuid with candidate len={sanitized.Length}...");
                        xuid = await client.ResolveXuidAsync(sanitized, ct);
                        if (!string.IsNullOrWhiteSpace(xuid))
                        {
                            token = sanitized;
                            Console.WriteLine($"xuid={xuid} (candidate len={sanitized.Length} authenticated)");
                            break;
                        }
                        Console.WriteLine("  rejected by profile API");
                    }
                }
                if (string.IsNullOrWhiteSpace(xuid))
                {
                    Console.WriteLine("ERROR: no candidate authenticated against the profile API");
                    return (client, null, null);
                }
                return (client, token, xuid);
            }

            if (string.IsNullOrWhiteSpace(xuid))
            {
                Console.WriteLine("resolving xuid from profile API...");
                xuid = await client.ResolveXuidAsync(token, ct);
                if (string.IsNullOrWhiteSpace(xuid))
                {
                    Console.WriteLine("ERROR: could not resolve XUID (token rejected by profile API?)");
                    return (client, null, null);
                }
                Console.WriteLine($"xuid={xuid}");
            }
            return (client, token, xuid);
        }

        private static string? GetOption(string[] args, string name)
        {
            int i = Array.IndexOf(args, name);
            return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
        }

        private static void PrintUsage()
        {
            Console.WriteLine("usage: XauSpoofCli scan | probe [--title ID] [--token T | env XAU_XAUTH] [--xuid N] | run --title ID [--interval S]");
        }

        private static string Trunc(string s, int n) => s.Length <= n ? s : s.Substring(0, n) + "...";
    }
}
