using System.Text.Json.Serialization;

public class DisplayClaims
{
    public string? gtg { get; set; } // gamertag
    public string? mgt { get; set; } // modern gamertag
    public string? umg { get; set; } // unique modern tag
    public string? mgs { get; set; } // modern tag suffix
    public string? xui { get; set; } // xbox user id
    public string? uhs { get; set; } // user hash
    public string? agg { get; set; } // age group
    public string? usr { get; set; } // user settings restrictions
    public string? utr { get; set; } // user title restrictions
    public string? prv { get; set; } // privileges
}

public class DeviceToken
{
    public DisplayClaims? DisplayClaims { get; set; }
    public string? IssueInstant { get; set; }
    public string? Token { get; set; }
    public string? ExpireOn { get; set; }
}

// https://learn.microsoft.com/en-us/gaming/gdk/_content/gc/reference/live/xsapi-c/presence_c/enums/xblpresencedevicetype
public class XboxDeviceTypes
{
    public const string Unknown = "Unknown";
    public const string WindowsPhone = "WindowsPhone";
    public const string WindowsPhone7 = "WindowsPhone7";
    public const string Web = "Web";
    public const string Xbox360 = "Xbox360";
    public const string PC = "PC";
    public const string Windows8 = "Windows8";
    public const string XboxOne = "XboxOne";
    public const string WindowsOneCore = "WindowsOneCore";
    public const string WindowsOneCoreMobile = "WindowsOneCoreMobile";
    public const string iOS = "iOS";
    public const string Android = "Android";
    public const string AppleTV = "AppleTV";
    public const string Nintendo = "Nintendo";
    public const string PlayStation = "PlayStation";
    public const string Win32 = "Win32";
    public const string Scarlett = "Scarlett "; // Lockhart/Anaconda
}

public enum XboxDeviceTypes2
{
    Unknown,
    WindowsPhone,
    WindowsPhone7,
    Web,
    Xbox360,
    PC,
    Windows8,
    XboxOne,
    WindowsOneCore,
    WindowsOneCoreMobile,
    iOS,
    Android,
    AppleTV,
    Nintendo,
    PlayStation,
    Win32,
    Scarlett // Lockhart/Anaconda
}
