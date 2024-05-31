using XboxAuthNet.OAuth.CodeFlow.Parameters;

namespace XboxAuthNet.OAuth.CodeFlow;

public class CodeFlowAuthenticator
{
    private readonly ICodeFlowApiClient _client;
    private readonly ICodeFlowUrlChecker _uriChecker;
    private readonly IWebUI _ui;

    internal CodeFlowAuthenticator(
        ICodeFlowApiClient client,
        IWebUI ui,
        ICodeFlowUrlChecker urlChecker)
    {
        _client = client;
        _ui = ui;
        _uriChecker = urlChecker;
    }

    public Task<MicrosoftOAuthResponse> AuthenticateInteractively(CancellationToken cancellationToken = default)
        => AuthenticateInteractively(
            new CodeFlowAuthorizationParameter(),
            cancellationToken);

    public async Task<MicrosoftOAuthResponse> AuthenticateInteractively(
        CodeFlowAuthorizationParameter parameter,
        CancellationToken cancellationToken = default)
    {   
        var uri = _client.CreateAuthorizeCodeUrl(parameter);
        var authCode = await _ui.DisplayDialogAndInterceptUri(
            new Uri(uri), _uriChecker, cancellationToken);

        if (!authCode.IsSuccess)
        {
            throw new AuthCodeException(authCode.Error, authCode.ErrorDescription);
        }

        var tokenParameter = new CodeFlowAccessTokenParameter
        {
            Code = authCode.Code
        };
        tokenParameter.RedirectUrl = parameter.RedirectUri;
        return await _client.GetAccessToken(tokenParameter, cancellationToken);
    }

    public Task<MicrosoftOAuthResponse> AuthenticateSilently(
        string refreshToken,
        CancellationToken cancellationToken = default)
    {
        var parameter = new CodeFlowRefreshTokenParameter
        { 
            RefreshToken = refreshToken 
        };
        return _client.RefreshToken(parameter, cancellationToken);
    }

    public async Task Signout(CancellationToken cancellationToken = default)
    {
        var url = _client.CreateSignoutUrl();
        await _ui.DisplayDialogAndNavigateUri(new Uri(url), cancellationToken);
    }
}
