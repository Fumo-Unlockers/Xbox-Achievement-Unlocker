using System.Reflection;
using XAU.Util.Diagnostics;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class SpoofAuthRecoveryTests
{
    [Fact]
    public void RejectedMemoryToken_SwitchesToSignedOutValidationCadence()
    {
        var previousLoggedIn = HomeViewModel._isLoggedIn;
        var previousTested = HomeViewModel.XAUTHTested;
        var previousInstance = HomeViewModel.Instance;
        var lastTestField = typeof(HomeViewModel).GetField("_lastXauthTestUtc",
            BindingFlags.NonPublic | BindingFlags.Static)!;
        var previousLastTest = (DateTime)lastTestField.GetValue(null)!;
        try
        {
            HomeViewModel._isLoggedIn = true;
            HomeViewModel.XAUTHTested = true;
            lastTestField.SetValue(null, DateTime.UtcNow);

            var home = new HomeViewModel(null!, null!);
            var generationField = typeof(HomeViewModel).GetField("_authGeneration",
                BindingFlags.NonPublic | BindingFlags.Instance)!;
            int oldGeneration = (int)generationField.GetValue(home)!;
            home.MarkSessionRejected();

            Assert.NotEqual(oldGeneration, (int)generationField.GetValue(home)!);
            Assert.False(HomeViewModel._isLoggedIn);
            Assert.False(HomeViewModel.XAUTHTested);
            Assert.Equal(DateTime.MinValue, (DateTime)lastTestField.GetValue(null)!);
        }
        finally
        {
            HomeViewModel._isLoggedIn = previousLoggedIn;
            HomeViewModel.XAUTHTested = previousTested;
            HomeViewModel.Instance = previousInstance;
            lastTestField.SetValue(null, previousLastTest);
        }
    }

    [Fact]
    public async Task DelayedOAuthRefresh_DoesNotCompleteBeforeNewTokenIsReady()
    {
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        bool fresh = false;
        var waiting = SpoofAuthRecovery.WaitForFreshTokenAsync(
            () => recovery.Task, () => fresh, TimeSpan.FromSeconds(2), CancellationToken.None);

        Assert.False(waiting.IsCompleted);
        fresh = true;
        recovery.SetResult(true);
        Assert.True(await waiting);
    }

    [Fact]
    public async Task MemoryScanCanCompleteAfterRecoveryReturnsFalse()
    {
        var nextPoll = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        bool fresh = false;
        var waiting = SpoofAuthRecovery.WaitForFreshTokenAsync(
            () => Task.FromResult(false), () => fresh, TimeSpan.FromSeconds(2),
            CancellationToken.None, (_, _) => nextPoll.Task);

        Assert.False(waiting.IsCompleted);
        fresh = true;
        nextPoll.SetResult();
        Assert.True(await waiting);
    }

    [Fact]
    public async Task StalledRecovery_TimesOutWithoutPretendingSpoofIsActive()
    {
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        Assert.False(await SpoofAuthRecovery.WaitForFreshTokenAsync(
            () => recovery.Task, () => false, TimeSpan.FromMilliseconds(20), CancellationToken.None));
    }

    [Fact]
    public async Task StoppingSpoof_CancelsRecoveryWait()
    {
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        using var stop = new CancellationTokenSource();
        var waiting = SpoofAuthRecovery.WaitForFreshTokenAsync(
            () => recovery.Task, () => false, TimeSpan.FromSeconds(2), stop.Token);
        stop.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await waiting);
    }
}
