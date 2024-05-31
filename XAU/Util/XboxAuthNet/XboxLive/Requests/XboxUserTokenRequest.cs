using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxUserTokenRequest : AbstractXboxAuthRequest
    {
        public const string UserAuthenticateUrl = "https://user.auth.xboxlive.com/user/authenticate";

        public XboxUserTokenRequest()
        {
            ContractVersion = "0";
            RelyingParty = XboxAuthConstants.XboxAuthRelyingParty;
        }

        public string? AccessToken { get; set; }
        public string? RelyingParty { get; set; }

        protected override HttpRequestMessage BuildRequest()
        {
            if (string.IsNullOrEmpty(AccessToken))
                throw new InvalidOperationException("AccessToken was null");

            var req = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(UserAuthenticateUrl),
                Content = HttpHelper.CreateJsonContent(new
                {
                    RelyingParty = RelyingParty,
                    TokenType = "JWT",
                    Properties = new
                    {
                        AuthMethod = "RPS",
                        SiteName = "user.auth.xboxlive.com",
                        RpsTicket = AccessToken
                    }
                })
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