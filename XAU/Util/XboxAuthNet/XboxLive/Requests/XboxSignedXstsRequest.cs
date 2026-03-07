using System.Threading.Tasks;
using System.Net.Http;
using XboxAuthNet.XboxLive.Crypto;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxSignedXstsRequest : AbstractXboxSignedAuthRequest
    {
        public string? UserToken { get; set; }
        public string? DeviceToken { get; set; }
        public string? TitleToken { get; set; }
        public string? RelyingParty { get; set; } = XboxAuthConstants.XboxLiveRelyingParty;
        public string[]? OptionalDisplayClaims { get; set; }

        protected override string RequestUrl => "https://xsts.auth.xboxlive.com/xsts/authorize";

        protected override object BuildBody(object proofKey)
        {
            if (string.IsNullOrEmpty(UserToken))
                throw new InvalidOperationException("UserToken was null");
            if (string.IsNullOrEmpty(RelyingParty))
                throw new InvalidOperationException("RelyingParty was null");

            return new
            {
                RelyingParty = RelyingParty,
                TokenType = "JWT",
                Properties = new
                {
                    UserTokens = new string[] { UserToken },
                    DeviceToken = DeviceToken,
                    TitleToken = TitleToken,
                    OptionalDisplayClaims = OptionalDisplayClaims,
                    SandboxId = "RETAIL",
                    ProofKey = proofKey
                }
            };
        }

        public Task<XboxAuthResponse> Send(HttpClient httpClient, IXboxRequestSigner signer)
        {
            return Send<XboxAuthResponse>(httpClient, signer);
        }
    }
}
