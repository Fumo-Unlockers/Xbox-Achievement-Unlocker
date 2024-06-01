using System.Text.Json.Serialization;

namespace XboxAuthNet.OAuth.CodeFlow.Parameters;

public class CodeFlowRefreshTokenParameter : CodeFlowParameter
{
    [JsonPropertyName("grant_type")]
    public string? GrantType { get; set; }

    [JsonPropertyName("client_secret")]
    public string? ClientSecret { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}