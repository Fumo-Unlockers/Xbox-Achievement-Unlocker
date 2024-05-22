using System.Net;
using System.Net.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

public class GithubRestApi
{
    private readonly HttpClient _httpClient;

    // User specifics
    public GithubRestApi()
    {
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add("User-Agent",
            "Mozilla/5.0 (Windows NT 10.0; Win64; x64; rv:108.0) Gecko/20100101 Firefox/108.0");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, "gzip, deflate, br");
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept,
            "text/html,application/xhtml+xml,application/xml;q=0.9,image/avif,image/webp,*/*;q=0.8");
    }

    public async Task<VersionResponse?> GetDevToolVersionAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubRaw);
        var responseString =
            await _httpClient.GetStringAsync("https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Pre-Release/info.json");
        return JsonConvert.DeserializeObject<VersionResponse>(responseString);
    }

    public async Task<dynamic> GetReleaseVersionAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubApi);
        var responseString =
            await _httpClient.GetStringAsync("https://api.github.com/repos/Fumo-Unlockers/Xbox-Achievement-unlocker/releases");
        var jsonResponse = (dynamic)JArray.Parse(responseString);
        return jsonResponse;
    }

    public async Task<EventsUpdateResponse?> CheckForEventUpdatesAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubRaw);
        var responseString = await _httpClient.GetStringAsync("https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Events-Data/meta.json");
        return JsonConvert.DeserializeObject<EventsUpdateResponse>(responseString);
    }
}
