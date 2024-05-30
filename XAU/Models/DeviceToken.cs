public class Properties
{
    public string? AuthMethod { get; set; } = "ProofOfPossession";
    public string? Id { get; set; } = "{" + Guid.NewGuid().ToString("D") + "}";
    public string? DeviceType { get; set; } = XboxDeviceTypes.Win32.ToString();
    public string? SerialNumber { get; set; } = "{" + Guid.NewGuid().ToString("D") + "}";
    public string? Version { get; set; } = "0.0.0";
    public object? ProofKey { get; set; }
}

public class DeviceTokenRequest
{
    public Properties? Properties { get; set; }
    public string? RelyingParty { get; set; } = "http://auth.xboxlive.com";
    public string? TokenType { get; set; } = "JWT";
}


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

public enum XboxDeviceTypes
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
