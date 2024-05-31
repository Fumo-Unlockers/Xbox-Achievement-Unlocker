using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet.XboxLive.Crypto;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxSignedUserTokenRequest : AbstractXboxSignedAuthRequest
    {
        public string? RelyingParty { get; set; } = XboxAuthConstants.XboxAuthRelyingParty;
        public string? TokenPrefix { get; set; } = XboxAuthConstants.XboxTokenPrefix;
        public string? AccessToken { get; set; }

        protected override string RequestUrl => "https://user.auth.xboxlive.com/user/authenticate";
        protected override object BuildBody(object proofKey)
        {
            if (string.IsNullOrEmpty(AccessToken))
                throw new InvalidOperationException("AccessToken was null");
            if (string.IsNullOrEmpty(RelyingParty))
                throw new InvalidOperationException("RelyingParty was null");

            return new
            {
                RelyingParty = RelyingParty,
                TokenType = "JWT",
                Properties = new
                {
                    AuthMethod = "RPS",
                    SiteName = "user.auth.xboxlive.com",
                    RpsTicket = TokenPrefix + AccessToken
                } 
            };
        }

        public Task<XboxAuthResponse> Send(HttpClient httpClient, IXboxRequestSigner signer)
        {
            return Send<XboxAuthResponse>(httpClient, signer);
        }
    }
}