using System.Net;
using System.Net.Http;
using HtmlAgilityPack;
using Newtonsoft.Json.Linq;

public class DBoxRestApi
{
    private readonly HttpClient _httpClient;

    public DBoxRestApi()
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

    public async Task<JObject> SearchAsync(string searchText)
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, "application/json");

        searchText = Uri.EscapeDataString(searchText);

        var response = await _httpClient.GetAsync($"https://dbox.tools/api/title_ids/?name={searchText}&limit=100&offset=0");
        var jsonString = await response.Content.ReadAsStringAsync();
        if (response.IsSuccessStatusCode)
        {
            return JObject.Parse(jsonString);
        }
        else
        {
            throw new HttpRequestException($"Error fetching data: {response.StatusCode} - {jsonString}");
        }
    }
}
