using System.Text.Json.Serialization;

namespace XboxAuthNet.OAuth;

// https://docs.microsoft.com/en-us/azure/active-directory/develop/id-tokens#payload-claims
public class MicrosoftUserPayload
{
    [JsonPropertyName("aud")]
    public string? ApplicationId { get; set; }

    [JsonPropertyName("iss")]
    public string? Issuer { get; set; }

    [JsonPropertyName("iat")]
    public long IssuedAt { get; set; }

    [JsonPropertyName("idp")]
    public string? IdentityProvider { get; set; }

    [JsonPropertyName("nbf")]
    public long NotBefore { get; set; }

    [JsonPropertyName("exp")]
    public long ExpiresOn { get; set; }

    [JsonPropertyName("c_hash")]
    public string? CodeHash { get; set; }

    [JsonPropertyName("at_hash")]
    public string? AccessTokenHash { get; set; }

    [JsonPropertyName("preferred_username")]
    public string? Username { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }

    [JsonPropertyName("oid")]
    public string? UserId { get; set; }

    [JsonPropertyName("roles")]
    public string[]? Roles { get; set; }

    [JsonPropertyName("sub")]
    public string? Subject { get; set; }

    [JsonPropertyName("tid")]
    public string? Tenant { get; set; }

    [JsonPropertyName("unique_name")]
    public string? UniqueName { get; set; }

    [JsonPropertyName("uti")]
    public string? TokenIdentifier { get; set; }

    [JsonPropertyName("ver")]
    public string? Version { get; set; }

    [JsonPropertyName("hasgroups")]
    public bool HasGroups { get; set; }
}
