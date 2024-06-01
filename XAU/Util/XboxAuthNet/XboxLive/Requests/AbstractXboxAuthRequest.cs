using System.Net.Http;
using XboxAuthNet.XboxLive.Responses;

namespace XboxAuthNet.XboxLive.Requests;

public abstract class AbstractXboxAuthRequest
{
    public XboxAuthResponseHandler? ResponseHandler { get; set; } = new();
    public string? ContractVersion { get; set; }

    protected abstract HttpRequestMessage BuildRequest();

    public async Task<T> Send<T>(HttpClient httpClient)
    {
        if (ResponseHandler == null)
            throw new InvalidOperationException("ResponseHandler was null");

        var request = BuildRequest();
        request.Headers.Add("x-xbl-contract-version", ContractVersion ?? "");

        var response = await httpClient.SendAsync(request);
        return await ResponseHandler.HandleResponse<T>(response);
    }
}
