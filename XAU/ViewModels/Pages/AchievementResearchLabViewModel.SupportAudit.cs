using System.Collections.ObjectModel;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace XAU.ViewModels.Pages;

public partial class AchievementResearchLabViewModel
{
    [ObservableProperty] private string _supportAuditSummary = "Select a title to audit its support.";
    [ObservableProperty] private string _supportAuditSearch = "";
    [ObservableProperty] private string _supportAuditFilter = "All";
    [ObservableProperty] private ObservableCollection<ResearchSupportAuditRow> _supportAuditRows = new();
    [ObservableProperty] private ResearchSupportAuditRow? _selectedSupportAuditRow;
    private List<ResearchSupportAuditRow> _allSupportAuditRows = [];

    public IReadOnlyList<string> SupportAuditFilters { get; } =
        ["All", "Unlockable", "Unsupported", "Missing mapping", "Mapping/parser problem"];

    partial void OnSupportAuditSearchChanged(string value) => FilterSupportAudit();
    partial void OnSupportAuditFilterChanged(string value) => FilterSupportAudit();

    private void FilterSupportAudit()
    {
        var selectedId = SelectedSupportAuditRow?.Id;
        SupportAuditRows = new ObservableCollection<ResearchSupportAuditRow>(_allSupportAuditRows.Where(row =>
            (SupportAuditFilter switch
            {
                "Unlockable" => row.CurrentlyUnlockable == true,
                "Unsupported" => !row.Supported,
                "Missing mapping" => row.Category == "Missing mapping",
                "Mapping/parser problem" => row.MappingPresent && !row.FullyMapped,
                _ => true
            }) && (string.IsNullOrWhiteSpace(SupportAuditSearch)
                || row.Details.Contains(SupportAuditSearch, StringComparison.OrdinalIgnoreCase))));
        SelectedSupportAuditRow = SupportAuditRows.FirstOrDefault(row => row.Id == selectedId)
            ?? SupportAuditRows.FirstOrDefault();
    }

    [RelayCommand]
    private void CopySupportAudit()
    {
        System.Windows.Clipboard.SetText(SupportAuditSummary + Environment.NewLine + Environment.NewLine
            + string.Join(Environment.NewLine + Environment.NewLine, SupportAuditRows.Select(row => row.Details)));
    }

