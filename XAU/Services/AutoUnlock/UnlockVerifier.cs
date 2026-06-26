using System;
using System.Linq;
using System.Threading;

namespace XAU.Services.AutoUnlock
{
    public enum VerifyOutcome
    {
        Confirmed,
        Pending,
        Cancelled
    }

    public sealed class UnlockVerifier
    {
        private readonly XboxRestAPI _api;
        private readonly string _xuid;
        private readonly string _titleId;
        private static readonly Random _jitter = new Random();

        public UnlockVerifier(XboxRestAPI api, string xuid, string titleId)
        {
            _api = api;
            _xuid = xuid;
            _titleId = titleId;
        }

        public async Task<VerifyOutcome> WaitForAchievedAsync(string achievementId, bool eventBased, CancellationToken token, IChangeSignal? signal = null)
        {
            var timeout = eventBased ? TimeSpan.FromMinutes(3) : TimeSpan.FromSeconds(30);
            var deadline = DateTime.UtcNow + timeout;
            // Rápido no começo (pega unlock instantâneo), depois estabiliza sem inflar.
            int[] backoff = { 3, 6, 10, 10, 15, 15 };
            int attempt = 0;

            while (true)
            {
                if (token.IsCancellationRequested)
                    return VerifyOutcome.Cancelled;

                try
                {
                    await XboxRateLimiter.Achievements.WaitAsync(token);
                    var resp = await _api.GetAchievementsForTitleAsync(_xuid, _titleId);
                    var ach = resp?.achievements?.FirstOrDefault(a => a.id == achievementId);
                    if (ach != null && ach.progressState == StringConstants.Achieved)
                        return VerifyOutcome.Confirmed;
                }
                catch (OperationCanceledException)
                {
                    return VerifyOutcome.Cancelled;
                }
                catch
                {
                }

                if (DateTime.UtcNow >= deadline)
                    return VerifyOutcome.Pending;

                var baseSec = backoff[Math.Min(attempt, backoff.Length - 1)];
                attempt++;
                var seconds = baseSec * (0.8 + _jitter.NextDouble() * 0.4);
                var wait = TimeSpan.FromSeconds(seconds);
                var remaining = deadline - DateTime.UtcNow;
                if (wait > remaining)
                    wait = remaining;
                if (wait <= TimeSpan.Zero)
                    return VerifyOutcome.Pending;

                try
                {
                    if (signal != null && signal.IsAlive)
                        await signal.WaitAsync(wait, token);
                    else
                        await Task.Delay(wait, token);
                }
                catch (OperationCanceledException)
                {
                    return VerifyOutcome.Cancelled;
                }
            }
        }
    }
}
