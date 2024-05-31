using System;
using System.Net.Http;
using System.Threading.Tasks;
using XboxAuthNet.XboxLive.Crypto;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests
{
    public class XboxDeviceTokenRequest : AbstractXboxSignedAuthRequest
    {
        public string? Id { get; set; }
        public string? SerialNumber { get; set; }
        public string? DeviceType { get; set; }
        public string? DeviceVersion { get; set; }
        public string? RelyingParty { get; set; } = XboxAuthConstants.XboxAuthRelyingParty;

        protected override string RequestUrl => "https://device.auth.xboxlive.com/device/authenticate";
        protected override object BuildBody(object proofKey)
        {
            if (string.IsNullOrEmpty(DeviceType))
                throw new InvalidOperationException("DeviceType was null");
            if (string.IsNullOrEmpty(DeviceVersion))
                throw new InvalidOperationException("DeviceVersion was null");
            if (string.IsNullOrEmpty(RelyingParty))
                throw new InvalidOperationException("RelyingParty was null");

            var id = this.Id ?? nextUUID();
            var serialNumber = this.SerialNumber ?? nextUUID();

            return new
            {
                Properties = new
                {
                    AuthMethod = "ProofOfPossession",
                    Id = "{" + id + "}",
                    DeviceType = DeviceType,
                    SerialNumber = "{" + serialNumber + "}",
                    Version = DeviceVersion,
                    ProofKey = proofKey
                },
                RelyingParty = RelyingParty,
                TokenType = "JWT"
            };
        }

        private string nextUUID()
        {
            return Guid.NewGuid().ToString();
        }

        public Task<XboxAuthResponse> Send(HttpClient httpClient, IXboxRequestSigner signer)
        {
            return Send<XboxAuthResponse>(httpClient, signer);
        }
    }
}