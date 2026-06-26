using System;
using System.Net.Http;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XAU.Services.AutoUnlock
{
    public interface IChangeSignal
    {
        bool IsAlive { get; }
        Task<bool> WaitAsync(TimeSpan timeout, CancellationToken token);
    }

    public sealed class RtaAchievementStream : IChangeSignal, IDisposable
    {
        private const string NonceUrl = "https://rta.xboxlive.com/nonce";
        private const string WsBase = "wss://rta.xboxlive.com/connect";
        private const string SubProtocol = "rta.xboxlive.com.V2";

        private readonly ClientWebSocket _ws;
        private readonly CancellationTokenSource _cts = new CancellationTokenSource();
        private volatile bool _alive;
        private TaskCompletionSource<bool> _changed =
            new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

        public bool IsAlive => _alive && !_cts.IsCancellationRequested;

        private RtaAchievementStream(ClientWebSocket ws)
        {
            _ws = ws;
        }

        public static async Task<RtaAchievementStream?> TryStartAsync(
            string xauthToken, string xuid, string scid, CancellationToken token)
        {
            if (string.IsNullOrEmpty(xauthToken) || string.IsNullOrEmpty(xuid) || string.IsNullOrEmpty(scid))
                return null;

            ClientWebSocket? ws = null;
            try
            {
                string nonce;
                using (var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10) })
                using (var req = new HttpRequestMessage(HttpMethod.Get, NonceUrl))
                {
                    req.Headers.TryAddWithoutValidation("Authorization", xauthToken);
                    req.Headers.TryAddWithoutValidation("Accept", "application/json");
                    using var resp = await http.SendAsync(req, token);
                    if (!resp.IsSuccessStatusCode)
                        return null;
                    var body = await resp.Content.ReadAsStringAsync();
                    nonce = JObject.Parse(body)["nonce"]?.ToString() ?? "";
                    if (string.IsNullOrEmpty(nonce))
                        return null;
                }

                ws = new ClientWebSocket();
                ws.Options.AddSubProtocol(SubProtocol);
                ws.Options.SetRequestHeader("User-Agent", "XboxServicesAPI/2021.10.20211005.0 c");
                var url = $"{WsBase}?nonce={Uri.EscapeDataString(nonce)}";
                using (var connectCts = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    connectCts.CancelAfter(TimeSpan.FromSeconds(10));
                    await ws.ConnectAsync(new Uri(url), connectCts.Token);
                }

                var uri = $"https://achievements.xboxlive.com/users/xuid({xuid})/achievements/{scid.ToLowerInvariant()}";
                var subscribe = $"[1,1,{JsonConvert.SerializeObject(uri)}]";
                var bytes = Encoding.UTF8.GetBytes(subscribe);
                await ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, token);

                var stream = new RtaAchievementStream(ws) { _alive = true };
                _ = Task.Run(() => stream.PumpAsync());
                return stream;
            }
            catch
            {
                try { ws?.Dispose(); } catch { }
                return null;
            }
        }

        public async Task<bool> WaitAsync(TimeSpan timeout, CancellationToken token)
        {
            var tcs = _changed;
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(token);
            var delay = Task.Delay(timeout, linked.Token);
            var done = await Task.WhenAny(tcs.Task, delay);
            if (done == tcs.Task)
            {
                linked.Cancel();
                return true;
            }
            return false;
        }

        private void Pulse()
        {
            var fresh = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            var old = Interlocked.Exchange(ref _changed, fresh);
            old.TrySetResult(true);
        }

        private async Task PumpAsync()
        {
            var buffer = new byte[8192];
            var sb = new StringBuilder();
            int subId = -1;
            try
            {
                while (!_cts.IsCancellationRequested && _ws.State == WebSocketState.Open)
                {
                    sb.Clear();
                    WebSocketReceiveResult result;
                    do
                    {
                        result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                        if (result.MessageType == WebSocketMessageType.Close)
                            return;
                        sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
                    } while (!result.EndOfMessage);

                    JArray arr;
                    try { arr = JArray.Parse(sb.ToString()); }
                    catch { continue; }
                    if (arr.Count == 0)
                        continue;

                    var type = arr[0].Value<int?>() ?? 0;
                    if (type == 1 && arr.Count == 5)
                    {
                        var status = arr[2].Value<int?>() ?? -1;
                        if (status == 0)
                            subId = arr[3].Value<int?>() ?? -1;
                        else
                            return; // rejeitado (throttle/limite/recurso desconhecido)
                    }
                    else if (type == 3 && arr.Count >= 2)
                    {
                        var evtSub = arr[1].Value<int?>() ?? -1;
                        if (subId < 0 || evtSub == subId)
                            Pulse();
                    }
                    else if (type == 4)
                    {
                        Pulse();
                    }
                }
            }
            catch
            {
            }
            finally
            {
                _alive = false;
                Pulse();
            }
        }

        public void Dispose()
        {
            _alive = false;
            try { _cts.Cancel(); } catch { }
            try
            {
                if (_ws.State == WebSocketState.Open)
                    _ = _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch { }
            try { _ws.Dispose(); } catch { }
            try { _cts.Dispose(); } catch { }
            Pulse();
        }
    }
}
