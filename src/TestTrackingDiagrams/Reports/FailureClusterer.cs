using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Groups failed scenarios by normalised error message to surface common failure patterns.
/// </summary>
public static class FailureClusterer
{
    /// <summary>
    /// A group of scenarios sharing the same normalised error message.
    /// </summary>
    public record FailureCluster(string ClusterKey, Scenario[] Scenarios);

    public static FailureCluster[] Cluster(Scenario[] scenarios)
    {
        var failed = scenarios
            .Where(s => s.Result == ExecutionResult.Failed && s.ErrorMessage is not null)
            .ToArray();

        if (failed.Length == 0)
            return [];

        return failed
            .GroupBy(s => NormalizeKey(s.ErrorMessage!))
            .Where(g => g.Count() >= 2)
            .OrderByDescending(g => g.Count())
            .Select(g => new FailureCluster(g.Key, g.ToArray()))
            .ToArray();
    }

    private static string NormalizeKey(string errorMessage)
    {
        // Use only the first line of the error message
        var firstLine = errorMessage.Split('\n')[0].Trim();
        // Collapse multiple whitespace
        return Regex.Replace(firstLine, @"\s+", " ").Trim();
    }
}
