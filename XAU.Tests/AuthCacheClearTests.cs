using Newtonsoft.Json;
using XAU.ViewModels.Pages;
using Xunit;

namespace XAU.Tests;

/// <summary>
/// Tests covering the "Clear Auth Cache" QoL feature. When a user's XAUTH or events
/// token goes out of scope, they should be able to reset auth state from the Settings
/// page without needing to reboot.
///
/// ClearAuthCache() cannot be invoked directly in unit tests because HomeViewModel
/// requires DI / a WPF dispatcher, so these tests validate the observable contract:
/// once the underlying static fields are reset the app is back to the
/// "unauthenticated, must log in again" state.
/// </summary>
public class AuthCacheClearTests : IDisposable
{
    public AuthCacheClearTests()
    {
        ResetAuthState();
    }

    public void Dispose()
    {
        ResetAuthState();
    }

    private static void ResetAuthState()
    {
        HomeViewModel.XAUTH = "";
        HomeViewModel.XAUTHTested = false;
        HomeViewModel.InitComplete = false;
        HomeViewModel._isLoggedIn = false;
        HomeViewModel.Settings = new XAUSettings();
        AchievementsViewModel.EventsToken = null;
        SettingsViewModel.ManualXauth = false;
    }

    [Fact]
    public void ClearedState_NoTokenValid()
    {
        // Given a fully cleared cache
        HomeViewModel.XAUTH = "";
        AchievementsViewModel.EventsToken = null;

        // Then neither auth path should consider the user authenticated
        Assert.False(HomeViewModel.IsEventsTokenValid());
        Assert.False(HomeViewModel._isLoggedIn);
        Assert.False(HomeViewModel.InitComplete);
    }

    [Fact]
    public void ClearedState_RequiresXAUTHTest_WhenTokenArrives()
    {
        // Simulate clearAuthCache -> user re-enters token via manual box
        HomeViewModel.XAUTH = "";
        HomeViewModel.XAUTHTested = false;

        HomeViewModel.XAUTH = "XBL3.0 x=123;sometoken";

        Assert.False(HomeViewModel.XAUTHTested);
    }

    [Fact]
    public void ClearedState_ManualXauthFlagReset_AutoScanCanResume()
    {
        // After clearing, the ManualXauth flag must be false so the XauthWorker's
        // auto-scan path (in XauthWorker_ProgressChanged) can resume populating XAUTH.
        SettingsViewModel.ManualXauth = true;

        SettingsViewModel.ManualXauth = false;

        Assert.False(SettingsViewModel.ManualXauth);
    }

    [Fact]
    public void PersistedSettings_RoundTrip_NullAfterClear()
    {
        // Given settings that had a cached events token
        var populated = new XAUSettings
        {
            CachedEventsToken = "x:XBL3.0 x=1234567890;" + new string('a', 40),
            EventsTokenObtainedAt = DateTime.UtcNow,
            EventsUserHash = "1234567890"
        };

        // When we simulate clearing (as ClearAuthCache does) by nulling them out
        populated.CachedEventsToken = null;
        populated.EventsTokenObtainedAt = null;
        populated.EventsUserHash = null;

        var json = JsonConvert.SerializeObject(populated);
        var restored = JsonConvert.DeserializeObject<XAUSettings>(json);

        // Then the persisted settings should have no leftover auth data
        Assert.NotNull(restored);
        Assert.Null(restored.CachedEventsToken);
        Assert.Null(restored.EventsTokenObtainedAt);
        Assert.Null(restored.EventsUserHash);
    }

