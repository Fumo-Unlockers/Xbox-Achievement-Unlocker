using Newtonsoft.Json;

public class EventsUpdateResponse
{
    public int Timestamp { get; set; }
    public string? DataVersion { get; set; }
}


public class GitHubFile
{
    [JsonProperty("name")]
    public string Name { get; set; }

    [JsonProperty("path")]
    public string Path { get; set; }

    [JsonProperty("sha")]
    public string Sha { get; set; }

    [JsonProperty("size")]
    public long Size { get; set; }

    [JsonProperty("download_url")]
    public string DownloadUrl { get; set; }
}

public class GitHubReleaseAsset
{
    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("browser_download_url")]
    public string? BrowserDownloadUrl { get; set; }
}

public class GitHubRelease
{
    [JsonProperty("tag_name")]
    public string? TagName { get; set; }

    [JsonProperty("name")]
    public string? Name { get; set; }

    [JsonProperty("body")]
    public string? Body { get; set; }

    [JsonProperty("html_url")]
    public string? HtmlUrl { get; set; }

    [JsonProperty("assets")]
    public List<GitHubReleaseAsset>? Assets { get; set; } = new();

    [JsonProperty("draft")]
    public bool Draft { get; set; }

    [JsonProperty("prerelease")]
    public bool Prerelease { get; set; }
}
