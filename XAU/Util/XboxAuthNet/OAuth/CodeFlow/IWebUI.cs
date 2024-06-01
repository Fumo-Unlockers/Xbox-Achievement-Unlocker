// This code is from MSAL.NET

namespace XboxAuthNet.OAuth.CodeFlow;

public interface IWebUI
{
    Task<CodeFlowAuthorizationResult> DisplayDialogAndInterceptUri(Uri uri, ICodeFlowUrlChecker uriChecker, CancellationToken cancellationToken);
    Task DisplayDialogAndNavigateUri(Uri uri, CancellationToken cancellationToken);
}