using System.Net.Http;
using XboxAuthNet.OAuth.CodeFlow.Parameters;

namespace XboxAuthNet.OAuth.CodeFlow;

public class CodeFlowLiveApiClient : ICodeFlowApiClient
{
    public const string OAuthDesktop = "https://login.live.com/oauth20_desktop.srf";
    public const string OAuthAuthorize = "https://login.live.com/oauth20_authorize.srf";
    public const string OAuthErrorPath = "/err.srf";
    public const string OAuthToken = "https://login.live.com/oauth20_token.srf";

    private readonly HttpClient httpClient;

    public CodeFlowLiveApiClient(string clientId, string scope, HttpClient client)
    {
        ClientId = clientId;
        Scope = scope;
        httpClient = client;
    }

    public string ClientId { get; }
    public string Scope { get; }

    public string CreateAuthorizeCodeUrl(CodeFlowAuthorizationParameter parameter)
    {
        setCommonParameters(parameter);
        if (string.IsNullOrEmpty(parameter.RedirectUri))
            parameter.RedirectUri = OAuthDesktop;
        if (string.IsNullOrEmpty(parameter.ResponseType))
            parameter.ResponseType = "code";
        if (string.IsNullOrEmpty(parameter.ResponseMode))
            parameter.ResponseMode = "query";
        if (string.IsNullOrEmpty(parameter.Prompt))
            parameter.Prompt = "select_account";

        var query = parameter.ToQueryDictionary();
        return OAuthAuthorize + "?" + HttpHelper.GetQueryString(query);
    }

    public string CreateSignoutUrl() =>
        CreateSignoutUrl("consumer");

    public string CreateSignoutUrl(string tenant) =>
        $"https://login.microsoftonline.com/{tenant}/oauth2/v2.0/logout";

    public Task<MicrosoftOAuthResponse> GetAccessToken(
        CodeFlowAccessTokenParameter parameter,
        CancellationToken cancellationToken) =>
        requestToken(setAccessTokenParameters(parameter), cancellationToken);

    private CodeFlowAccessTokenParameter setAccessTokenParameters(CodeFlowAccessTokenParameter parameters)
    {
        if (string.IsNullOrEmpty(parameters.RedirectUrl))
            parameters.RedirectUrl = OAuthDesktop;
        if (string.IsNullOrEmpty(parameters.GrantType))
            parameters.GrantType = "authorization_code";
        return parameters;
    }

    public Task<MicrosoftOAuthResponse> RefreshToken(
        CodeFlowRefreshTokenParameter parameter,
        CancellationToken cancellationToken) =>
        requestToken(setRefreshTokenParameters(parameter), cancellationToken);

    private CodeFlowRefreshTokenParameter setRefreshTokenParameters(CodeFlowRefreshTokenParameter parameters)
    {
        if (string.IsNullOrEmpty(parameters.GrantType))
            parameters.GrantType = "refresh_token";
        return parameters;
    }

    private async Task<MicrosoftOAuthResponse> requestToken(
        CodeFlowParameter parameter,
        CancellationToken cancellationToken)
    {
        setCommonParameters(parameter);
        var queryDict = parameter.ToQueryDictionary();
        return await microsoftOAuthRequest(new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(OAuthToken),
            Content = new FormUrlEncodedContent(queryDict!)
        }, cancellationToken).ConfigureAwait(false);
    }

    private async Task<MicrosoftOAuthResponse> microsoftOAuthRequest(
        HttpRequestMessage req,
        CancellationToken cancellationToken)
    {
        req.Headers.Add("User-Agent", HttpHelper.UserAgent);
        req.Headers.Add("Accept-Encoding", "gzip");
        req.Headers.Add("Accept-Language", "en-US");

        var res = await httpClient.SendAsync(req, cancellationToken)
            .ConfigureAwait(false);

        var resBody = await res.Content.ReadAsStringAsync()
            .ConfigureAwait(false);
        var statusCode = (int)res.StatusCode;
        var reasonPhrase = res.ReasonPhrase;
        return MicrosoftOAuthResponse.FromHttpResponse(resBody, statusCode, reasonPhrase);
    }

    private void setCommonParameters(CodeFlowParameter parameter)
    {
        parameter.Tenant = "consumer";

        if (string.IsNullOrEmpty(parameter.ClientId))
            parameter.ClientId = ClientId;
        if (string.IsNullOrEmpty(parameter.Scope))
            parameter.Scope = Scope;
    }
}
