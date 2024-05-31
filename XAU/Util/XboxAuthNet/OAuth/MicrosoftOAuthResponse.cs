using System.Text.Json;
using System.Text.Json.Serialization;
using XboxAuthNet.Jwt;

namespace XboxAuthNet.OAuth;

public class MicrosoftOAuthResponse
{
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("token_type")]
    public string? TokenType { get; set; }

    [JsonPropertyName("expires_in")]
    public int ExpireIn { get; set; }

    [JsonPropertyName("expires_on")]
    public DateTimeOffset ExpiresOn { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RawRefreshToken { get; set; }

    [JsonIgnore]
    public string? RefreshToken
    {
        get => RawRefreshToken?.Split('.')?.Last();
        set => RawRefreshToken = "M.R3_BAY." + value;
    }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    public MicrosoftUserPayload? DecodeIdTokenPayload()
    {
        if (string.IsNullOrEmpty(IdToken))
            return null;

        return JwtDecoder.DecodePayload<MicrosoftUserPayload>(IdToken!);
    }

    public bool Validate()
    {
        if (string.IsNullOrEmpty(AccessToken))
            return false;
        
        if (DateTime.UtcNow > ExpiresOn)
            return false;

        return true;
    }

    public static MicrosoftOAuthResponse FromHttpResponse(string resBody, int statusCode, string? reasonPhrase)
    {
        if (statusCode / 100 != 2)
            throw MicrosoftOAuthException.FromResponseBody(resBody, statusCode, reasonPhrase);

        try
        {
            var resObj = JsonSerializer.Deserialize<MicrosoftOAuthResponse>(resBody);
            if (resObj == null)
                throw new MicrosoftOAuthException("The response was empty.", statusCode);

            if (resObj.ExpiresOn == default)
                resObj.ExpiresOn = DateTimeOffset.UtcNow.AddSeconds(resObj.ExpireIn);
            return resObj;
        }
        catch (JsonException)
        {
            throw MicrosoftOAuthException.FromResponseBody(resBody, statusCode, reasonPhrase);
        }
    }
}
