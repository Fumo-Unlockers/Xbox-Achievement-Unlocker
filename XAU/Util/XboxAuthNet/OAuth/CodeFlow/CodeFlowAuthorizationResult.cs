namespace XboxAuthNet.OAuth.CodeFlow;

public struct CodeFlowAuthorizationResult
{
    public string? Code { get; set; }
    public string? IdToken { get; set; }
    public string? State { get; set; }
    public string? Error { get; set; }
    public string? ErrorDescription { get; set; }

    public bool IsSuccess
       => !string.IsNullOrEmpty(Code)
        && string.IsNullOrEmpty(Error);

    public bool IsEmpty
        => string.IsNullOrEmpty(Code)
        && string.IsNullOrEmpty(Error)
        && string.IsNullOrEmpty(ErrorDescription);
}