    private void BuildSupportAudit(AchievementsResponse? response)
    {
        _allSupportAuditRows = [];
        var title = SelectedResearchTitle;
        if (title == null)
        {
            SupportAuditSummary = "Select a title to audit its support.";
            FilterSupportAudit();
            return;
        }

        var mappings = LoadSelectedTitleMappings();
        var templateText = TryReadTemplate(title.TemplatePath);
        var live = response != null;
        var achievements = response?.achievements ?? [];
        // Mirror the normal page's title-wide first-requirement test; multiple requirements alone are not a rejection.
        var eventBased = !live || achievements.Any(achievement =>
            achievement?.progression?.requirements?.FirstOrDefault() is { } requirement
            && requirement.id != StringConstants.ZeroUid);
        var registered = TryGetArray(_eventData, "SupportedTitleIDs", out var supportedIds)
            && supportedIds!.Any(id => TryGetString(id, out var value) && value == title.TitleId);
        TryGetObject(_eventData, title.TitleId, out var eventTitle);
        TryGetString(eventTitle, "FullySupported", out var fullySupportedFlag);
        var sourceRows = live ? achievements : (mappings?.Properties().Select(property =>
            new OneCoreAchievementResponse { id = property.Name, name = "<Xbox name unavailable>", progressState = "<unknown>" }).ToList() ?? []);

        foreach (var achievement in sourceRows)
        {
            var row = new ResearchSupportAuditRow
            {
                Id = achievement?.id ?? "<missing ID>",
                Name = achievement?.name ?? "<missing name>",
                State = live ? achievement?.progressState ?? "<missing state>" : "<unknown>",
                TemplateAvailable = templateText != null
            };
            var detail = new StringBuilder()
                .AppendLine($"Title: {title.Name} ({title.TitleId})")
                .AppendLine($"Achievement: {row.Id} - {row.Name}")
                .AppendLine($"State: {row.State}")
                .AppendLine($"Template: {title.TemplatePath}")
                .AppendLine($"Template available: {row.TemplateAvailable}")
                .AppendLine($"Listed in SupportedTitleIDs: {registered}; FullySupported flag: {fullySupportedFlag ?? "<missing>"}");
            try
            {
                if (achievement == null || string.IsNullOrWhiteSpace(achievement.id))
                {
                    row.Reason = "Missing achievement record or API ID; cannot correlate a local mapping or normal Unlock eligibility.";
                    row.Details = detail.AppendLine($"Category: {row.Category}").AppendLine($"Reason: {row.Reason}").ToString();
                    _allSupportAuditRows.Add(row);
                    continue;
                }
                var requirements = achievement?.progression?.requirements ?? [];
                row.RequirementIds = string.Join("; ", requirements.Select(r => r?.id ?? "<missing>"));
                row.Current = string.Join("; ", requirements.Select(r => r?.current ?? "<empty>"));
                row.Target = string.Join("; ", requirements.Select(r => r?.target ?? "<empty>"));
                row.OperationTypes = string.Join("; ", requirements.Select(r => r?.operationType ?? "<not returned>"));
                row.ValueTypes = string.Join("; ", requirements.Select(r => r?.valueType ?? "<not returned>"));
                row.RuleParticipationTypes = string.Join("; ", requirements.Select(r => r?.ruleParticipationType ?? "<not returned>"));
                detail.AppendLine($"Requirements: {(live ? requirements.Count.ToString() : "<Xbox response unavailable>")}");
                foreach (var requirement in requirements)
                    detail.AppendLine($"  ID={requirement?.id ?? "<missing>"}; current={requirement?.current ?? "<empty>"}; target={requirement?.target ?? "<empty>"}; "
                        + $"operationType={requirement?.operationType ?? "<not returned>"}; valueType={requirement?.valueType ?? "<not returned>"}; "
                        + $"ruleParticipationType={requirement?.ruleParticipationType ?? "<not returned>"}");
                if (requirements.Count > 1)
                    detail.AppendLine("Multiple requirements: present; XAU does not reject a mapping on this basis alone.");

                var numericId = int.TryParse(row.Id, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsedId);
                var normalKey = numericId ? parsedId.ToString(CultureInfo.InvariantCulture) : row.Id;
                var normalMappingPresent = mappings?.ContainsKey(normalKey) == true;
                var exactMappingPresent = mappings?.ContainsKey(row.Id) == true;
                var mappingToken = exactMappingPresent ? mappings![row.Id] : normalMappingPresent ? mappings![normalKey] : null;
                row.MappingPresent = mappingToken != null;
                row.CurrentlyUnlockable = !live ? null : numericId && row.State != StringConstants.Achieved
                    && (!eventBased || registered && normalMappingPresent);
                detail.AppendLine($"Mapping present: {row.MappingPresent}; exact API ID match: {exactMappingPresent}; normal numeric key: {normalKey}");
                detail.AppendLine($"Normal Unlock eligibility: {row.UnlockableDisplay} (initial-load row flag; not a server validation or events-token check)");
                if (!live)
                    detail.AppendLine("Current state and normal Unlock eligibility require a live Xbox response.");
                else if (!numericId)
                    detail.AppendLine("Normal page parses achievement IDs as Int32; this ID cannot be correlated through that path.");
                else if (row.State == StringConstants.Achieved)
                    detail.AppendLine("Normal Unlock is disabled because this achievement is already Achieved, not because its mapping is unsupported.");
                else if (eventBased && !registered)
                    detail.AppendLine("Normal Unlock is disabled because the title is absent from SupportedTitleIDs.");
                else if (eventBased && !normalMappingPresent)
                    detail.AppendLine("Normal Unlock is disabled because no mapping exists at the ID used by the normal page.");

                if (!eventBased)
                {
                    row.Category = "Title-based path";
                    row.Supported = numericId;
                    row.Reason = "The normal page routes this title through title-based requests; no event mapping is required.";
                }
                else if (!row.MappingPresent)
                {
                    row.Category = "Missing mapping";
                    row.Reason = "No mapping exists in Data.json for this API achievement ID.";
                }
                else if (mappingToken is not JObject mapping || !mapping.HasValues)
                {
                    row.Category = "Unsupported mapping format";
                    row.Reason = "Mapping exists but is not a non-empty replacement object.";
                }
                else
                {
                    AuditReplacementRules(row, mapping, templateText, detail);
                    row.Supported = row.FullyMapped && registered && numericId && normalMappingPresent;
                    if (row.FullyMapped && !row.Supported)
                        row.Reason += " Existing mapping data is blocked by the normal title-registration or numeric-ID eligibility rule.";
                }
            }
            catch (Exception ex)
            {
                row.Category = "Other failures";
                row.Reason = $"Audit failed for this achievement ({ex.GetType().Name}); remaining rows were retained.";
            }
            detail.AppendLine($"Category: {row.Category}").AppendLine($"Reason: {row.Reason}");
            if (row.MappingPresent && !row.Supported)
                detail.AppendLine("MAPPING EXISTS: this is distinct from missing research data.");
            if (row.CurrentlyUnlockable == true && !row.Supported)
                detail.AppendLine("Normal UI enables this row by mapping-key presence even though the audit found a mapping/template problem.");
            row.Details = detail.ToString();
            _allSupportAuditRows.Add(row);
        }

        var apiIds = achievements.Where(a => a != null).Select(a => a.id).ToHashSet(StringComparer.Ordinal);
        var unmatched = live ? mappings?.Properties().Where(p => !apiIds.Contains(p.Name)).Select(p => p.Name).ToList() ?? [] : [];
        var rows = _allSupportAuditRows;
        SupportAuditSummary = $"Title {title.TitleId}; source: "
            + (live ? $"Xbox achievements response ({rows.Count} rows)." : $"local Data.json only ({rows.Count} mappings); Xbox totals/states/eligibility unknown.")
            + Environment.NewLine
            + $"Total achievements: {(live ? rows.Count.ToString() : "unknown")}; Fully mapped: {rows.Count(r => r.FullyMapped)}; "
            + $"Currently unlockable: {(live ? rows.Count(r => r.CurrentlyUnlockable == true).ToString() : "unknown")}; "
            + $"Partially mapped: {rows.Count(r => r.Category == "Partially mapped")}; Missing mappings: {(live ? rows.Count(r => r.Category == "Missing mapping").ToString() : "unknown")}; "
            + $"Unsupported mapping format: {rows.Count(r => r.Category == "Unsupported mapping format")}; "
            + $"Missing/broken template: {rows.Count(r => r.Category == "Missing/broken template")}; Other failures: {rows.Count(r => r.Category == "Other failures")}."
            + Environment.NewLine
            + $"Statically supported: {rows.Count(r => r.Supported)}; Unsupported: {(live ? rows.Count(r => !r.Supported).ToString() : "unknown across Xbox achievements")}; "
            + $"Already Achieved: {(live ? rows.Count(r => r.State == StringConstants.Achieved).ToString() : "unknown")}. "
            + "Fully mapped means structurally reconstructable, not a proven unlock. Range previews use Min; timestamps use a dummy value."
            + Environment.NewLine
            + $"FullySupported={fullySupportedFlag ?? "<missing>"}; the normal partial-support warning requires false, independently of these counts. "
            + $"Template available: {templateText != null}; listed in SupportedTitleIDs: {registered}. "
            + $"Existing mappings with audit/eligibility problems: {rows.Count(r => r.MappingPresent && !r.Supported)}."
            + (unmatched.Count == 0 ? "" : Environment.NewLine + $"Local IDs absent from the response ({unmatched.Count}): {string.Join(", ", unmatched)}. Correlation unresolved; check response completeness.");
        FilterSupportAudit();
    }

