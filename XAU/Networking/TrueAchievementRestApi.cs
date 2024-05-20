using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

public class TrueAchievementRestApi
{
    private readonly HttpClient _httpClient;

    public TrueAchievementRestApi()
    {
        // This is a placeholder for the Xbox REST API
        var handler = new HttpClientHandler()
        {
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
        };
        _httpClient = new HttpClient(handler);
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.AcceptEncoding, HeaderValues.AcceptEncoding);
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
    }

    public async Task<Tuple<List<string>, List<string>>> SearchAsync(string searchText)
    {
        SetDefaultHeaders();

        // Sanitize. TODO: actually full sanitize?
        var SearchQuerytext = Uri.EscapeDataString(searchText);
        SearchQuerytext = SearchQuerytext.Replace("%20", "+");
        var response = await _httpClient.GetAsync($"https://www.trueachievements.com/searchresults.aspx?search={SearchQuerytext}");
        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);
        var table = doc.DocumentNode.Descendants("table").FirstOrDefault(x => x.HasClass("maintable"));
        var templinks = table.Descendants("a").Select(a => a.GetAttributeValue("href", null)).Where(h => !string.IsNullOrEmpty(h)).ToList();
        var tempnames = table.Descendants("td")
              .Where(td => td.HasClass("gamerwide"))
              .Select(td => td.InnerText.Trim())
              .ToList();
        templinks.RemoveAt(0);
        templinks.RemoveAt(0);
        for (var i = 0; i < templinks.Count; i++)
        {
            templinks[i] = "https://www.trueachievements.com" + templinks[i];
            templinks[i] = templinks[i].Replace("/achievements", "/price");
            if (i > 0)
            {
                if (templinks[i - 1] == templinks[i])
                {
                    templinks.RemoveAt(i);
                    i--;
                    continue;
                }

            }
            if (!templinks[i].Contains("/game/"))
            {
                templinks.RemoveAt(i);
                templinks.RemoveAt(i);
                tempnames.RemoveAt(i);
                i--;
            }
        }
        return new Tuple<List<string>, List<string>>(tempnames, templinks);
    }

    public async Task<string> GetGameLinkAsync(XboxRestAPI xboxApi, string gameLink)
    {
        SetDefaultHeaders();
        var response = await _httpClient.GetAsync(gameLink);
        var html = await response.Content.ReadAsStringAsync();
        var doc = new HtmlDocument();
        doc.LoadHtml(html);

        var productIds = doc.DocumentNode.SelectNodes("//a[@class='price']");

        // Bundles can be a problem
        foreach (var pid in productIds)
        {
            var productId = pid.Attributes["href"].Value;
            productId = productId.Replace("/ext?u=", "");
            productId = System.Web.HttpUtility.UrlDecode(productId);
            productId = productId.Substring(0, productId.LastIndexOf('&'));
            productId = productId.Split('/').Last();
            if (productId.Contains("-"))
            {
                return Convert.ToInt32(productId.Substring(productId.Length - 8), 16).ToString();
            }
            else
            {
                var titleIDsContent = await xboxApi.GetTitleIdsFromGamePass(productId);
                var xboxTitleId = titleIDsContent.Products[$"{productId}"].XboxTitleId;
                //here is some super dumb shit to handle bundles
                if (xboxTitleId == null)
                {
                    foreach (var product in titleIDsContent.Products)
                    {
                        if (product.Value.ProductType == "Game")
                        {
                            return product.Value.XboxTitleId;
                        }
                    }
                }
            }
        }
        return "-1";
    }
}
