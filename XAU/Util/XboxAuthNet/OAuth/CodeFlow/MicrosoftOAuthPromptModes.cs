namespace XboxAuthNet.OAuth.CodeFlow;

public static class MicrosoftOAuthPromptModes
{
    /// <summary>
    /// Forces the user to enter their credentials on that request, negating single-sign on.
    /// </summary>
    public const string Login = "login";

    /// <summary>
    /// Ensures that the user isn't presented with any interactive prompt. 
    /// If the request can't be completed silently by using sigle-sign on, 
    /// the Microsoft identity platform returns an `interaction_required` error.
    /// </summary>
    public const string None = "none";

    /// <summary>
    /// Triggers the OAuth consent dialog after the user signs in, asking the user to grant permissions to the app.
    /// </summary>
    public const string Consent = "consent";

    /// <summary>
    /// Interrupts single sign-on providing account selection experience listing all the accounts 
    /// either in session or any remembered account or an option to choose to use a different account altogether.
    /// </summary>
    public const string SelectAccount = "select_account";
}
