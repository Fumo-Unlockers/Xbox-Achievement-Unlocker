using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Web;
using System.Text.Json;

namespace XboxAuthNet
{
    public class HttpHelper
    {
        public const string UserAgent = "Mozilla/5.0 (XboxReplay; XboxLiveAuth/3.0) " +
                                        "AppleWebKit/537.36 (KHTML, like Gecko) " +
                                        "Chrome/71.0.3578.98 Safari/537.36";

        public static string GetQueryString(Dictionary<string, string?> queries)
        {
            return string.Join("&",
                queries.Select(x => $"{x.Key}={HttpUtility.UrlEncode(x.Value)}"));
        }

        public static JsonContent CreateJsonContent<T>(T obj)
        {
            return JsonContent.Create(obj,
                mediaType: new System.Net.Http.Headers.MediaTypeHeaderValue("application/json"),
                options: new JsonSerializerOptions
                {
                    DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
                    PropertyNamingPolicy = null
                });
        }
    }
}
