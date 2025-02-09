using Newtonsoft.Json;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;

public static class AchievementRoutes
{
    private static readonly Dictionary<string, string> ServiceConfigCache = new();
    public static Dictionary<string, Func<HttpListenerContext, Task>> GetRoutes(Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        return new Dictionary<string, Func<HttpListenerContext, Task>>
        {
            { "/api/achievements/unlockall", async context => await UnlockAllAchievementsRequest(context, getXboxRestAPI, getXUIDOnly) },
            { "/api/achievements/unlock", async context => await UnlockAchievementRequest(context, getXboxRestAPI, getXUIDOnly) },
            { "/api/achievements/", async context => await AchievementsTitleRequest(context, getXboxRestAPI, getXUIDOnly) }

        };
    }

    private static async Task AchievementsTitleRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.Url.Segments.Length < 3)
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

            var achievements = await xboxRestAPI.GetAchievementsForTitleAsync(xuid, titleId);

            if (achievements == null || achievements.achievements == null || !achievements.achievements.Any())
            {
                var xbox360Achievements = await xboxRestAPI.GetAchievementsFor360TitleAsync(xuid, titleId);

                if (xbox360Achievements != null)
                {
                    await SendJsonResponse(response, xbox360Achievements);
                    return;
                }
            }

            if (achievements == null)
            {
                response.StatusCode = 404;
                await SendJsonResponse(response, new { error = "Achievements not found" });
                return;
            }

            await SendJsonResponse(response, achievements);
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
    private static async Task UnlockAchievementRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                await SendJsonResponse(response, new { error = "Method not allowed. Use POST." });
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync();
            var unlockRequest = JsonConvert.DeserializeObject<UnlockAchievementRequest>(body);

            if (unlockRequest == null || string.IsNullOrWhiteSpace(unlockRequest.TitleId) || string.IsNullOrWhiteSpace(unlockRequest.AchievementId))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "Invalid request. Provide TitleId and AchievementId in your body." });
                return;
            }

            string xuid = getXUIDOnly?.Invoke();
            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID found" });
                return;
            }

            string serviceConfigId = await GetServiceConfigId(getXboxRestAPI, getXUIDOnly, unlockRequest.TitleId);

            var xboxRestAPI = getXboxRestAPI();
            await xboxRestAPI.UnlockTitleBasedAchievementAsync(
                serviceConfigId,
                unlockRequest.TitleId,
                xuid,
                unlockRequest.AchievementId
            );

            await SendJsonResponse(response, new
            {
                message = "Achievement unlocked successfully",
                achievementId = unlockRequest.AchievementId,
                titleId = unlockRequest.TitleId
            });
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

    private static async Task UnlockAllAchievementsRequest(HttpListenerContext context, Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly)
    {
        var request = context.Request;
        var response = context.Response;

        try
        {
            if (request.HttpMethod != "POST")
            {
                response.StatusCode = 405;
                await SendJsonResponse(response, new { error = "Method not allowed. Use POST." });
                return;
            }

            using var reader = new StreamReader(request.InputStream);
            var body = await reader.ReadToEndAsync();
            var unlockRequest = JsonConvert.DeserializeObject<UnlockAllAchievementsRequest>(body);

            if (unlockRequest == null || string.IsNullOrWhiteSpace(unlockRequest.TitleId))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "Invalid request. Provide TitleId in your body." });
                return;
            }

            string serviceConfigId = await GetServiceConfigId(getXboxRestAPI, getXUIDOnly, unlockRequest.TitleId);

            string xuid = getXUIDOnly?.Invoke();
            if (string.IsNullOrWhiteSpace(xuid))
            {
                response.StatusCode = 400;
                await SendJsonResponse(response, new { error = "No XUID found" });
                return;
            }

            var xboxRestAPI = getXboxRestAPI();
            var achievements = await xboxRestAPI.GetAchievementsForTitleAsync(xuid, unlockRequest.TitleId);

            var achievementIds = achievements.achievements.Select(a => a.id).ToList();
            await xboxRestAPI.UnlockTitleBasedAchievementsAsync(
                serviceConfigId,
                unlockRequest.TitleId,
                xuid,
                achievementIds
            );

            await SendJsonResponse(response, new
            {
                message = "All achievements unlocked successfully",
                titleId = unlockRequest.TitleId,
                totalAchievements = achievementIds.Count
            });
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

    private static async Task<string> GetServiceConfigId(Func<XboxRestAPI> getXboxRestAPI, Func<string> getXUIDOnly, string titleId)
    {
        if (ServiceConfigCache.TryGetValue(titleId, out var cachedServiceConfigId))
        {
            return cachedServiceConfigId;
        }

        var xuid = getXUIDOnly?.Invoke();
        if (string.IsNullOrWhiteSpace(xuid)) throw new Exception("No XUID found.");

        var xboxRestAPI = getXboxRestAPI();
        var achievements = await xboxRestAPI.GetAchievementsForTitleAsync(xuid, titleId);

        if (achievements == null || achievements.achievements == null || !achievements.achievements.Any())
        {
            throw new Exception("Achievements not found for the title.");
        }

        var serviceConfigId = achievements.achievements.FirstOrDefault()?.serviceConfigId;
        if (string.IsNullOrWhiteSpace(serviceConfigId))
        {
            throw new Exception("ServiceConfigId not found for the title.");
        }

        ServiceConfigCache[titleId] = serviceConfigId;

        return serviceConfigId;
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

public class UnlockAchievementRequest
{
    public string TitleId { get; set; }
    public string AchievementId { get; set; }
}

public class UnlockAllAchievementsRequest
{
    public string TitleId { get; set; }
}
