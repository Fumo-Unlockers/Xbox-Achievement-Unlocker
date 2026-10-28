using System.Globalization;
using System.Text.RegularExpressions;
using Newtonsoft.Json.Linq;
using XAU.Services;

var time = new DateTime(2026, 10, 28, 12, 0, 0, DateTimeKind.Utc);
string template = "{\"ver\":\"4.0\",\"name\":\"test\",\"ext\":{},\"data\":{},\"value\":VALUE}";
JObject Replace(string value) => JObject.Parse("{\"operation\":{\"ReplacementType\":\"Replace\",\"Target\":\"VALUE\",\"Replacement\":" + value + "}}");
void Require(bool condition, string name) { if (!condition) throw new Exception(name); }
void Reject(JObject mapping, string name)
{
    try { EventPayloadRenderer.Render(template, mapping, "1", time); }
    catch { return; }
    throw new Exception(name);
}

var direct = EventPayloadRenderer.Render(template, Replace("17"), "1", time);
Require(JObject.Parse(direct[0]).Value<int>("value") == 17, "Legacy numeric replacement");
var wrapped = new JObject { ["Request1"] = Replace("1"), ["Request2"] = Replace("2") };
var sequence = EventPayloadRenderer.Render(template, wrapped, "1", time);
Require(sequence.Count == 2 && JObject.Parse(sequence[0]).Value<int>("value") == 1 &&
    JObject.Parse(sequence[1]).Value<int>("value") == 2, "Ordered requests use independent templates");
var sessions = EventPayloadRenderer.Render(template.Replace("VALUE", "VALUE,\"session\":\"REPLACESESSIONGUID\""), wrapped, "1", time);
string session = JObject.Parse(sessions[0]).Value<string>("session")!;
Require(Guid.TryParse(session, out _) && JObject.Parse(sessions[1]).Value<string>("session") == session,
    "All requests in an achievement use the same generated session GUID");
Reject(new JObject(), "Empty achievement must fail");
Reject(new JObject { ["Request2"] = Replace("1") }, "Missing Request1 must fail");
Reject(new JObject { ["Request1"] = Replace("1"), ["Name"] = "research" }, "Mixed research fields must fail");
Reject(new JObject { ["Request2"] = Replace("2"), ["Request1"] = Replace("1") }, "Unordered requests must fail");
Reject(JObject.Parse("{\"op\":{\"ReplacementType\":\"Unknown\",\"Target\":\"VALUE\"}}"), "Unknown operations must fail");
var malformed = new JObject { ["Request1"] = Replace("1"), ["Request2"] = Replace("\"invalid-json\"") };
Reject(malformed, "Invalid later request must prevent a rendered sequence from being returned");
CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
var range = JObject.Parse("{\"op\":{\"ReplacementType\":\"RangeFloat\",\"Target\":\"VALUE\",\"Min\":1.25,\"Max\":1.25}}");
Require(JObject.Parse(EventPayloadRenderer.Render(template, range, "1", time)[0]).Value<double>("value") == 1.25,
    "RangeFloat must render valid JSON in decimal-comma locales");

if (args.Length > 0)
{
    string directory = Path.GetFullPath(args[0]);
    var data = EventCatalog.Load(directory);
    int achievements = 0, requests = 0;
    foreach (var id in (JArray)data["SupportedTitleIDs"]!)
    {
        string tid = id.ToString();
        var mappings = (JObject)data[tid]!["Achievements"]!;
        Require(mappings.HasValues, $"Listed title {tid} has no achievement mappings");
        string catalogTemplate = File.ReadAllText(Path.Combine(directory, tid + ".json"));
        foreach (var mapping in mappings.Properties())
        {
            try
            {
                var bodies = EventPayloadRenderer.Render(catalogTemplate, (JObject)mapping.Value, "281474976710656", time);
                foreach (string body in bodies)
                    Require(!Regex.IsMatch(body, @"REPLAC[A-Z0-9_]+"), "Unresolved runtime placeholder");
                requests += bodies.Count;
            }
            catch (Exception ex) { throw new Exception($"Title {tid}, achievement {mapping.Name}: {ex.Message}", ex); }
            achievements++;
        }
    }
    Console.WriteLine($"Catalog passed: {achievements} achievements, {requests} requests. No network calls.");
}
Console.WriteLine("Renderer regression checks passed.");
