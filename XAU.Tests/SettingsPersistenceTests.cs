using System.IO;
using Newtonsoft.Json;
using XAU.ViewModels.Pages;
using XAU.Util.Logging;
using Xunit;

namespace XAU.Tests;

public sealed class SettingsPersistenceTests : IDisposable
{
    private readonly string _directory = Path.Combine(
        Environment.GetEnvironmentVariable("XAU_TEST_SCRATCH") ?? Path.GetTempPath(),
        "XAU.SettingsTests", Guid.NewGuid().ToString("N"));
    private readonly XAUSettings _originalSettings = HomeViewModel.Settings;
    private readonly bool _originalDiagnosticsEnabled = DiagLog.Enabled;

    public void Dispose()
    {
        HomeViewModel.Settings = _originalSettings;
        DiagLog.Enabled = _originalDiagnosticsEnabled;
        if (Directory.Exists(_directory))
            Directory.Delete(_directory, recursive: true);
    }

    [Fact]
    public void RefreshAfterAuthCacheCleared_ReplacesStaleOAuthAndPreservesOtherPreferences()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "settings.json");
        HomeViewModel.Settings = new XAUSettings { OAuthLogin = true, PrivacyMode = true };
        var viewModel = new SettingsViewModel(path);
        viewModel.LoadSettings();
        Assert.True(viewModel.OAuthLogin);

        // The Home clear path changes persisted settings while this page is still open.
        HomeViewModel.Settings.OAuthLogin = false;
        viewModel.RefreshAfterAuthCacheCleared();
        Assert.False(viewModel.OAuthLogin);
        Assert.True(viewModel.PrivacyMode);

        // Saving an unrelated setting must not restore the stale OAuth flag.
        viewModel.UnlockAllEnabled = true;
        viewModel.SaveSettings();
        var saved = JsonConvert.DeserializeObject<XAUSettings>(File.ReadAllText(path));
        Assert.NotNull(saved);
        Assert.False(saved.OAuthLogin);
        Assert.True(saved.PrivacyMode);
        Assert.True(saved.UnlockAllEnabled);
    }

    [Fact]
    public void SaveSettings_CapturesEventsMetadataOnlyAfterAcquiringWriteLock()
    {
        Directory.CreateDirectory(_directory);
        string path = Path.Combine(_directory, "settings.json");
        HomeViewModel.Settings = new XAUSettings();
        var viewModel = new SettingsViewModel(path) { OAuthLogin = true };

        Exception? saveError = null;
        using var started = new ManualResetEventSlim();
        var worker = new Thread(() =>
        {
            started.Set();
            try { viewModel.SaveSettings(); }
            catch (Exception ex) { saveError = ex; }
        });

        lock (HomeViewModel.SettingsWriteLock)
        {
            worker.Start();
            Assert.True(started.Wait(TimeSpan.FromSeconds(5)));
            Assert.True(SpinWait.SpinUntil(
                () => (worker.ThreadState & ThreadState.WaitSleepJoin) != 0,
                TimeSpan.FromSeconds(5)), "SaveSettings should wait for SettingsWriteLock");

            // A token writer changes metadata while SaveSettings is waiting for the lock.
            HomeViewModel.Settings.CachedEventsToken = "placeholder-event-token";
            HomeViewModel.Settings.EventsTokenObtainedAt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc);
            HomeViewModel.Settings.EventsUserHash = "placeholder-user";
        }

        Assert.True(worker.Join(TimeSpan.FromSeconds(5)), "SaveSettings should finish after lock release");
        Assert.Null(saveError);
        var saved = JsonConvert.DeserializeObject<XAUSettings>(File.ReadAllText(path));
        Assert.NotNull(saved);
        Assert.True(saved.OAuthLogin);
        Assert.Equal("placeholder-event-token", saved.CachedEventsToken);
        Assert.Equal(new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Utc), saved.EventsTokenObtainedAt);
        Assert.Equal("placeholder-user", saved.EventsUserHash);
        Assert.Equal(saved.CachedEventsToken, HomeViewModel.Settings.CachedEventsToken);
    }
}
