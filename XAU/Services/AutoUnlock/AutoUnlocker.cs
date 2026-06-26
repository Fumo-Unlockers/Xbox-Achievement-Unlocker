using System;
using System.Collections.Generic;
using System.Threading;
using XAU.AutoUnlock;

namespace XAU.Services.AutoUnlock
{
    public class AutoUnlocker
    {
        private readonly string _titleId;

        public AutoUnlocker(string titleId)
        {
            _titleId = titleId;
        }

        public async Task RunAsync(
            UnlockOrder order,
            Func<UnlockOrderItem, Task<bool>> unlockItem,
            HashSet<string> alreadyUnlocked,
            Action<AutoProgress> report,
            Func<bool> isPaused,
            CancellationToken token)
        {
            int total = order.Items.Count;

            int start = 0;
            long remainingWait = -1;
            var state = OrderRepository.LoadState(_titleId);
            if (state != null
                && state.TitleId == _titleId
                && state.Index >= 0
                && state.Index < total
                && order.Items[state.Index].Id == state.AchievementId)
            {
                start = state.Index;
                var missing = (state.TargetTimeUtc - DateTime.UtcNow).TotalSeconds;
                remainingWait = missing > 0 ? (long)Math.Ceiling(missing) : 0;
            }

            for (int i = start; i < total; i++)
            {
                if (token.IsCancellationRequested)
                {
                    Report(report, order, i, total, "", -1, "Stopped", false);
                    return;
                }

                var item = order.Items[i];

                if (alreadyUnlocked.Contains(item.Id))
                    continue;

                long delay;
                if (i == start && remainingWait >= 0)
                {
                    delay = remainingWait;
                    remainingWait = -1;
                }
                else
                {
                    delay = item.DelaySeconds;
                }

                var target = DateTime.UtcNow.AddSeconds(delay);

                OrderRepository.SaveState(new AutoUnlockState
                {
                    TitleId = _titleId,
                    Index = i,
                    AchievementId = item.Id,
                    AchievementName = item.Name,
                    TargetTimeUtc = target
                });

                while (DateTime.UtcNow < target)
                {
                    if (token.IsCancellationRequested)
                    {
                        Report(report, order, i, total, item.Name, -1, "Stopped", false);
                        return;
                    }

                    if (isPaused())
                    {
                        target = target.AddSeconds(1);
                        var frozen = (long)Math.Ceiling((target - DateTime.UtcNow).TotalSeconds);
                        Report(report, order, i + 1, total, item.Name, Math.Max(0, frozen), "Paused", false);
                        try { await Task.Delay(1000, token); }
                        catch (OperationCanceledException)
                        {
                            Report(report, order, i, total, item.Name, -1, "Stopped", false);
                            return;
                        }
                        continue;
                    }

                    var remaining = (long)Math.Ceiling((target - DateTime.UtcNow).TotalSeconds);
                    Report(report, order, i + 1, total, item.Name, Math.Max(0, remaining), "", false);

                    try
                    {
                        await Task.Delay(1000, token);
                    }
                    catch (OperationCanceledException)
                    {
                        Report(report, order, i, total, item.Name, -1, "Stopped", false);
                        return;
                    }
                }

                while (isPaused() && !token.IsCancellationRequested)
                {
                    Report(report, order, i + 1, total, item.Name, 0, "Paused", false);
                    try { await Task.Delay(1000, token); }
                    catch (OperationCanceledException)
                    {
                        Report(report, order, i, total, item.Name, -1, "Stopped", false);
                        return;
                    }
                }
                if (token.IsCancellationRequested)
                {
                    Report(report, order, i, total, item.Name, -1, "Stopped", false);
                    return;
                }

                Report(report, order, i + 1, total, item.Name, -1, "Unlocking", false);
                bool success;
                try
                {
                    success = await unlockItem(item);
                }
                catch
                {
                    success = false;
                }

                if (!success)
                {
                    Report(report, order, i + 1, total, item.Name, -1, $"Failed on: {item.Name}", true);
                    return;
                }

                alreadyUnlocked.Add(item.Id);

                if (i + 1 < total)
                {
                    var next = order.Items[i + 1];
                    OrderRepository.SaveState(new AutoUnlockState
                    {
                        TitleId = _titleId,
                        Index = i + 1,
                        AchievementId = next.Id,
                        AchievementName = next.Name,
                        TargetTimeUtc = DateTime.UtcNow.AddSeconds(next.DelaySeconds)
                    });
                }
            }

            OrderRepository.ClearState(_titleId);
            Report(report, order, total, total, "", -1, "Completed", true);
        }

        private static void Report(
            Action<AutoProgress> report,
            UnlockOrder order,
            int index,
            int total,
            string achievementName,
            long secondsRemaining,
            string message,
            bool completed)
        {
            report(new AutoProgress
            {
                Index = index,
                Total = total,
                GameName = order.GameName,
                AchievementName = achievementName,
                SecondsRemaining = secondsRemaining,
                Message = message,
                Completed = completed
            });
        }
    }
}
