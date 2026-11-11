using System.Diagnostics;
using System.IO;
using System.Text;

namespace XAU.Util.Logging
{
    /// <summary>
    /// Thread-safe, size-bounded append-only diagnostic writer. Mirrors each line to the debug
    /// console AND a log file, so a long-running session's auth/stats/status behaviour can be
    /// reviewed after the fact even when the app was NOT started under a debugger.
    ///
    /// IMPORTANT -- the file lives under %LOCALAPPDATA%\XAU\logs, NOT under Documents\XAU.
    /// On many machines Documents is redirected into OneDrive; putting an ever-growing log there
    /// caused (a) intermittent UnauthorizedAccessException while OneDrive held the file open for
    /// sync/upload, and (b) app-wide slowness as OneDrive/Defender re-processed the dirtied file
    /// on every append. Local application data is neither synced nor placeholder-managed.
    ///
    /// Writing is gated by <see cref="Enabled"/> (bound to the Settings "Diagnostics Log" toggle)
    /// and every operation is swallowed -- logging must never throw out of the caller.
    /// </summary>
    public static class DiagLog
    {
        private static readonly object _gate = new object();
        private const long MaxBytesBeforeRotate = 4L * 1024 * 1024; // roll at 4 MB

        private static bool _enabled = false;    // opt in only after LoadSettings applies the saved choice
        private static bool _dirReady = false;
        private static long _bytesWritten = -1;   // -1 == not yet seeded from an existing file

        /// <summary>Master switch; typically bound to Settings (persisted as EnableDiagnosticsLog).</summary>
        public static bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }

        /// <summary>Location of the diagnostics log (created on first write).</summary>
        public static string LogPath
        {
            get
            {
                var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                if (string.IsNullOrEmpty(root))
                    root = Path.GetTempPath();
                return Path.Combine(root, "XAU", "logs", "xau_diagnostics.log");
            }
        }

        /// <summary>Formats a line with a full date+time stamp (pure; unit-testable).</summary>
        public static string FormatLine(string message)
        {
            return "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") + "] " + message;
        }

        /// <summary>Writes diagnostics only when the user has opted in.</summary>
        public static void Write(string message)
        {
            if (!_enabled)
                return;
            string line = FormatLine(message);

            try { Debug.WriteLine(line); }
            catch { /* console mirror must never break logging */ }

            try
            {
                lock (_gate)
                {
                    string path = LogPath;

                    if (!_dirReady)
                    {
                        var dir = Path.GetDirectoryName(path);
                        if (!string.IsNullOrEmpty(dir))
                            Directory.CreateDirectory(dir!);
                        _dirReady = true;
                    }

                    if (_bytesWritten < 0) // seed once from any file left by previous runs
                        _bytesWritten = File.Exists(path) ? new FileInfo(path).Length : 0;

                    string payload = line + Environment.NewLine;

                    if (_bytesWritten > MaxBytesBeforeRotate)
                        Rotate(path);

                    File.AppendAllText(path, payload);
                    _bytesWritten += Encoding.UTF8.GetByteCount(payload);
                }
            }
            catch { /* disk/logging failure must never crash the app */ }
        }

        private static void Rotate(string path)
        {
            try
            {
                string oldPath = path + ".old";
                if (File.Exists(oldPath))
                {
                    try { File.Delete(oldPath); }
                    catch { }
                }
                File.Move(path, oldPath);
            }
            catch { }
            _bytesWritten = 0;
        }
    }
}
