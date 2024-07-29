using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using Newtonsoft.Json;
using System.Diagnostics;

public class IGDBRestApi
{
    private readonly HttpClient _httpClient;

    public IGDBRestApi()
    {
        _httpClient = new HttpClient();
    }

    private void SetDefaultHeaders()
    {
        _httpClient.DefaultRequestHeaders.Clear();
        _httpClient.DefaultRequestHeaders.Add(HeaderNames.Accept, HeaderValues.Accept);
    }

    public async Task<Tuple<List<string>, List<string>>> SearchAsync(string searchText)
    {
        SetDefaultHeaders();

        var searchQuery = new { search = searchText };
        var content = new StringContent(JsonConvert.SerializeObject(searchQuery), System.Text.Encoding.UTF8, "application/json");

        var response = await _httpClient.PostAsync("https://xau.lol/search", content);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
        {
            Debug.WriteLine("No games returned from your search query");
            return new Tuple<List<string>, List<string>>(new List<string>(), new List<string>());
        }

        response.EnsureSuccessStatusCode();
        var jsonResponse = await response.Content.ReadAsStringAsync();
        Debug.WriteLine($"XAUGames API Response: {jsonResponse}");

        var searchResults = JArray.Parse(jsonResponse);

        var gameNames = new List<string>();
        var xboxTitleIds = new List<string>();

        foreach (var result in searchResults)
        {
            var productTitle = result["ProductTitle"]?.ToString();
            var xboxTitleId = result["XboxTitleId"]?.ToString();

            if (productTitle != null && xboxTitleId != null)
            {
                gameNames.Add(productTitle);
                xboxTitleIds.Add(xboxTitleId);
            }
        }

        return new Tuple<List<string>, List<string>>(gameNames, xboxTitleIds);
    }
}
