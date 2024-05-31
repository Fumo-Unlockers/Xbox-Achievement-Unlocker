using System.Web;

namespace XboxAuthNet.OAuth.CodeFlow;

public class CodeFlowUrlChecker : ICodeFlowUrlChecker
{
    public CodeFlowAuthorizationResult GetAuthCodeResult(Uri uri)
    {
        var query = HttpUtility.ParseQueryString(uri.Query);
        var authCode = new CodeFlowAuthorizationResult
        {
            Code = query["code"],
            IdToken = query["id_token"],
            State = query["state"],
            Error = query["error"],
            ErrorDescription = HttpUtility.UrlDecode(query["error_description"])
        };
        return authCode;
    }
}
