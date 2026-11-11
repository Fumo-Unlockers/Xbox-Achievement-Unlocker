using Xunit;

namespace XAU.Tests;

/// <summary>
/// Spoof request-construction tests: the token sanitizer (presence/heartbeat 403 on a token
/// carrying trailing garbage) and the heartbeat body (numeric title id, 600s expiry, active
/// state, RETAIL sandbox — Fumo PR #519 reference format).
/// </summary>
public class SpoofRequestTests
{
    [Theory]
    [InlineData("XBL3.0 x=1234567890;abcDEF123-_=", "XBL3.0 x=1234567890;abcDEF123-_=")]
    [InlineData("  XBL3.0 x=1234567890;abc  ", "XBL3.0 x=1234567890;abc")]
    [InlineData("XBL3.0 x=1234567890;abc\0junk", "XBL3.0 x=1234567890;abc")]
    [InlineData("junk XBL3.0 x=1;a trailing", "XBL3.0 x=1;a")]
    [InlineData("XBL3.0 x=1234567890;", "XBL3.0 x=1234567890;")]
    public void SanitizeXauth_TrimsTokenToHeaderSemicolonPayload(string input, string expected)
    {
        Assert.Equal(expected, XboxRestAPI.SanitizeXauth(input));
    }

    [Fact]
    public void SanitizeXauth_EmptyReturnsEmpty()
    {
        Assert.Equal("", XboxRestAPI.SanitizeXauth(""));
        Assert.Equal("", XboxRestAPI.SanitizeXauth(null));
    }

    [Fact]
    public void BuildHeartbeatBody_UsesNumericIdAndRetailSandbox()
    {
        var body = XboxRestAPI.BuildHeartbeatBody("1285166951");
        Assert.Equal(
            "{\"titles\":[{\"expiration\":600,\"id\":1285166951,\"state\":\"active\",\"sandbox\":\"RETAIL\"}]}",
            body);
    }

    [Theory]
    [InlineData(" 1285166951 ")]
    [InlineData("1285166951")]
    public void BuildHeartbeatBody_ToleratesWhitespaceAroundTitleId(string titleId)
    {
        Assert.Contains("\"id\":1285166951", XboxRestAPI.BuildHeartbeatBody(titleId));
    }
}
