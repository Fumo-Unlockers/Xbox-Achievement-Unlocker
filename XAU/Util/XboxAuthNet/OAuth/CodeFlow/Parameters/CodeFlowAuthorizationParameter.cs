using System.Text.Json.Serialization;

namespace XboxAuthNet.OAuth.CodeFlow.Parameters;

public class CodeFlowAuthorizationParameter : CodeFlowParameter
{
    /// <summary>
    /// response_type: id_token, token, code
    /// </summary>
    [JsonPropertyName("response_type")]
    public string? ResponseType { get; set; }

    /// <summary>
    /// redirect_uri
    /// </summary>
    [JsonPropertyName("redirect_uri")]
    public string? RedirectUri { get; set; }

    /// <summary>
    /// response_mode: query, fragment, form_post
    /// </summary>
    [JsonPropertyName("response_mode")]
    public string? ResponseMode { get; set; }

    /// <summary>
    /// state
    /// </summary>
    [JsonPropertyName("state")]
    public string? State { get; set; }

    /// <summary>
    /// prompt: login, none, consent, select_account
    /// </summary>
    [JsonPropertyName("prompt")]
    public string? Prompt { get; set; }

    /// <summary>
    /// login_hint
    /// </summary>
    [JsonPropertyName("login_hint")]
    public string? LoginHint { get; set; }

    /// <summary>
    /// domain_hint
    /// </summary>
    [JsonPropertyName("domain_hint")]
    public string? DomainHint { get; set; }

    /// <summary>
    /// code_challenge
    /// </summary>
    [JsonPropertyName("code_challenge")]
    public string? CodeChallenge { get; set; }

    /// <summary>
    /// code_challenge_method
    /// </summary>
    [JsonPropertyName("code_challenge_method")]
    public string? CodeChallengeMethod { get; set; }

    [JsonPropertyName("nonce")]
    public string? Nonce { get; set; }
}