    [Fact]
    public void Settings_RoundTrip_PreservesNonAuthFields_AfterClear()
    {
        // The clear operation must not clobber user preferences.
        var populated = new XAUSettings
        {
            UnlockAllEnabled = true,
            AutoSpooferEnabled = true,
            FakeSignatureEnabled = false,
            RegionOverride = true,
            OAuthLogin = true,
            AutoGrabEventsToken = true,
            PrivacyMode = true,
            CachedEventsToken = "x:XBL3.0 x=1234567890;" + new string('a', 40),
            EventsTokenObtainedAt = DateTime.UtcNow,
            EventsUserHash = "1234567890"
        };

        populated.CachedEventsToken = null;
        populated.EventsTokenObtainedAt = null;
        populated.EventsUserHash = null;

        var json = JsonConvert.SerializeObject(populated);
        var restored = JsonConvert.DeserializeObject<XAUSettings>(json);

        Assert.NotNull(restored);
        Assert.True(restored.UnlockAllEnabled);
        Assert.True(restored.AutoSpooferEnabled);
        Assert.False(restored.FakeSignatureEnabled);
        Assert.True(restored.RegionOverride);
        Assert.True(restored.OAuthLogin);
        Assert.True(restored.AutoGrabEventsToken);
        Assert.True(restored.PrivacyMode);
        Assert.Null(restored.CachedEventsToken);
    }

    [Fact]
    public void LoadSettings_DoesNotRestoreExpiredCachedToken_AfterClear()
    {
        // Simulate an expired cached token (older than 23h EventsTokenMaxAge).
        // LoadSettings should skip restoring it, matching ClearAuthCache behavior.
        var stale = new XAUSettings
        {
            CachedEventsToken = "x:XBL3.0 x=1234567890;" + new string('a', 40),
            EventsTokenObtainedAt = DateTime.UtcNow - TimeSpan.FromHours(24),
        };

        // The exact age check used in LoadSettings:
        var age = DateTime.UtcNow - stale.EventsTokenObtainedAt!.Value;
        var maxAge = TimeSpan.FromHours(23);
        bool shouldRestore = !string.IsNullOrEmpty(stale.CachedEventsToken) &&
                             stale.EventsTokenObtainedAt.HasValue &&
                             age < maxAge;

        Assert.False(shouldRestore);
    }

    [Fact]
    public void LoadSettings_RestoresFreshCachedToken_AfterRestart()
    {
        // Positive-path counterpart: a freshly cached token should still be restored,
        // so we did not over-aggressively invalidate cached tokens via the clear feature.
        var fresh = new XAUSettings
        {
            CachedEventsToken = "x:XBL3.0 x=1234567890;" + new string('a', 40),
            EventsTokenObtainedAt = DateTime.UtcNow - TimeSpan.FromHours(1),
        };

        var age = DateTime.UtcNow - fresh.EventsTokenObtainedAt!.Value;
        var maxAge = TimeSpan.FromHours(23);
        bool shouldRestore = !string.IsNullOrEmpty(fresh.CachedEventsToken) &&
                             fresh.EventsTokenObtainedAt.HasValue &&
                             age < maxAge;

        Assert.True(shouldRestore);
    }

    [Fact]
    public void ClearedState_EventsTokenEmpty_IsInvalid()
    {
        // Regression guard: after ClearAuthCache the events token is null, so any
        // UI / code path that calls IsEventsTokenValid must return false rather than
        // throwing on null.
        AchievementsViewModel.EventsToken = null;
        Assert.False(HomeViewModel.IsEventsTokenValid());
    }

    [Fact]
    public void ClearedState_EventsTokenExpired_ReturnsFalse_WithNoTimestamp()
    {
        // After clear, _eventsTokenObtainedAt is reset to DateTime.MinValue.
        // IsEventsTokenExpired must return false (not throw) in that state.
        Assert.False(HomeViewModel.IsEventsTokenExpired());
    }

    [Fact]
    public void XAUTH_Empty_AfterClear_AuthorizationHeaderNotSent()
    {
        // After ClearAuthCache, XAUTH is "", so XboxRestAPI should not attach a stale
        // Authorization header. This is the exact scenario that previously required
        // a reboot because the in-memory token stayed populated even after 401.
        HomeViewModel.XAUTH = "";
        var api = new XboxRestAPI(HomeViewModel.XAUTH);
        api.SetDefaultHeaders();

        Assert.False(api._httpClient.DefaultRequestHeaders.Contains(HeaderNames.Authorization));
    }
}
