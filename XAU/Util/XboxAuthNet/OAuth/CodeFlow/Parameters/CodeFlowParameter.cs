using System.Text.Json.Serialization;

namespace XboxAuthNet.OAuth.CodeFlow.Parameters;

public class CodeFlowParameter
{
    public string? Tenant { get; set; }

    [JsonPropertyName("client_id")]
    public string? ClientId { get; set; }

    [JsonPropertyName("scope")]
    public string? Scope { get; set; }

    public Dictionary<string, string> ExtraQueries { get; } = new();

    public Dictionary<string, string?> ToQueryDictionary()
    {
        var query = new Dictionary<string, string?>();
        var props = GetType().GetProperties();
        foreach (var prop in props)
        {
            var value = prop.GetMethod?.Invoke(this, null)?.ToString();
            if (string.IsNullOrEmpty(value))
                continue;

            var attr = prop
                .GetCustomAttributes(typeof(JsonPropertyNameAttribute), true)
                .FirstOrDefault();
            if (attr is not JsonPropertyNameAttribute jsonAttr)
                continue;

            var propName = jsonAttr.Name;
            query[propName] = value;
        }

        foreach (var kv in ExtraQueries)
        {
            query[kv.Key] = kv.Value;
        }
        return query;
    }
}
