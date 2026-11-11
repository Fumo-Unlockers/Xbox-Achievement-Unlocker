using System;
using System.IO;
using System.Reflection;
using XAU.Util.Logging;
using XboxAuthNet.OAuth;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

public class AuthLifecycleRegressionTests
{
    [Fact]
    public async System.Threading.Tasks.Task ValidOAuthRestore_DoesNotPromptWhenXboxExchangeFails()
    {
        var restoredOAuth = new MicrosoftOAuthResponse { AccessToken = "synthetic-not-a-token" };
        var response = await HomeViewModel.RestoreOAuthSessionAsync(
            () => System.Threading.Tasks.Task.FromResult(restoredOAuth),
            _ => System.Threading.Tasks.Task.FromResult(false));

        Assert.Same(restoredOAuth, response);
    }

    [Fact]
    public async System.Threading.Tasks.Task PendingXboxTokenCannotRestoreInvalidatedSession()
    {
        var previousXauth = HomeViewModel.XAUTH;
        var previousLoggedIn = HomeViewModel._isLoggedIn;
        var previousInstance = HomeViewModel.Instance;
        try
        {
            HomeViewModel.XAUTH = "";
            HomeViewModel._isLoggedIn = false;
            var home = new HomeViewModel(null!, null!);
            var gate = typeof(HomeViewModel).GetField("_authGeneration", BindingFlags.Instance | BindingFlags.NonPublic)!;
            int generation = (int)gate.GetValue(home)!;
            var pending = new System.Threading.Tasks.TaskCompletionSource<(string, string, string?)>(
                System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            var publishing = home.AwaitAndPublishAuthAsync(pending.Task, generation);

            gate.SetValue(home, generation + 1); // a cache clear/logout invalidates outstanding work
            pending.SetResult(("XBL3.0 x=1;synthetic-new-token", "123", null));

            Assert.False(await publishing);
            Assert.Equal("", HomeViewModel.XAUTH);
            Assert.False(HomeViewModel._isLoggedIn);
        }
        finally
        {
            HomeViewModel.XAUTH = previousXauth;
            HomeViewModel._isLoggedIn = previousLoggedIn;
            HomeViewModel.Instance = previousInstance;
        }
    }

    [Fact]
    public async System.Threading.Tasks.Task XboxLoginReportsSuccessOnlyAfterTokenGenerationCompletes()
    {
        var previousXauth = HomeViewModel.XAUTH;
        var previousLoggedIn = HomeViewModel._isLoggedIn;
        var previousXuid = HomeViewModel.XUIDOnly;
        var previousInstance = HomeViewModel.Instance;
        try
        {
            HomeViewModel.XAUTH = "";
            HomeViewModel._isLoggedIn = false;
            var home = new HomeViewModel(null!, null!);
            var pending = new System.Threading.Tasks.TaskCompletionSource<(string, string, string?)>(
                System.Threading.Tasks.TaskCreationOptions.RunContinuationsAsynchronously);
            var publishing = home.AwaitAndPublishAuthAsync(pending.Task, 0);
            Assert.False(publishing.IsCompleted);
            Assert.False(HomeViewModel._isLoggedIn);

            pending.SetResult(("XBL3.0 x=1;synthetic-new-token", "123", null));
            Assert.True(await publishing);
            Assert.True(HomeViewModel._isLoggedIn);
            Assert.Equal("123", HomeViewModel.XUIDOnly);
        }
        finally
        {
            HomeViewModel.XAUTH = previousXauth;
            HomeViewModel._isLoggedIn = previousLoggedIn;
            HomeViewModel.XUIDOnly = previousXuid;
            HomeViewModel.Instance = previousInstance;
        }
    }

    [Fact]
    public void ClearAuthCache_DeletesSavedSessionAndRevokesExistingClient()
    {
        var scratch = Environment.GetEnvironmentVariable("XAU_TEST_SCRATCH")
            ?? Path.Combine(Path.GetTempPath(), "XAU.Tests");
        var directory = Path.Combine(scratch, "auth-clear-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var oldSettings = HomeViewModel.Settings;
        var oldToken = HomeViewModel.XAUTH;
        var oldXuid = HomeViewModel.XUIDOnly;
        var oldLoggedIn = HomeViewModel._isLoggedIn;
        var oldInstance = HomeViewModel.Instance;
        var oldEventsToken = AchievementsViewModel.EventsToken;
        var oldLogging = DiagLog.Enabled;
        try
        {
            DiagLog.Enabled = false;
            const string synthetic = "XBL3.0 x=1;synthetic-not-a-token";
            HomeViewModel.Settings = new XAUSettings { OAuthLogin = true };
            HomeViewModel.XAUTH = synthetic;
            HomeViewModel._isLoggedIn = true;
            AchievementsViewModel.EventsToken = "x:XBL3.0 x=1;synthetic-events-token";
            var api = new XboxRestAPI(synthetic);
            var home = new HomeViewModel(null!, null!);
            var sessionPath = Path.Combine(directory, "synthetic-session.bin");
            var settingsPath = Path.Combine(directory, "settings.json");
            File.WriteAllText(sessionPath, "placeholder-not-a-credential");
            typeof(HomeViewModel).GetField("AuthFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(home, sessionPath);
            typeof(HomeViewModel).GetField("SettingsFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(home, settingsPath);

            Assert.True(home.ClearAuthCache());
            api.SetDefaultSpooferHeaders();
            Assert.False(File.Exists(sessionPath));
            Assert.False(api._spooferClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));
            Assert.Null(AchievementsViewModel.EventsToken);
            Assert.False(HomeViewModel.Settings.OAuthLogin);
            var stored = Newtonsoft.Json.JsonConvert.DeserializeObject<XAUSettings>(File.ReadAllText(settingsPath));
            Assert.NotNull(stored);
            Assert.Null(stored.CachedEventsToken);
            Assert.False(stored.OAuthLogin);
        }
        finally
        {
            HomeViewModel.Settings = oldSettings;
            HomeViewModel.XAUTH = oldToken;
            HomeViewModel.XUIDOnly = oldXuid;
            HomeViewModel._isLoggedIn = oldLoggedIn;
            HomeViewModel.Instance = oldInstance;
            AchievementsViewModel.EventsToken = oldEventsToken;
            DiagLog.Enabled = oldLogging;
            Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void ClearAuthCache_ReportsFailure_WhenSavedSessionCannotBeDeleted()
    {
        var scratch = Environment.GetEnvironmentVariable("XAU_TEST_SCRATCH")
            ?? Path.Combine(Path.GetTempPath(), "XAU.Tests");
        var directory = Path.Combine(scratch, "auth-clear-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        var previousSettings = HomeViewModel.Settings;
        var previousXauth = HomeViewModel.XAUTH;
        var previousXuid = HomeViewModel.XUIDOnly;
        var previousLoggedIn = HomeViewModel._isLoggedIn;
        var previousInit = HomeViewModel.InitComplete;
        var previousInstance = HomeViewModel.Instance;
        bool previousLogging = DiagLog.Enabled;
        try
        {
            DiagLog.Enabled = false;
            HomeViewModel.Settings = new XAUSettings();
            HomeViewModel.XAUTH = "XBL3.0 x=1;synthetic-not-a-token";
            var home = new HomeViewModel(null!, null!);
            typeof(HomeViewModel).GetField("AuthFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(home, directory); // File.Delete(directory) fails without touching actual credentials.
            typeof(HomeViewModel).GetField("SettingsFilePath", BindingFlags.Instance | BindingFlags.NonPublic)!
                .SetValue(home, Path.Combine(directory, "settings.json"));

            Assert.False(home.ClearAuthCache());
            Assert.Equal("", HomeViewModel.XAUTH);
        }
        finally
        {
            HomeViewModel.Settings = previousSettings;
            HomeViewModel.XAUTH = previousXauth;
            HomeViewModel.XUIDOnly = previousXuid;
            HomeViewModel._isLoggedIn = previousLoggedIn;
            HomeViewModel.InitComplete = previousInit;
            HomeViewModel.Instance = previousInstance;
            DiagLog.Enabled = previousLogging;
            Directory.Delete(directory, recursive: true);
        }
    }
}
