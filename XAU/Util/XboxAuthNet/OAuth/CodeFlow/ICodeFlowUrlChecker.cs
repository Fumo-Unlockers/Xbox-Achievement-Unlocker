namespace XboxAuthNet.OAuth.CodeFlow
{
    public interface ICodeFlowUrlChecker
    {
        CodeFlowAuthorizationResult GetAuthCodeResult(Uri uri);
    }
}
