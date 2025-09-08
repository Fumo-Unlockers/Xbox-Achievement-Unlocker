using Newtonsoft.Json;

public class EventsUpdateResponse
{
    public int Timestamp { get; set; }
    public string? DataVersion { get; set; }
}

public class VersionResponse
{
    public string? DownloadURL { get; set; }
    public string? LatestBuildVersion { get; set; }
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