    private static void AuditReplacementRules(ResearchSupportAuditRow row, JObject mapping, string? template, StringBuilder details)
    {
        var types = new List<string>();
        var targets = new List<string>();
        var problems = new List<string>();
        var body = template ?? "";
        var targetMismatch = false;
        foreach (var entry in mapping.Properties())
        {
            if (entry.Value is not JObject rule)
            {
                problems.Add($"Entry {entry.Name}: expected a replacement object, found {entry.Value.Type}.");
                continue;
            }
            TryGetString(rule, "ReplacementType", out var type);
            TryGetString(rule, "Target", out var target);
            TryGetString(rule, "Replacement", out var value);
            TryGetString(rule, "Min", out var min);
            TryGetString(rule, "Max", out var max);
            types.Add(type ?? "<missing>");
            targets.Add(target ?? "<missing>");
            var sensitiveTarget = Regex.IsMatch(target ?? "", "XUID|AUTH|TOKEN|DEVICE|SESSION|USERID|IKEY", RegexOptions.IgnoreCase);
            details.AppendLine($"  {entry.Name}: type={type ?? "<missing>"}; target={target ?? "<missing>"}; value={(sensitiveTarget ? "[REDACTED]" : AuditSafeReplacement(value))}; min={(sensitiveTarget ? "[REDACTED]" : min ?? "<none>")}; max={(sensitiveTarget ? "[REDACTED]" : max ?? "<none>")}");
            if (string.IsNullOrWhiteSpace(target))
            {
                problems.Add($"Entry {entry.Name}: missing target.");
                continue;
            }
            if (template != null && !body.Contains(target, StringComparison.Ordinal))
            {
                targetMismatch = true;
                details.AppendLine($"  Target {target} is not present when this rule runs (replacement order or mapping/template mismatch).");
            }
            switch (type)
            {
                case "Replace":
                    if (rule["Replacement"] is not JValue { Value: not null })
                        problems.Add($"Entry {entry.Name}: missing/non-scalar Replacement; normal path requires a scalar or JSON text string.");
                    break;
                case "RangeInt":
                    if (!TryGetInt(rule, "Min", out var lowInt) || !TryGetInt(rule, "Max", out var highInt) || lowInt > highInt)
                        problems.Add($"Entry {entry.Name}: invalid Int32 bounds or Min > Max.");
                    value = min;
                    break;
                case "RangeFloat":
                    if (!float.TryParse(min, NumberStyles.Float, CultureInfo.InvariantCulture, out var lowFloat)
                        || !float.TryParse(max, NumberStyles.Float, CultureInfo.InvariantCulture, out var highFloat)
                        || !float.IsFinite(lowFloat) || !float.IsFinite(highFloat) || lowFloat > highFloat)
                        problems.Add($"Entry {entry.Name}: invalid finite float bounds or Min > Max.");
                    value = min;
                    break;
                case "StupidFuckingLDAPTimestamp":
                    value = "0";
                    break;
                default:
                    problems.Add($"Entry {entry.Name}: replacement type {type ?? "<missing>"} is not handled by XAU's normal event switch.");
                    break;
            }
            if (value != null)
                body = body.Replace(target, value, StringComparison.Ordinal);
        }
        row.ReplacementTypes = string.Join(", ", types.Distinct());
        row.ReplacementTargets = string.Join(", ", targets.Distinct());
        if (problems.Count > 0)
        {
            row.Category = "Unsupported mapping format";
            row.Reason = string.Join(" ", problems);
        }
        else if (template == null)
        {
            row.Category = "Missing/broken template";
            row.Reason = "Mapping exists but the title template is missing or unreadable.";
        }
        else if (!TryReconstructMappedPayload(template, mapping, out var payload, out var error))
        {
            row.Category = error?.StartsWith("unresolved placeholders", StringComparison.Ordinal) == true
                ? "Partially mapped" : "Missing/broken template";
            // Parser messages can contain raw payload fragments; retain precise location without copying raw content.
            var position = Regex.Match(error ?? "", @"line \d+, position \d+").Value;
            row.Reason = row.Category == "Partially mapped" ? error! : $"Invalid JSON after mapped replacements. {position}";
        }
        else if (targetMismatch || payload is not JObject)
        {
            row.Category = "Partially mapped";
            row.Reason = targetMismatch ? "Mapping/template target mismatch; see per-rule diagnostics." : "Reconstructed payload is not a JSON object.";
        }
        else
        {
            row.FullyMapped = true;
            row.Category = "Fully mapped";
            row.Reason = "All ordered replacement rules reconstruct a JSON object using types handled by the normal event path. Server acceptance and achievement semantics are untested.";
        }
    }

