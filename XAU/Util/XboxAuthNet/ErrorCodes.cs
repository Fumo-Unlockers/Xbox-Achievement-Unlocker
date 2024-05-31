namespace XboxAuthNet
{
    // https://github.com/microsoft/xbox-live-api/blob/f1a347b91f5f5dae62c35623719ecf8b9ba68746/Source/Shared/errors_legacy.h#L875
    public class ErrorCodes
    {
        /// <summary>
        /// <b>0x8015DC00</b>
        /// Developer mode is not authorized for the client device.
        /// </summary>
        const uint XO_E_DEVMODE_NOT_AUTHORIZED = (uint)0x8015DC00,

        /// <summary>
        /// <b>0x8015DC01</b>
        /// A system update is required before this action can be performed.
        /// </summary>
        XO_E_SYSTEM_UPDATE_REQUIRED = (uint)0x8015DC01,

        /// <summary>
        /// <b>0x8015DC02</b>
        /// A content update is required before this action can be performed.
        /// </summary>
        XO_E_CONTENT_UPDATE_REQUIRED = (uint)0x8015DC02,

        /// <summary>
        /// <b>0x8015DC03</b>
        /// The device or user was banned.
        /// </summary>
        XO_E_ENFORCEMENT_BAN = (uint)0x8015DC03,

        /// <summary>
        /// <b>0x8015DC04</b>
        /// The device or user was banned.
        /// </summary>
        XO_E_THIRD_PARTY_BAN = (uint)0x8015DC04,

        /// <summary>
        /// <b>0x8015DC05</b>
        /// Access to this resource has been parentally restricted.
        /// </summary>
        XO_E_ACCOUNT_PARENTALLY_RESTRICTED = (uint)0x8015DC05,

        /// <summary>
        /// <b>0x8015DC08</b>
        /// Access to this resource requires that the account billing information
        /// is updated.
        /// </summary>
        XO_E_ACCOUNT_BILLING_MAINTENANCE_REQUIRED = (uint)0x8015DC08,

        /// <summary>
        /// <b>0x8015DC0A</b>
        /// The user has not accepted the terms of use for this resource.
        /// </summary>
        XO_E_ACCOUNT_TERMS_OF_USE_NOT_ACCEPTED = (uint)0x8015DC0A,

        /// <summary>
        /// <b>0x8015DC0B</b>
        /// This resource is not available in the country associated with the user.
        /// </summary>
        XO_E_ACCOUNT_COUNTRY_NOT_AUTHORIZED = (uint)0x8015DC0B,

        /// <summary>
        /// <b>0x8015DC0C</b>
        /// Access to this resource requires age verification.
        /// </summary>
        XO_E_ACCOUNT_AGE_VERIFICATION_REQUIRED = (uint)0x8015DC0C,

        /// <summary>
        /// <b>0x8015DC0D</b>
        /// </summary>
        XO_E_ACCOUNT_CURFEW = (uint)0x8015DC0D,

        /// <summary>
        /// <b>0x8015DC0E</b>
        /// </summary>
        XO_E_ACCOUNT_CHILD_NOT_IN_FAMILY = (uint)0x8015DC0E,

        /// <summary>
        /// <b>0x8015DC0F</b>
        /// </summary>
        XO_E_ACCOUNT_CSV_TRANSITION_REQUIRED = (uint)0x8015DC0F,

        /// <summary>
        /// <b>0x8015DC09</b>
        /// </summary>
        XO_E_ACCOUNT_CREATION_REQUIRED = (uint)0x8015DC09,

        /// <summary>
        /// <b>0x8015DC10</b>
        /// </summary>
        XO_E_ACCOUNT_MAINTENANCE_REQUIRED = (uint)0x8015DC10,

        /// <summary>
        /// <b>0x8015DC11</b>
        /// The call was blocked because there was a conflict with the sandbox, console, application, or 
        /// your account.Verify your account, console and title settings in XDP, and check the current 
        /// Sandbox on the device.
        /// </summary>
        XO_E_ACCOUNT_TYPE_NOT_ALLOWED = (uint)0x8015DC11,

        /// <summary>
        /// <b>0x8015DC12</b>
        /// Your device does not have access to the Sandbox it is set to, or the account you are signed 
        /// in with does not have access to the Sandbox.Check that you are using the correct Sandbox.
        ///
        /// Note: All XDK samples use XDKS.1 SandboxID, which allow all user accounts to access and run 
        /// the samples.SandboxID's are case sensitive- Not matching the case of your SandboxID exactly may 
        /// result in errors. If you are still having issues running the sample, please work with your 
        /// Developer Account Manager and provide a fiddler trace to help with troubleshooting.
        ///
        /// For more information on handling this error, please see the "Troubleshooting Sign-in" article
        /// in the Xbox Live documentation
        /// </summary>
        XO_E_CONTENT_ISOLATION = (uint)0x8015DC12,

        /// <summary>
        /// <b>0x8015DC13</b>
        /// </summary>
        XO_E_ACCOUNT_NAME_CHANGE_REQUIRED = (uint)0x8015DC13,

        /// <summary>
        /// <b>0x8015DC14</b>
        /// </summary>
        XO_E_DEVICE_CHALLENGE_REQUIRED = (uint)0x8015DC14,

        /// <summary>
        /// <b>0x8015DC16</b>
        /// The account was signed in on another device.
        /// </summary>
        XO_E_SIGNIN_COUNT_BY_DEVICE_TYPE_EXCEEDED = (uint)0x8015DC16,

        /// <summary>
        /// <b>0x8015DC17</b>
        /// </summary>
        XO_E_PIN_CHALLENGE_REQUIRED = (uint)0x8015DC17,

        /// <summary>
        /// <b>0x8015DC18</b>
        /// </summary>
        XO_E_RETAIL_ACCOUNT_NOT_ALLOWED = (uint)0x8015DC18,

        /// <summary>
        /// <b>0x8015DC19</b>
        /// The current sandbox is not allowed to access the SCID.  Please ensure that your current
        /// sandbox is set to your development sandbox.  If you are running on a Windows 10 PC, then
        /// you can change your current sandbox using the SwitchSandbox.cmd script in the Xbox Live SDK
        /// tools directory.  If you are using an Xbox One, you can switch the sandbox using Xbox One
        /// Manager.
        ///
        /// For more information on handling this error, please see the "Troubleshooting Sign-in" article
        /// in the Xbox Live documentation.
        /// </summary>
        XO_E_SANDBOX_NOT_ALLOWED = (uint)0x8015DC19,

        /// <summary>
        /// <b>0x8015DC1A</b>
        /// </summary>
        XO_E_ACCOUNT_SERVICE_UNAVAILABLE_UNKNOWN_USER = (uint)0x8015DC1A,

        /// <summary>
        /// <b>0x8015DC1B</b>
        /// </summary>
        XO_E_GREEN_SIGNED_CONTENT_NOT_AUTHORIZED = (uint)0x8015DC1B,

        /// <summary>
        /// <b>0x8015DC1C</b>
        /// </summary>
        XO_E_CONTENT_NOT_AUTHORIZED = (uint)0x8015DC1C;
    }
}
