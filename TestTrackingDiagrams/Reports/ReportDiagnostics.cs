using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Reports;

public static class ReportDiagnostics
{
    public static string[] Analyse(RequestResponseLog[] logs, Feature[] features)
    {
        if (logs.Length == 0 && features.Length == 0)
            return [];

        var warnings = new List<string>();

        var distinctTestIds = logs.Select(l => l.TestId).Distinct().ToArray();

        warnings.Add($"Report diagnostics: {logs.Length} log entries across {distinctTestIds.Length} test(s).");

        var unpairedCount = CountUnpairedRequests(logs);
        if (unpairedCount > 0)
            warnings.Add($"Warning: {unpairedCount} unpaired request(s) detected (no matching response with same RequestResponseId).");

        if (features.Length > 0)
        {
            var scenarioIds = features
                .SelectMany(f => f.Scenarios)
                .Select(s => s.Id)
                .ToHashSet();

            var orphanedTestIds = distinctTestIds
                .Where(id => !scenarioIds.Contains(id))
                .ToArray();

            if (orphanedTestIds.Length > 0)
                warnings.Add($"Warning: {orphanedTestIds.Length} orphaned test ID(s) in logs do not match any feature scenario.");
        }

        return warnings.ToArray();
    }

    private static int CountUnpairedRequests(RequestResponseLog[] logs)
    {
        var requests = logs.Where(l => l.Type == RequestResponseType.Request).ToArray();
        var responseIds = logs
            .Where(l => l.Type == RequestResponseType.Response)
            .Select(l => l.RequestResponseId)
            .ToHashSet();

        return requests.Count(r => !responseIds.Contains(r.RequestResponseId));
    }
}
