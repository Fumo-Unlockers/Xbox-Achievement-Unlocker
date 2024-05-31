using System.Net.Http;
using XboxAuthNet.XboxLive.Requests;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive;

// https://github.com/PrismarineJS/prismarine-auth/blob/master/src/TokenManagers/XboxTokenManager.js
public class XboxAuthClient
{
    private readonly HttpClient _httpClient;

    public XboxAuthClient(HttpClient httpClient) =>
        _httpClient = httpClient;

    public Task<XboxAuthResponse> RequestUserToken(string rps) =>
        RequestUserToken(new XboxUserTokenRequest()
        {
            AccessToken = rps
        });

    public Task<XboxAuthResponse> RequestUserToken(XboxUserTokenRequest request) =>
        request.Send(_httpClient);

    public Task<XboxAuthResponse> RequestXsts(string userToken) =>
        RequestXsts(new XboxXstsRequest
        {
            UserToken = userToken
        });

    public Task<XboxAuthResponse> RequestXsts(string userToken, string relyingParty) =>
        RequestXsts(new XboxXstsRequest
        {
            UserToken = userToken,
            RelyingParty = relyingParty
        });

    public Task<XboxAuthResponse> RequestXsts(XboxXstsRequest request) =>
        request.Send(_httpClient);
}
