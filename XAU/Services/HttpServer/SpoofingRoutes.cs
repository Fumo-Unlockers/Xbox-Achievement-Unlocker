using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Text;

public static class SpoofingRoutes
{
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes(Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        return new Dictionary<string, Func<HttpListenerContext, Task>>
        {
            { "/api/spoof", async context => await StartSpoofingRequest(context, getXboxRestAPI, getXUIDOnly) },
        };
    }

    private static async Task StartSpoofingRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod != "GET")
            {
                response.StatusCode = 405;
                await SendJsonResponse(response, new { error = "Method not allowed. Use GET." });
                return;
            }

            if (request.Url.Segments.Length < 3)
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No Title ID provided in URL." });
                return;
            }

            string titleId = request.Url.Segments.Last().TrimEnd('/');
            if (string.IsNullOrWhiteSpace(titleId))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "Invalid Title ID." });
                return;
            }

            string xuid = getXUIDOnly?.Invoke();
            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID found." });
                return;
            }

            var xboxRestAPI = getXboxRestAPI();
            await xboxRestAPI.SendHeartbeatAsync(xuid, titleId);

            response.StatusCode = 200;
            await SendJsonResponse(response, new { message = "Spoofing started successfully.", titleId });
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

public class SpoofingRequest
{
    public string TitleId { get; set; }
}
