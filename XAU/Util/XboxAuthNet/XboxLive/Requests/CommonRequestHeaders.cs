using System.Net.Http;

namespace XboxAuthNet.XboxLive.Requests;

internal class CommonRequestHeaders
{
    public static void AddDefaultHeaders(HttpRequestMessage request)
    {
        request.Headers.Add("Accept", "application/json");
        request.Headers.TryAddWithoutValidation("User-Agent", HttpHelper.UserAgent);
        request.Headers.Add("Accept-Language", "en-US");
        request.Headers.Add("Cache-Control", "no-store, must-revalidate, no-cache");
    }
}
