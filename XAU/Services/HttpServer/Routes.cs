using Newtonsoft.Json;
using System.Net;
using System.Text;

public static class Routes
{
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes(Func<string> getXauthToken, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var routes = new Dictionary<string, Func<HttpListenerContext, Task>>
        {
            { "/api/xauth", async context => await ApiXauthRequest(context, getXauthToken) }
        };

        var profileRoutes = ProfileRoutes.GetRoutes(getXboxRestAPI, getXUIDOnly);
        foreach (var route in profileRoutes)
        {
            routes[route.Key] = route.Value;
        }

        var gameRoutes = GameRoutes.GetRoutes(getXboxRestAPI, getXUIDOnly);
        foreach (var route in gameRoutes)
        {
            routes[route.Key] = route.Value;
        }

        var achievementRoutes = AchievementRoutes.GetRoutes(getXboxRestAPI, getXUIDOnly);
        foreach (var route in achievementRoutes)
        {
            routes[route.Key] = route.Value;
        }

        var spoofingRoutes = SpoofingRoutes.GetRoutes(getXboxRestAPI, getXUIDOnly);
        foreach (var route in spoofingRoutes)
        {
            routes[route.Key] = route.Value;
        }

        var endpointRoutes = EndpointRoutes.GetRoutes();
        foreach (var route in endpointRoutes)
        {
            routes[route.Key] = route.Value;
        }

        return routes;
    }

    private static async Task ApiXauthRequest(HttpListenerContext context, Func<string> getXauthToken)
    {
        var response = context.Response;
        string query = context.Request.Url?.Query ?? string.Empty;

        bool isJson = query.Contains("?format=json", StringComparison.OrdinalIgnoreCase);

        var xauth = getXauthToken();

        if (isJson)
        {
            var jsonResponse = new { token = xauth };
            var json = JsonConvert.SerializeObject(jsonResponse);
            var buffer = Encoding.UTF8.GetBytes(json);

            response.ContentLength64 = buffer.Length;
            response.ContentType = "application/json";

            await response.OutputStream.WriteAsync(buffer);
        }
        else
        {
            var buffer = Encoding.UTF8.GetBytes(xauth);

            response.ContentLength64 = buffer.Length;
            response.ContentType = "text/plain";

            await response.OutputStream.WriteAsync(buffer);
        }

        response.Close();
    }
}
