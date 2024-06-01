using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Responses
{
    public class XboxSisuResponse
    {
        [JsonPropertyName("DeviceToken")]
        public string? DeviceToken { get; set; }

        [JsonPropertyName("TitleToken")]
        public XboxAuthResponse? TitleToken { get; set; }

        [JsonPropertyName("UserToken")]
        public XboxAuthResponse? UserToken { get; set; }

        [JsonPropertyName("AuthorizationToken")]
        public XboxAuthResponse? AuthorizationToken { get; set; }

        [JsonPropertyName("WebPage")]
        public string? WebPage { get; set; }

        [JsonPropertyName("Sandbox")]
        public string? Sandbox { get; set; }

        [JsonPropertyName("UseModernGamerTag")]
        public string? UseModernGamertag { get; set; }
    }
}
