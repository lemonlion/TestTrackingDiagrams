using System.Text;
using System.Text.RegularExpressions;
using Humanizer;

namespace TestTrackingDiagrams.ComponentDiagram;

public static partial class ComponentDiagramDiffer
{
    [GeneratedRegex(@"[^a-zA-Z0-9_]")]
    private static partial Regex SanitizeAliasRegex();

    public record DiffResult(
        ComponentRelationship[] Added,
        ComponentRelationship[] Removed,
        ComponentRelationship[] Unchanged,
        string[] NewServices,
        string[] RemovedServices,
        StatsChange[] Regressions,
        StatsChange[] Improvements);

    public record StatsChange(
        ComponentRelationship Relationship,
        RelationshipStats Baseline,
        RelationshipStats Current,
        double P95DeltaMs);

    public static DiffResult Compare(
        ComponentRelationship[] baseline,
        ComponentRelationship[] current,
        Dictionary<string, RelationshipStats>? baselineStats = null,
        Dictionary<string, RelationshipStats>? currentStats = null)
    {
        var baselineKeys = new HashSet<string>(baseline.Select(RelKey));
        var currentKeys = new HashSet<string>(current.Select(RelKey));

        var added = current.Where(r => !baselineKeys.Contains(RelKey(r))).ToArray();
        var removed = baseline.Where(r => !currentKeys.Contains(RelKey(r))).ToArray();
        var unchanged = current.Where(r => baselineKeys.Contains(RelKey(r))).ToArray();

        // Service-level diff
        var baselineServices = new HashSet<string>(baseline.SelectMany(r => new[] { r.Caller, r.Service }));
        var currentServices = new HashSet<string>(current.SelectMany(r => new[] { r.Caller, r.Service }));
        var newServices = currentServices.Except(baselineServices).ToArray();
        var removedServices = baselineServices.Except(currentServices).ToArray();

        // Stats comparison
        var regressions = new List<StatsChange>();
        var improvements = new List<StatsChange>();

        if (baselineStats != null && currentStats != null)
        {
            foreach (var rel in unchanged)
            {
                var key = $"iflow-rel-{ComponentFlowSegmentBuilder.SanitizeKey(rel.Caller)}-{ComponentFlowSegmentBuilder.SanitizeKey(rel.Service)}";
                if (!baselineStats.TryGetValue(key, out var bStats) || !currentStats.TryGetValue(key, out var cStats))
                    continue;

                var delta = cStats.P95Ms - bStats.P95Ms;
                // Regression if P95 increased by >20%
                if (bStats.P95Ms > 0 && delta / bStats.P95Ms > 0.2)
                    regressions.Add(new StatsChange(rel, bStats, cStats, delta));
                // Improvement if P95 decreased by >20%
                else if (bStats.P95Ms > 0 && delta / bStats.P95Ms < -0.2)
                    improvements.Add(new StatsChange(rel, bStats, cStats, delta));
            }
        }

        return new DiffResult(added, removed, unchanged, newServices, removedServices,
            regressions.ToArray(), improvements.ToArray());
    }

    public static string GenerateDiffPlantUml(DiffResult diff, string title = "Component Diagram Diff")
    {
        var sb = new StringBuilder();
        sb.AppendLine("@startuml");
        sb.AppendLine("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml");
        sb.AppendLine();
        sb.AppendLine($"title {title}");
        sb.AppendLine();

        // Collect all participants
        var allRels = diff.Added.Concat(diff.Removed).Concat(diff.Unchanged).ToArray();
        var allCallers = new HashSet<string>(allRels.Select(r => r.Caller));
        var allServices = new HashSet<string>(allRels.Select(r => r.Service));
        var pureCallers = new HashSet<string>(allCallers.Except(allServices));
        var allParticipants = new HashSet<string>(allCallers.Union(allServices));
        var newServiceSet = new HashSet<string>(diff.NewServices);
        var removedServiceSet = new HashSet<string>(diff.RemovedServices);

        foreach (var participant in allParticipants)
        {
            var alias = SanitizeAlias(participant);
            var kind = pureCallers.Contains(participant) ? "Person" : "System";

            if (newServiceSet.Contains(participant))
                sb.AppendLine($"{kind}({alias}, \"{participant}\", $tags=\"#LimeGreen\")");
            else if (removedServiceSet.Contains(participant))
                sb.AppendLine($"{kind}({alias}, \"{participant}\", $tags=\"#Red\")");
            else
                sb.AppendLine($"{kind}({alias}, \"{participant}\")");
        }

        sb.AppendLine();

        // Unchanged
        foreach (var rel in diff.Unchanged)
        {
            var callerAlias = SanitizeAlias(rel.Caller);
            var serviceAlias = SanitizeAlias(rel.Service);
            sb.AppendLine($"Rel({callerAlias}, {serviceAlias}, \"unchanged\")");
        }

        // Added (green)
        foreach (var rel in diff.Added)
        {
            var callerAlias = SanitizeAlias(rel.Caller);
            var serviceAlias = SanitizeAlias(rel.Service);
            sb.AppendLine($"Rel({callerAlias}, {serviceAlias}, \"NEW\", $tags=\"#LimeGreen\")");
        }

        // Removed (red, dashed)
        foreach (var rel in diff.Removed)
        {
            var callerAlias = SanitizeAlias(rel.Caller);
            var serviceAlias = SanitizeAlias(rel.Service);
            sb.AppendLine($"{callerAlias} ..> {serviceAlias} : \"REMOVED\" #Red");
        }

        sb.AppendLine();
        sb.AppendLine("@enduml");

        return sb.ToString();
    }

    private static string RelKey(ComponentRelationship r) => $"{r.Caller}->{r.Service}";

    private static string SanitizeAlias(string name) =>
        SanitizeAliasRegex().Replace(name.Camelize(), "_");
}
