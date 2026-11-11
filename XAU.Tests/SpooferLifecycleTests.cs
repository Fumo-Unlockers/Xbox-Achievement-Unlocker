using System.Net;
using System.Net.Http;
using System.Reflection;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class SpooferLifecycleTests : IDisposable
{
    public SpooferLifecycleTests()
    {
        HomeViewModel.XUIDOnly = "synthetic-xuid";
        HomeViewModel.XAUTH = "";
        HomeViewModel.XAUTHTested = false;
        HomeViewModel.SpoofingStatus = 0;
        HomeViewModel.SpoofedTitleID = "0";
        HomeViewModel.AutoSpoofedTitleID = "0";
        AchievementsViewModel.SpoofingUpdate = false;
    }

    public void Dispose()
    {
        HomeViewModel.XUIDOnly = "";
        HomeViewModel.XAUTH = "";
        HomeViewModel.XAUTHTested = false;
        HomeViewModel.SpoofingStatus = 0;
        HomeViewModel.SpoofedTitleID = "0";
        HomeViewModel.AutoSpoofedTitleID = "0";
        AchievementsViewModel.SpoofingUpdate = true;
    }

    [Fact]
    public async Task Manual_StopDuringFetch_OldCompletionCannotStartOrOverwriteNewRun()
    {
        var firstTitle = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int titleRequests = 0;
        int heartbeats = 0;
        var api = FakeApi(async (request, _) =>
        {
            if (request.RequestUri!.Host == "titlehub.xboxlive.com")
            {
                if (Interlocked.Increment(ref titleRequests) == 1)
                {
                    firstEntered.SetResult();
                    return await firstTitle.Task; // deliberately ignores cancellation
                }
                return Json("{\"titles\":[{\"name\":\"New Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}");
            }
            if (request.RequestUri.Host == "userstats.xboxlive.com")
                return Json("{\"statListsCollection\":[]}");
            if (request.Method == HttpMethod.Post)
                Interlocked.Increment(ref heartbeats);
            return Json("{}");
        });
        var vm = new MiscViewModel(null!);
        InjectApi(vm, api);
        vm.NewSpoofingID = "111";

        var first = vm.SpooferButtonClicked();
        await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await vm.SpooferButtonClicked(); // stop even before the first info fetch returns
        Assert.Equal(1, Volatile.Read(ref titleRequests));
        vm.NewSpoofingID = "222";
        var second = vm.SpooferButtonClicked();
        await Eventually(() => Volatile.Read(ref heartbeats) == 1);
        firstTitle.SetResult(Json("{\"titles\":[{\"name\":\"Old Game\",\"titleId\":\"111\",\"devices\":[\"PC\"]}] }"));
        await first.WaitAsync(TimeSpan.FromSeconds(10));

        Assert.Equal(2, Volatile.Read(ref titleRequests));
        Assert.Equal("222", vm.CurrentSpoofingID);
        Assert.Contains("New Game", vm.SpoofingText);
        Assert.Equal("222", HomeViewModel.SpoofedTitleID);
        await vm.SpooferButtonClicked();
        await second.WaitAsync(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void FailedManualFetch_KeepsExistingAutoSpoofActive()
    {
        HomeViewModel.SpoofingStatus = 2;
        HomeViewModel.AutoSpoofedTitleID = "999";
        var vm = new MiscViewModel(null!);
        using var pendingManual = new CancellationTokenSource();
        typeof(MiscViewModel).GetField("_spoofCts", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(vm, pendingManual);
        typeof(MiscViewModel).GetMethod("AbortSpoofing", BindingFlags.NonPublic | BindingFlags.Instance)!
            .Invoke(vm, new object[] { pendingManual, "Spoofing Not Started" });

        Assert.Equal(2, HomeViewModel.SpoofingStatus);
        Assert.Equal("999", HomeViewModel.AutoSpoofedTitleID);
    }

    [Fact]
    public async Task CancelledManualTakeover_KeepsExistingAutoSpoofActive()
    {
        HomeViewModel.SpoofingStatus = 2;
        HomeViewModel.AutoSpoofedTitleID = "999";
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var title = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeats = 0;
        var api = FakeApi(async (request, _) =>
        {
            if (request.RequestUri!.Host == "titlehub.xboxlive.com")
            {
                entered.TrySetResult();
                return await title.Task;
            }
            if (request.RequestUri.Host == "userstats.xboxlive.com")
                return Json("{\"statListsCollection\":[]}");
            Interlocked.Increment(ref heartbeats);
            return Json("{}");
        });
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);

        var start = vm.SpooferButtonClickedCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        await vm.SpooferButtonClickedCommand.ExecuteAsync(null);
        Assert.Equal(2, HomeViewModel.SpoofingStatus);
        Assert.Equal("999", HomeViewModel.AutoSpoofedTitleID);

        title.SetResult(Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"));
        await start.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(0, Volatile.Read(ref heartbeats));
    }

    [Fact]
    public async Task Manual_CommandRemainsClickableWhileSpoofIsStarting()
    {
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var title = new TaskCompletionSource<HttpResponseMessage>(TaskCreationOptions.RunContinuationsAsynchronously);
        var api = FakeApi(async (request, _) =>
        {
            if (request.RequestUri!.Host == "titlehub.xboxlive.com")
            {
                entered.TrySetResult();
                return await title.Task;
            }
            if (request.RequestUri.Host == "userstats.xboxlive.com")
                return Json("{\"statListsCollection\":[]}");
            return Json("{}", HttpStatusCode.Forbidden);
        });
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        var start = vm.SpooferButtonClickedCommand.ExecuteAsync(null);
        await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
        bool canStop = vm.SpooferButtonClickedCommand.CanExecute(null);
        title.SetResult(Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"));
        await start.WaitAsync(TimeSpan.FromSeconds(3));

        Assert.True(canStop, "the same button must remain clickable to cancel a pending start");
    }

    [Fact]
    public async Task Manual_TimerUpdatesEverySecondBetweenSuccessfulHeartbeats()
    {
        var api = FakeApi((request, _) => Task.FromResult(request.RequestUri!.Host switch
        {
            "titlehub.xboxlive.com" => Json("{\"titles\":[{\"name\":\"New Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"),
            "userstats.xboxlive.com" => Json("{\"statListsCollection\":[]}"),
            _ => Json("{}")
        }));
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        var delaySeen = new TaskCompletionSource<TimeSpan>(TaskCreationOptions.RunContinuationsAsynchronously);
        vm.HeartbeatDelayAsync = (duration, token) =>
        {
            delaySeen.TrySetResult(duration);
            return Task.Delay(Timeout.InfiniteTimeSpan, token);
        };

        var running = vm.SpooferButtonClicked();
        var delay = await delaySeen.Task.WaitAsync(TimeSpan.FromSeconds(3));
        Assert.Equal(TimeSpan.FromSeconds(1), delay);
        await vm.SpooferButtonClicked();
        await running.WaitAsync(TimeSpan.FromSeconds(3));
    }

    [Fact]
    public async Task Manual_401WaitsForDelayedRefreshBeforeRetryingHeartbeat()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-expired";
        HomeViewModel._isLoggedIn = true;
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovering = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeats = 0;
        var api = FakeApi((request, _) => Task.FromResult(request.RequestUri!.Host switch
        {
            "titlehub.xboxlive.com" => Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"),
            "userstats.xboxlive.com" => Json("{\"statListsCollection\":[]}"),
            _ => Json("{}", Interlocked.Increment(ref heartbeats) == 1
                ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
        }));
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        vm.RecoverAuthAsync = () => { recovering.TrySetResult(); return recovery.Task; };
        vm.AuthRecoveryTimeout = TimeSpan.FromSeconds(3);
        vm.HeartbeatDelayAsync = (_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token);

        try
        {
            var running = vm.SpooferButtonClicked();
            await recovering.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(1, Volatile.Read(ref heartbeats));
            Assert.Contains("Refreshing", vm.SpoofingText);
            Assert.False(running.IsCompleted);

            HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-refreshed";
            HomeViewModel.XAUTHTested = true;
            recovery.SetResult(true);
            await Eventually(() => Volatile.Read(ref heartbeats) == 2);
            Assert.Contains("Spoofing Game", vm.SpoofingText);
            await vm.SpooferButtonClicked();
            await running.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally
        {
            HomeViewModel._isLoggedIn = false;
        }
    }

    [Fact]
    public async Task Manual_MemoryTokenIsNotRetriedUntilProfileValidationSucceeds()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-expired";
        HomeViewModel.XAUTHTested = true;
        HomeViewModel._isLoggedIn = true;
        var pollEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var resumePoll = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeats = 0;
        var api = FakeApi((request, _) => Task.FromResult(request.RequestUri!.Host switch
        {
            "titlehub.xboxlive.com" => Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"),
            "userstats.xboxlive.com" => Json("{\"statListsCollection\":[]}"),
            _ => Json("{}", Interlocked.Increment(ref heartbeats) == 1
                ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
        }));
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        vm.RecoverAuthAsync = () => Task.FromResult(false); // memory rescan starts asynchronously
        vm.AuthRecoveryTimeout = TimeSpan.FromSeconds(3);
        int polls = 0;
        vm.AuthRecoveryDelayAsync = (_, token) =>
        {
            if (Interlocked.Increment(ref polls) != 1)
                return Task.Delay(10, token);
            pollEntered.TrySetResult();
            return resumePoll.Task;
        };
        vm.HeartbeatDelayAsync = (_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token);
        var running = vm.SpooferButtonClicked();
        try
        {
            await pollEntered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-replacement";
            HomeViewModel.XAUTHTested = false; // scanner adopted, profile has not validated it
            resumePoll.SetResult();
            await Task.Delay(50);
            Assert.Equal(1, Volatile.Read(ref heartbeats));

            HomeViewModel.XAUTHTested = true;
            await Eventually(() => Volatile.Read(ref heartbeats) == 2);
        }
        finally
        {
            await vm.SpooferButtonClicked();
            await running.WaitAsync(TimeSpan.FromSeconds(3));
            HomeViewModel.XAUTHTested = false;
            HomeViewModel._isLoggedIn = false;
        }
    }

    [Fact]
    public async Task Auto_401WaitsForDelayedRefreshBeforeRetryingHeartbeat()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-expired";
        HomeViewModel._isLoggedIn = true;
        HomeViewModel.AutoSpoofedTitleID = "333";
        HomeViewModel.SpoofingStatus = 2;
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovering = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeats = 0;
        var api = FakeApi((request, _) => Task.FromResult(Json("{}",
            Interlocked.Increment(ref heartbeats) == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)));
        var vm = new AchievementsViewModel(null!, null!, null!) { GameInfo = "Auto Spoofing" };
        InjectApi(vm, api);
        vm.RecoverAuthAsync = () => { recovering.TrySetResult(); return recovery.Task; };
        vm.AuthRecoveryTimeout = TimeSpan.FromSeconds(3);
        vm.AutoSpoofDelayAsync = (_, token) => Task.Delay(Timeout.InfiniteTimeSpan, token);

        try
        {
            var running = vm.Spoofing();
            await recovering.Task.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(1, Volatile.Read(ref heartbeats));
            Assert.False(running.IsCompleted);
            HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-refreshed";
            HomeViewModel.XAUTHTested = true;
            recovery.SetResult(true);
            await Eventually(() => Volatile.Read(ref heartbeats) == 2);
            Assert.Equal(2, HomeViewModel.SpoofingStatus);
            var run = (CancellationTokenSource)typeof(AchievementsViewModel)
                .GetField("_autoSpoofCts", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(vm)!;
            run.Cancel();
            await running.WaitAsync(TimeSpan.FromSeconds(3));
        }
        finally
        {
            HomeViewModel._isLoggedIn = false;
        }
    }

    [Fact]
    public async Task Manual_401RefreshTimeout_StopsInsteadOfPretendingPresenceIsActive()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-expired";
        HomeViewModel._isLoggedIn = true;
        var api = FakeApi((request, _) => Task.FromResult(request.RequestUri!.Host switch
        {
            "titlehub.xboxlive.com" => Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"),
            "userstats.xboxlive.com" => Json("{\"statListsCollection\":[]}"),
            _ => Json("{}", HttpStatusCode.Unauthorized)
        }));
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        vm.RecoverAuthAsync = () => Task.FromResult(false);
        vm.AuthRecoveryTimeout = TimeSpan.FromMilliseconds(30);

        try
        {
            await vm.SpooferButtonClicked().WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(0, HomeViewModel.SpoofingStatus);
            Assert.Contains("did not refresh", vm.SpoofingText);
        }
        finally
        {
            HomeViewModel._isLoggedIn = false;
        }
    }

    [Fact]
    public async Task Manual_StopDuringRecovery_CannotResumeOldSpoof()
    {
        HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-expired";
        HomeViewModel._isLoggedIn = true;
        var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var recovery = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
        int heartbeats = 0;
        var api = FakeApi((request, _) => Task.FromResult(request.RequestUri!.Host switch
        {
            "titlehub.xboxlive.com" => Json("{\"titles\":[{\"name\":\"Game\",\"titleId\":\"222\",\"devices\":[\"PC\"]}]}"),
            "userstats.xboxlive.com" => Json("{\"statListsCollection\":[]}"),
            _ => request.Method == HttpMethod.Delete ? Json("{}") :
                Json("{}", Interlocked.Increment(ref heartbeats) == 1 ? HttpStatusCode.Unauthorized : HttpStatusCode.OK)
        }));
        var vm = new MiscViewModel(null!) { NewSpoofingID = "222" };
        InjectApi(vm, api);
        vm.RecoverAuthAsync = () => { entered.TrySetResult(); return recovery.Task; };
        vm.AuthRecoveryTimeout = TimeSpan.FromSeconds(3);

        try
        {
            var running = vm.SpooferButtonClickedCommand.ExecuteAsync(null);
            await entered.Task.WaitAsync(TimeSpan.FromSeconds(3));
            await vm.SpooferButtonClickedCommand.ExecuteAsync(null);
            HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-refreshed";
            HomeViewModel.XAUTHTested = true;
            recovery.SetResult(true);
            await running.WaitAsync(TimeSpan.FromSeconds(3));
            Assert.Equal(1, Volatile.Read(ref heartbeats));
            Assert.Equal(0, HomeViewModel.SpoofingStatus);
            Assert.Equal("Spoofing Not Started", vm.SpoofingText);
        }
        finally
        {
            HomeViewModel._isLoggedIn = false;
        }
    }

    [Fact]
    public async Task Auto_ForbiddenHeartbeat_StopsAndSurfacesFailure()
    {
        int heartbeats = 0;
        var api = FakeApi((request, _) =>
        {
            Interlocked.Increment(ref heartbeats);
            return Task.FromResult(Json("{}", HttpStatusCode.Forbidden));
        });
        var vm = new AchievementsViewModel(null!, null!, null!) { GameInfo = "Auto Spoofing" };
        InjectApi(vm, api);
        HomeViewModel.AutoSpoofedTitleID = "333";
        HomeViewModel.SpoofingStatus = 2;

        await vm.Spoofing().WaitAsync(TimeSpan.FromSeconds(3));

        // One spoof tick now walks the presence/heartbeat fallback chain (userpresence xuid, /me,
        // presence-heartbeat xuid, /me): an all-403 tick is 4 POSTs before the abort surfaces.
        Assert.Equal(4, Volatile.Read(ref heartbeats));
        Assert.Equal(0, HomeViewModel.SpoofingStatus);
        Assert.Equal("0", HomeViewModel.AutoSpoofedTitleID);
        Assert.Contains("403", vm.GameInfo);
    }

    private static XboxRestAPI FakeApi(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply)
    {
        var api = new XboxRestAPI("");
        var client = new HttpClient(new FakeHandler(reply));
        typeof(XboxRestAPI).GetField("_httpClient", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(api, client);
        typeof(XboxRestAPI).GetField("_spooferClient", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(api, client);
        return api;
    }

    private static void InjectApi(object vm, XboxRestAPI api)
    {
        vm.GetType().GetField("_xboxRestAPI", BindingFlags.NonPublic | BindingFlags.Instance)!
            .SetValue(vm, new Lazy<XboxRestAPI>(() => api));
    }

    private static HttpResponseMessage Json(string body, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(body) };

    private static async Task Eventually(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
        while (!condition())
            await Task.Delay(10, timeout.Token);
    }

    private sealed class FakeHandler(Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> reply) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) =>
            reply(request, cancellationToken);
    }
}
