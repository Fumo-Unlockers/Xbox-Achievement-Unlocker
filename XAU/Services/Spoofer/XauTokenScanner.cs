using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Memory;

namespace XAU.Services.Spoofer
{
    /// <summary>
    /// UI-independent XAUTH token extraction from the Xbox app's memory. Mirrors
    /// HomeViewModel.GetXAUTH's approach: attach (XboxPcApp or XboxPcAppFT), AoB-scan both the
    /// ASCII and UTF-16 layouts of "XBL3.0 x=", decode each hit with its matching encoding,
    /// keep strings starting with the prefix, frequency-vote.
    /// </summary>
    public sealed class XauTokenScanner
    {
        private const string AsciiPattern = "58 42 4C 33 2E 30 20 78 3D"; // "XBL3.0 x="
        private const string Utf16Pattern = "58 00 42 00 4C 00 33 00 2E 00 30 00 20 00 78 00 3D 00";

        /// <summary>Real token: "XBL3.0 x={digits};{payload}", no whitespace, plausible size.</summary>
        private static readonly Regex XblTokenShape = new(
            @"^XBL3\.0 x=\d{6,20};[A-Za-z0-9+/_=\-\.]{200,8000}$", RegexOptions.Compiled);

        private readonly string _processName;
        private readonly Mem _mem = new();

        private int _pid;
        public string AttachedProcess => _pid != 0 ? $"{_processName} pid={_pid}" : "";

        public XauTokenScanner(string processName = "XboxPcAppFT") => _processName = processName;

        public bool Attach()
        {
            int pid = _mem.GetProcIdFromName(_processName);
            if (pid != 0 && _mem.OpenProcess(pid, out _))
            {
                _pid = pid;
                return true;
            }
            return false;
        }

        /// <summary>
        /// Scans both encodings, shape-filters, and returns (token, frequency) pairs, best first.
        /// When keepRaw is true, prefix-only (garbage) candidates are included at the end.
        /// </summary>
        public async Task<List<(string token, int frequency, bool plausible)>> ScanAsync(bool keepRaw = false)
        {
            var ascii = await CollectAsync(AsciiPattern, Encoding.UTF8);
            var utf16 = await CollectAsync(Utf16Pattern, Encoding.Unicode);
            var all = ascii.Concat(utf16);

            var frequency = new Dictionary<string, int>();
            foreach (var token in all)
                frequency[token] = frequency.TryGetValue(token, out int n) ? n + 1 : 1;

            var ordered = frequency.OrderByDescending(kvp => kvp.Value)
                .Select(kvp => (kvp.Key, kvp.Value, plausible: XblTokenShape.IsMatch(kvp.Key)))
                .ToList();
            return keepRaw
                ? ordered
                : ordered.Where(t => t.plausible).ToList();
        }

        private async Task<List<string>> CollectAsync(string pattern, Encoding encoding)
        {
            var candidates = new List<string>();
            var addresses = await _mem.AoBScan(pattern, true);
            foreach (var address in addresses)
            {
                string token = _mem.ReadString(address.ToString("X"), length: 16384, stringEncoding: encoding);
                if (!string.IsNullOrEmpty(token) && token.StartsWith("XBL3.0 x="))
                    candidates.Add(token);
            }
            return candidates;
        }
    }
}
