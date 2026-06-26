using System;
using System.Linq;
using System.Threading;
using Newtonsoft.Json.Linq;

namespace XAU.Services.AutoUnlock
{
    public enum CounterResult
    {
        NotCounter, // sem requisito numérico alvo > 1
        Achieved,
        Pending,    // enviado, mas não confirmado a tempo (pode cair depois)
        Failed
    }

    // Sonda 1 evento, mede o incremento real e recalcula o lote a cada rodada para
    // convergir no alvo com poucos eventos sem estourar quando o passo é fracionado.
    public sealed class CounterUnlocker
    {
        private const int MaxTotalEvents = 10000;
        private const int MaxEventsPerRound = 2000;
        private const int MaxRounds = 8;
        private const int MaxStallRounds = 2;
        private static readonly TimeSpan HardTimeout = TimeSpan.FromMinutes(8);
        private static readonly int[] VerifyBackoff = { 3, 5, 8, 12 };

        private readonly XboxRestAPI _api;
        private readonly string _titleId;
        private readonly string _xuid;
        private readonly string _eventsToken;

        public CounterUnlocker(XboxRestAPI api, string titleId, string xuid, string eventsToken)
        {
            _api = api;
            _titleId = titleId;
            _xuid = xuid;
            _eventsToken = eventsToken;
        }

        public async Task<CounterResult> RunAsync(string achievementId, JObject gameData, CancellationToken token)
        {
            var snap = await ReadAsync(achievementId, token);
            if (snap == null)
                return CounterResult.NotCounter;
            if (snap.Achieved)
                return CounterResult.Achieved;
            if (snap.Target <= 1)
                return CounterResult.NotCounter;

            var events = new EventUnlocker(_api, _titleId, _xuid, _eventsToken);
            var started = DateTime.UtcNow;

            long current = snap.Current;
            long target = snap.Target;
            long sent = 0;

            // Sonda: 1 evento para medir o incremento.
            if (!await TrySendAsync(events, achievementId, gameData, 1))
                return CounterResult.Failed;
            sent += 1;

            var probed = await VerifyAsync(achievementId, token, 0);
            if (probed == null) return CounterResult.Pending;
            if (probed.Achieved || probed.Current >= target) return CounterResult.Achieved;

            // Maximum/Minimum não acumulam: lote não adianta.
            if (snap.IsSingleShot)
                return CounterResult.Pending;

            double perEvent = Math.Max(0, probed.Current - current);
            current = probed.Current;

            int stall = 0;
            for (int round = 0; round < MaxRounds; round++)
            {
                if (token.IsCancellationRequested) return CounterResult.Pending;
                if (DateTime.UtcNow - started > HardTimeout) return CounterResult.Pending;
                if (sent >= MaxTotalEvents) break;

                long remaining = target - current;
                if (remaining <= 0) break;

                // Sem incremento medido: assume +1/evento.
                double step = perEvent > 0 ? perEvent : 1;
                long toSend = (long)Math.Ceiling(remaining / step);
                toSend = Math.Clamp(toSend, 1, Math.Min(MaxEventsPerRound, MaxTotalEvents - sent));

                if (!await TrySendAsync(events, achievementId, gameData, (int)toSend))
                    return current > snap.Current ? CounterResult.Pending : CounterResult.Failed;
                sent += toSend;

                var after = await VerifyAsync(achievementId, token, round);
                if (after == null) return CounterResult.Pending;
                if (after.Achieved || after.Current >= target) return CounterResult.Achieved;

                long moved = after.Current - current;
                if (moved > 0)
                {
                    perEvent = (double)moved / toSend;
                    current = after.Current;
                    stall = 0;
                }
                else if (++stall >= MaxStallRounds)
                {
                    break; // não anda mais; não é puramente contador
                }
            }

            var final = await ReadAsync(achievementId, token);
            return (final?.Achieved ?? false) ? CounterResult.Achieved : CounterResult.Pending;
        }

        private static async Task<bool> TrySendAsync(EventUnlocker events, string achievementId, JObject gameData, int count)
        {
            try { await events.SendEventsAsync(achievementId, gameData, count); return true; }
            catch { return false; }
        }

        private async Task<Snapshot?> VerifyAsync(string achievementId, CancellationToken token, int round)
        {
            var seconds = VerifyBackoff[Math.Min(round, VerifyBackoff.Length - 1)];
            try { await Task.Delay(TimeSpan.FromSeconds(seconds), token); }
            catch (OperationCanceledException) { return null; }
            return await ReadAsync(achievementId, token);
        }

        private async Task<Snapshot?> ReadAsync(string achievementId, CancellationToken token)
        {
            try
            {
                await XboxRateLimiter.Achievements.WaitAsync(token);
                var resp = await _api.GetAchievementsForTitleAsync(_xuid, _titleId);
                var ach = resp?.achievements?.FirstOrDefault(a => a.id == achievementId);
                if (ach == null) return null;

                bool achieved = ach.progressState == StringConstants.Achieved;

                // Gargalo: o requisito numérico mais longe do alvo.
                var reqs = ach.progression?.requirements?
                    .Where(r => long.TryParse(r.target, out var t) && t > 1)
                    .ToList();
                if (reqs == null || reqs.Count == 0)
                    return achieved ? new Snapshot(0, 1, true, false) : null;

                var bottleneck = reqs
                    .Select(r =>
                    {
                        long.TryParse(r.target, out var tg);
                        long.TryParse(r.current, out var cu);
                        return (cu, tg, op: r.operationType ?? "");
                    })
                    .OrderByDescending(x => x.tg - x.cu)
                    .First();

                bool single = bottleneck.op.Equals("Maximum", StringComparison.OrdinalIgnoreCase)
                           || bottleneck.op.Equals("Minimum", StringComparison.OrdinalIgnoreCase);

                return new Snapshot(bottleneck.cu, bottleneck.tg, achieved, single);
            }
            catch (OperationCanceledException) { return null; }
            catch { return null; }
        }

        private sealed record Snapshot(long Current, long Target, bool Achieved, bool IsSingleShot);
    }
}
