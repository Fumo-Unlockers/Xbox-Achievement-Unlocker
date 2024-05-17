public class GameTitleRequest
{
    public String? Pfns { get; set; }
    public List<string> TitleIds { get; set; }
}


public class HeartbeatRequest
{
    public List<TitleRequest> titles { get; set; }
}

public class TitleRequest
{
    public int expiration { get; set; }
    public string id { get; set; }
    public string state { get; set; }
    public string sandbox { get; set; }
}

public class GamepassProductsRequest
{
    public List<string> Products { get; set; }
}
