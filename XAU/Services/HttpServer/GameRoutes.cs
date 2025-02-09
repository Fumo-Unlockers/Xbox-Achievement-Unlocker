using Newtonsoft.Json;
using System.Net;
using System.Text;

public static class GameRoutes
{
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes(Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        return new Dictionary<string, Func<HttpListenerContext, Task>>
    {
        { "/api/games/me", async context => await GamesMeRequest(context, getXboxRestAPI, getXUIDOnly) },
        { "/api/games/xuid/", async context => await GamesXuidRequest(context, getXboxRestAPI) },
        { "/api/games/gt/", async context => await GamesGamertagRequest(context, getXboxRestAPI) },
        { "/api/games/search/titleid/", async context => await GameTitleIDSearchRequest(context, getXboxRestAPI, getXUIDOnly) },
        { "/api/games/search/productid/", async context => await GameProductIDSearchRequest(context, getXboxRestAPI) }


    };
    }

    private static async Task GamesMeRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var response = context.Response;

        try
        {
            string xuid = getXUIDOnly?.Invoke();
            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID found" });
                return;
            }

            var xboxRestAPI = getXboxRestAPI();
            var gamesList = await xboxRestAPI.GetGamesListAsync(xuid);

            if (gamesList == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Games list not found" });
                return;
            }

            await SendJsonResponse(response, gamesList);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private static async Task GamesXuidRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 3)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID provided in URL" });
                return;
            }

            string xuid = request.Url.Segments.Last().TrimEnd('/');
            var xboxRestAPI = getXboxRestAPI();

            var gamesList = await xboxRestAPI.GetGamesListAsync(xuid);

            if (gamesList == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Games list not found" });
                return;
            }

            await SendJsonResponse(response, gamesList);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private static async Task GamesGamertagRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 3)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No Gamertag provided in URL" });
                return;
            }

            string gamertag = request.Url.Segments.Last().TrimEnd('/');
            var xboxRestAPI = getXboxRestAPI();

            var gamertagProfile = await xboxRestAPI.GetGamertagProfileAsync(gamertag);
            if (gamertagProfile == null || !gamertagProfile.ContainsKey("profileUsers"))
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Gamertag not found" });
                return;
            }

            string xuid = gamertagProfile["profileUsers"][0]["id"].ToString();
            var gamesList = await xboxRestAPI.GetGamesListAsync(xuid);

            if (gamesList == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Games list not found" });
                return;
            }

            await SendJsonResponse(response, gamesList);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private static async Task GameTitleIDSearchRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 4)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No Title ID provided in URL" });
                return;
            }

            string titleId = request.Url.Segments.Last().TrimEnd('/');

            string xuid = getXUIDOnly?.Invoke();
            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID found" });
                return;
            }

            var xboxRestAPI = getXboxRestAPI();
            var gameTitle = await xboxRestAPI.GetGameTitleAsync(xuid, titleId);

            if (gameTitle == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "No Games found" });
                return;
            }

            await SendJsonResponse(response, gameTitle);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private static async Task GameProductIDSearchRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 4)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No Product ID provided in URL" });
                return;
            }

            string productId = request.Url.Segments.Last().TrimEnd('/');

            var xboxRestAPI = getXboxRestAPI();
            var gamePassProducts = await xboxRestAPI.GetTitleIdsFromGamePass(productId);

            if (gamePassProducts == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "No Games found" });
                return;
            }

            await SendJsonResponse(response, gamePassProducts);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new { error = ex.Message, innerError = ex.InnerException?.Message });
        }
    }

    private static async Task SendJsonResponse(HttpListenerResponse response, object data)
    {
        var json = JsonConvert.SerializeObject(data);
        var buffer = Encoding.UTF8.GetBytes(json);

        response.ContentLength64 = buffer.Length;
        response.ContentType = "application/json";

        await response.OutputStream.WriteAsync(buffer);
        response.Close();
    }
}
