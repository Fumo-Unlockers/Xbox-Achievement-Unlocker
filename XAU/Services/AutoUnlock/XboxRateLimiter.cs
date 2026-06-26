using System;
using System.Collections.Generic;
using System.Threading;

namespace XAU.Services.AutoUnlock
{
    public sealed class XboxRateLimiter
    {
        public static readonly XboxRateLimiter Achievements = new XboxRateLimiter(90, 280);
        public static readonly XboxRateLimiter StatsRead = new XboxRateLimiter(90, 280);
        public static readonly XboxRateLimiter StatsWrite = new XboxRateLimiter(90, 280);

        private static readonly TimeSpan BurstWindow = TimeSpan.FromSeconds(15);
        private static readonly TimeSpan SustainWindow = TimeSpan.FromSeconds(300);

        private readonly int _burstMax;
        private readonly int _sustainMax;
        private readonly Queue<DateTime> _burst = new Queue<DateTime>();
        private readonly Queue<DateTime> _sustain = new Queue<DateTime>();
        private readonly SemaphoreSlim _gate = new SemaphoreSlim(1, 1);

        public XboxRateLimiter(int burstMax, int sustainMax)
        {
            _burstMax = burstMax;
            _sustainMax = sustainMax;
        }

        public async Task WaitAsync(CancellationToken token)
        {
            await _gate.WaitAsync(token);
            try
            {
                while (true)
                {
                    var now = DateTime.UtcNow;
                    Trim(_burst, now - BurstWindow);
                    Trim(_sustain, now - SustainWindow);

                    if (_burst.Count < _burstMax && _sustain.Count < _sustainMax)
                    {
                        _burst.Enqueue(now);
                        _sustain.Enqueue(now);
                        return;
                    }

                    var waitBurst = _burst.Count >= _burstMax
                        ? _burst.Peek() + BurstWindow - now
                        : TimeSpan.Zero;
                    var waitSustain = _sustain.Count >= _sustainMax
                        ? _sustain.Peek() + SustainWindow - now
                        : TimeSpan.Zero;
                    var wait = waitBurst > waitSustain ? waitBurst : waitSustain;
                    if (wait < TimeSpan.FromMilliseconds(50))
                        wait = TimeSpan.FromMilliseconds(50);

                    await Task.Delay(wait, token);
                }
            }
            finally
            {
                _gate.Release();
            }
        }

        private static void Trim(Queue<DateTime> q, DateTime cutoff)
        {
            while (q.Count > 0 && q.Peek() < cutoff)
                q.Dequeue();
        }
    }
}
