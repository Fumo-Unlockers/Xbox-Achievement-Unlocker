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
