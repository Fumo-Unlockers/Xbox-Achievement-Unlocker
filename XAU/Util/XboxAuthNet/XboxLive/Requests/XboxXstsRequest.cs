using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxXstsRequest : AbstractXboxAuthRequest
    {
        public const string XstsAuthorizeUrl = "https://xsts.auth.xboxlive.com/xsts/authorize";

        public XboxXstsRequest()
        {
            RelyingParty = XboxAuthConstants.XboxLiveRelyingParty;
            ContractVersion = "1";
        }

        public string? UserToken { get; set; }
        public string? RelyingParty { get; set; }
        public string? DeviceToken { get; set; }
        public string? TitleToken { get; set; }
        public string[]? OptionalDisplayClaims { get; set; }

        protected override HttpRequestMessage BuildRequest()
        {
            if (string.IsNullOrEmpty(UserToken))
                throw new InvalidOperationException("UserToken was null");
            if (string.IsNullOrEmpty(RelyingParty))
                throw new InvalidOperationException("RelyingParty was null");
            
            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(XstsAuthorizeUrl),
                Content = HttpHelper.CreateJsonContent(new
                {
                    RelyingParty = RelyingParty,
                    TokenType = "JWT",
                    Properties = new
                    {
                        UserTokens = new string[] { UserToken },
                        DeviceToken = DeviceToken,
                        TitleToken = TitleToken,
                        OptionalDisplayClaims = OptionalDisplayClaims,
                        SandboxId = "RETAIL"
                    }
                }),
            };

            CommonRequestHeaders.AddDefaultHeaders(req);
            return req;
        }

        public Task<XboxAuthResponse> Send(HttpClient httpClient)
        {
            return Send<XboxAuthResponse>(httpClient);
        }
    }
}