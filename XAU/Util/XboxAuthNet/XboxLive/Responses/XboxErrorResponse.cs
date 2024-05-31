using System.Text.Json.Serialization;

namespace XboxAuthNet.XboxLive.Responses
{
    public class XboxErrorResponse
    {
        [JsonPropertyName("XErr")]
        [JsonConverter(typeof(XErrJsonConverter))]
        public string? XErr { get; set; }

        [JsonPropertyName("Message")]
        public string? Message { get; set; }

        [JsonPropertyName("Redirect")]
        public string? Redirect { get; set; }
    }
}
