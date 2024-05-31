using System.Net.Http;
using XboxAuthNet.XboxLive.Crypto;
using XboxAuthNet.XboxLive.Requests;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive;

public class XboxSignedClient
{
    private readonly HttpClient _httpClient;
    private readonly IXboxRequestSigner _signer;

    public XboxSignedClient(HttpClient httpClient)
    {
        _httpClient = httpClient;
        _signer = new XboxRequestSigner(new ECDCertificatePopCryptoProvider());
    }

    public XboxSignedClient(IXboxRequestSigner signer, HttpClient httpClient)
    {
        _signer = signer;
        _httpClient = httpClient;
    }

    public Task<XboxAuthResponse> RequestSignedUserToken(string rps) =>
        RequestSignedUserToken(new XboxSignedUserTokenRequest()
        {
            AccessToken = rps
        });

    public Task<XboxAuthResponse> RequestSignedUserToken(XboxSignedUserTokenRequest request) =>
        request.Send(_httpClient, _signer);

    public Task<XboxAuthResponse> RequestDeviceToken(string deivceType, string deviceVersion) =>
        RequestDeviceToken(new XboxDeviceTokenRequest()
        {
            DeviceType = deivceType,
            DeviceVersion = deviceVersion
        });

    public Task<XboxAuthResponse> RequestDeviceToken(XboxDeviceTokenRequest request) =>
        request.Send(_httpClient, _signer);

    public Task<XboxAuthResponse> RequestTitleToken(string accessToken, string deviceToken) =>
        RequestTitleToken(new XboxTitleTokenRequest()
        {
            AccessToken = accessToken,
            DeviceToken = deviceToken
        });

    public Task<XboxAuthResponse> RequestTitleToken(XboxTitleTokenRequest request) =>
        request.Send(_httpClient, _signer);

    public Task<XboxSisuResponse> SisuAuth(XboxSisuAuthRequest request) =>
        request.Send(_httpClient, _signer);
}
