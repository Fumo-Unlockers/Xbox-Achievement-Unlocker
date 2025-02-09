using System.Diagnostics;
using System.Net;
using System.Security.Principal;
using System.Runtime.Versioning;
using System.Net.NetworkInformation;
using System.Net.Sockets;

namespace XAU.Services.HttpServer
{
    public sealed class HttpServer : IDisposable
    {
        private readonly HttpListener _listener;
        private readonly Dictionary<string, Func<HttpListenerContext, Task>> _routes;
        private string _port;
        private bool _isRunning;
        private bool _disposed;
        private const string FirewallRuleName = "XAU API Server";

        public HttpServer(string port, Dictionary<string, Func<HttpListenerContext, Task>> routes)
        {
            _port = port;
            _routes = routes;
            _listener = new HttpListener();
            UpdateListenerPrefixes();
        }

        private static bool IsAdministrator()
        {
            using var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        [SupportedOSPlatform("windows")]
        public void RestartAsAdmin()
        {
            var startInfo = new ProcessStartInfo
            {
                UseShellExecute = true,
                WorkingDirectory = Environment.CurrentDirectory,
                FileName = Process.GetCurrentProcess().MainModule?.FileName,
                Verb = "runas"
            };

            Process.Start(startInfo);
            Environment.Exit(0);
        }

        private void UpdateListenerPrefixes()
        {
            _listener.Prefixes.Clear();
            _listener.Prefixes.Add($"http://localhost:{_port}/");

            if (IsAdministrator())
            {
                // Admin required for other PCs on the network to access API (ex: XAU Mobile)
                _listener.Prefixes.Add($"http://*:{_port}/");
            }
        }

        public static void AddFirewallRule(string port)
        {
            if (RuleExists(port)) return;

            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall add rule name=\"{FirewallRuleName}\" dir=in action=allow protocol=TCP localport={port}",
                    Verb = "runas",
                    UseShellExecute = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            process.WaitForExit();
        }

        private static bool RuleExists(string port)
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "netsh",
                    Arguments = $"advfirewall firewall show rule name=\"{FirewallRuleName}\"",
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();

            return output.Contains(port);
        }

        public string GetListeningAddress()
        {
            if (_listener.Prefixes.Count == 1 && _listener.Prefixes.First().Contains("localhost"))
            {
                return $"http://localhost:{_port}";
            }

            return $"http://{GetLocalIPAddress()}:{_port}";
        }

        public static string GetLocalIPAddress()
        {
            try
            {
                var networkInterfaces = NetworkInterface.GetAllNetworkInterfaces()
                    .Where(n => n.OperationalStatus == OperationalStatus.Up
                                && n.NetworkInterfaceType != NetworkInterfaceType.Loopback);

                foreach (var network in networkInterfaces)
                {
                    var properties = network.GetIPProperties();

                    var ipv4 = properties.UnicastAddresses
                        .FirstOrDefault(ua => ua.Address.AddressFamily == AddressFamily.InterNetwork);

                    if (ipv4 != null)
                    {
                        return ipv4.Address.ToString();
                    }
                }

                return "127.0.0.1";
            }
            catch
            {
                return "127.0.0.1";
            }
        }

        public void Start()
        {
            if (_isRunning) return;

            try
            {
                if (!IsAdministrator())
                {
                    _listener.Start();
                    _isRunning = true;
                    Task.Run(HandleRequests);
                    return;
                }

                AddFirewallRule(_port);
                _listener.Start();
                _isRunning = true;
                Task.Run(HandleRequests);
            }
            catch (HttpListenerException ex)
            {
                if (IsAdministrator())
                {
                    RestartAsAdmin();
                }
                else
                {
                    Debug.WriteLine($"Failed to start HTTP server: {ex.Message}");
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;
            _listener.Stop();
            _isRunning = false;
        }

        public void UpdatePort(string newPort)
        {
            if (_port == newPort) return;

            bool wasRunning = _isRunning;
            if (wasRunning) Stop();

            _port = newPort;
            UpdateListenerPrefixes();

            if (wasRunning) Start();
        }

        private async Task HandleRequests()
        {
            while (_isRunning)
            {
                try
                {
                    var context = await _listener.GetContextAsync();
                    var path = context.Request.Url?.LocalPath ?? string.Empty;

                    var handler = _routes
                        .Where(r => path.StartsWith(r.Key))
                        .OrderByDescending(r => r.Key.Length)
                        .Select(r => r.Value)
                        .FirstOrDefault();

                    if (handler != null)
                    {
                        await handler.Invoke(context);
                    }
                    else
                    {
                        context.Response.StatusCode = 404;
                        context.Response.Close();
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error handling request: {ex.Message}");
                    _isRunning = false;
                }
            }
        }

        public bool IsRunning => _isRunning;

        public void Dispose()
        {
            if (_disposed) return;

            if (_isRunning) Stop();
            _listener.Close();
            _disposed = true;
        }
    }
}
