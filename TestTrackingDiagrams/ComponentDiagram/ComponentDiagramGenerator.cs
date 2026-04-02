using System.Text;
using System.Text.RegularExpressions;
using Humanizer;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.ComponentDiagram;

public static partial class ComponentDiagramGenerator
{
    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex SanitizeAliasRegex();

    public static ComponentRelationship[] ExtractRelationships(
        IEnumerable<RequestResponseLog> logs,
        Func<string, bool>? participantFilter = null)
    {
        var filtered = logs.Where(log =>
            !log.TrackingIgnore &&
            !log.IsOverrideStart &&
            !log.IsOverrideEnd &&
            !log.IsActionStart &&
            log.Type == RequestResponseType.Request);

        if (participantFilter is not null)
            filtered = filtered.Where(log =>
                participantFilter(log.CallerName) &&
                participantFilter(log.ServiceName));

        var groups = filtered.GroupBy(log => (log.CallerName, log.ServiceName, Protocol: GetProtocol(log)));

        return groups.Select(g =>
        {
            var methods = new HashSet<string>(g.Select(log => GetMethodName(log)));
            var callCount = g.Count();
            var testCount = g.Select(log => log.TestId).Distinct().Count();
            return new ComponentRelationship(g.Key.CallerName, g.Key.ServiceName, g.Key.Protocol, methods, callCount, testCount);
        }).ToArray();
    }

    public static string GeneratePlantUml(
        ComponentRelationship[] relationships,
        ComponentDiagramOptions? options = null)
    {
        options ??= new ComponentDiagramOptions();
        var sb = new StringBuilder();

        sb.AppendLine("@startuml");
        sb.AppendLine("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml");

        if (!string.IsNullOrWhiteSpace(options.PlantUmlTheme))
            sb.AppendLine($"!theme {options.PlantUmlTheme}");

        sb.AppendLine();
        sb.AppendLine($"title {options.Title}");
        sb.AppendLine();

        // Discover all unique participants
        var allCallers = new HashSet<string>(relationships.Select(r => r.Caller));
        var allServices = new HashSet<string>(relationships.Select(r => r.Service));
        var pureCallers = new HashSet<string>(allCallers.Except(allServices)); // only appear as callers

        var allParticipants = new HashSet<string>(allCallers.Union(allServices));

        foreach (var participant in allParticipants)
        {
            var alias = SanitizeAlias(participant);
            if (pureCallers.Contains(participant))
                sb.AppendLine($"Person({alias}, \"{participant}\")");
            else
                sb.AppendLine($"System({alias}, \"{participant}\")");
        }

        sb.AppendLine();

        foreach (var rel in relationships)
        {
            var callerAlias = SanitizeAlias(rel.Caller);
            var serviceAlias = SanitizeAlias(rel.Service);

            string label;
            if (options.RelationshipLabelFormatter is not null)
            {
                label = options.RelationshipLabelFormatter(rel);
            }
            else
            {
                var methodsPart = rel.Protocol == "HTTP"
                    ? $"HTTP: {string.Join(", ", rel.Methods.OrderBy(m => m))}"
                    : $"{rel.Protocol}: {string.Join(", ", rel.Methods.OrderBy(m => m))}";
                label = $"{methodsPart} - {rel.CallCount} calls across {rel.TestCount} tests";
            }

            sb.AppendLine($"Rel({callerAlias}, {serviceAlias}, \"{label}\")");
        }

        sb.AppendLine();
        sb.AppendLine("@enduml");

        return sb.ToString();
    }

    private static string GetProtocol(RequestResponseLog log) =>
        log.MetaType == RequestResponseMetaType.Event
            ? log.Method.Value?.ToString() ?? "Event"
            : "HTTP";

    private static string GetMethodName(RequestResponseLog log) =>
        log.Method.Value?.ToString() ?? "Unknown";

    private static string SanitizeAlias(string name) =>
        SanitizeAliasRegex().Replace(name.Camelize(), "_");
}
