public class XAUSettings
{
    public string? SettingsVersion { get; set; }
    public string? ToolVersion { get; set; }
    public bool UnlockAllEnabled { get; set; }
    public bool AutoSpooferEnabled { get; set; }
    public bool AutoLaunchXboxAppEnabled { get; set; }
    public bool LaunchHidden { get; set; }
    public bool FakeSignatureEnabled { get; set; }
    public bool RegionOverride { get; set; }
    public bool UseAcrylic { get; set; }
    public bool PrivacyMode { get; set; }
    public bool OAuthLogin { get; set; }
    public bool AutoGrabEventsToken { get; set; }
    // Account and stat diagnostics are opt-in. Explicitly enabled settings remain enabled.
    public bool EnableDiagnosticsLog { get; set; } = false;
    public int XauthScanReadLength { get; set; } = 16384;
    public string? CachedEventsToken { get; set; }
    public DateTime? EventsTokenObtainedAt { get; set; }
    public string? EventsUserHash { get; set; }
}