    private static string AuditSafeReplacement(string? value)
    {
        if (value == null)
            return "<none>";
        try
        {
            var normalized = Regex.Replace(value, @"REPLACE[A-Z0-9_]+", "0");
            var parsed = JToken.Parse(normalized);
            RedactPayload(parsed);
            return parsed.ToString(Formatting.None);
        }
        catch (JsonException)
        {
            return value.StartsWith("Microsoft.XboxLive.", StringComparison.Ordinal)
                && Regex.IsMatch(value, @"^Microsoft\.XboxLive\.[A-Za-z0-9_.]+$") ? value : "<non-JSON text withheld>";
        }
    }
}

public sealed class ResearchSupportAuditRow
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string State { get; set; } = "";
    public string RequirementIds { get; set; } = "";
    public string Current { get; set; } = "";
    public string Target { get; set; } = "";
    public string OperationTypes { get; set; } = "";
    public string ValueTypes { get; set; } = "";
    public string RuleParticipationTypes { get; set; } = "";
    public bool MappingPresent { get; set; }
    public bool TemplateAvailable { get; set; }
    public string ReplacementTypes { get; set; } = "";
    public string ReplacementTargets { get; set; } = "";
    public bool? CurrentlyUnlockable { get; set; }
    public string UnlockableDisplay => CurrentlyUnlockable.HasValue ? (CurrentlyUnlockable.Value ? "Yes" : "No") : "Unknown";
    public bool FullyMapped { get; set; }
    public bool Supported { get; set; }
    public string Category { get; set; } = "Other failures";
    public string Reason { get; set; } = "";
    public string Details { get; set; } = "";
}
