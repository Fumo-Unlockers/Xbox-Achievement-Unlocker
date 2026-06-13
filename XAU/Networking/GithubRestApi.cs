using System.Net;
using System.Net.Http;
using Newtonsoft.Json;

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
        _httpClient = new HttpClient(handler)
        {
            Timeout = TimeSpan.FromSeconds(15)
        };
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

    public async Task<GitHubRelease?> GetLatestReleaseAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubApi);
        var responseString =
            await _httpClient.GetStringAsync("https://api.github.com/repos/Fumo-Unlockers/Xbox-Achievement-unlocker/releases/latest");
        return JsonConvert.DeserializeObject<GitHubRelease>(responseString);
    }

    // /releases/latest excludes pre-releases, so to find the newest pre-release we list
    // all releases (returned newest-first) and pick the first one flagged as a prerelease.
    public async Task<GitHubRelease?> GetLatestPreReleaseAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubApi);
        var responseString =
            await _httpClient.GetStringAsync("https://api.github.com/repos/Fumo-Unlockers/Xbox-Achievement-unlocker/releases");
        var releases = JsonConvert.DeserializeObject<List<GitHubRelease>>(responseString);
        return releases?.FirstOrDefault(r => r.Prerelease && r.TagName != null);
    }

    public async Task<EventsUpdateResponse?> CheckForEventUpdatesAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubRaw);
        var responseString = await _httpClient.GetStringAsync("https://raw.githubusercontent.com/Fumo-Unlockers/Xbox-Achievement-Unlocker/Events-Data/meta.json");
        return JsonConvert.DeserializeObject<EventsUpdateResponse>(responseString);
    }


    public async Task<GitHubFile?> GetXboxGamesDatabaseInfoAsync()
    {
        SetDefaultHeaders();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Host, Hosts.GitHubApi);
        var responseString = await _httpClient.GetStringAsync("https://api.github.com/repos/Fumo-Unlockers/XboxGames/contents");
        var files = JsonConvert.DeserializeObject<List<GitHubFile>>(responseString);
        return files?.FirstOrDefault(f => f.Name.Equals("xbox_games.db", StringComparison.OrdinalIgnoreCase));
    }

}
