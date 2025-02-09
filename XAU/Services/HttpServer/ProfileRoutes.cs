using Newtonsoft.Json;
using System.Net;
using System.Text;

public static class ProfileRoutes
{
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes(Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        return new Dictionary<string, Func<HttpListenerContext, Task>>
        {
            { "/api/profile/me", async context => await ProfileMeRequest(context, getXboxRestAPI, getXUIDOnly) },
            { "/api/profile/xuid/", async context => await ProfileXuidRequest(context, getXboxRestAPI) },
            { "/api/profile/gt/", async context => await ProfileGamertagRequest(context, getXboxRestAPI) }
        };
    }

    private static async Task ProfileMeRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
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

            var profile = await xboxRestAPI.GetProfileAsync(xuid);

            if (profile == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Profile not found" });
                return;
            }

            await SendJsonResponse(response, profile);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new
            {
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    private static async Task ProfileXuidRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 4)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID provided in URL" });
                return;
            }

            string xuid = request.Url.Segments.Last().TrimEnd('/');

            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "Invalid XUID in URL" });
                return;
            }

            var xboxRestAPI = getXboxRestAPI();
            var profile = await xboxRestAPI.GetProfileAsync(xuid);

            if (profile == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Profile not found" });
                return;
            }

            await SendJsonResponse(response, profile);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new
            {
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
        }
    }

    private static async Task ProfileGamertagRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 4)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No Gamertag provided in URL" });
                return;
            }

            string gamertag = request.Url.Segments.Last().TrimEnd('/');

            var xboxRestAPI = getXboxRestAPI();

            // First, get the profile to extract XUID
            var gamertagProfile = await xboxRestAPI.GetGamertagProfileAsync(gamertag);

            if (gamertagProfile == null ||
                gamertagProfile["profileUsers"] == null ||
                !gamertagProfile["profileUsers"].Any())
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Gamertag not found" });
                return;
            }

            string xuid = gamertagProfile["profileUsers"][0]["id"].ToString();

            var fullProfile = await xboxRestAPI.GetProfileAsync(xuid);

            if (fullProfile == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Full profile not found" });
                return;
            }

            await SendJsonResponse(response, fullProfile);
        }
        catch (Exception ex)
        {
            response.StatusCode = 500;
            await SendJsonResponse(response, new
            {
                error = ex.Message,
                innerError = ex.InnerException?.Message
            });
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
