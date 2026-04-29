using System.Security.Cryptography;
using System.Text;

namespace TestTrackingDiagrams.Reports;

/// <summary>
/// Computes deterministic stable IDs for scenarios. Unlike runtime <see cref="Scenario.Id"/>
/// (which varies by test framework and can be randomised), stable IDs are consistent across runs.
/// </summary>
public static class ScenarioStableId
{
    public static string Compute(string featureName, string scenarioDisplayName, string? outlineId = null)
    {
        var input = $"{featureName}::{scenarioDisplayName}";
        if (outlineId is not null)
            input = $"{featureName}::{outlineId}::{scenarioDisplayName}";

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexString(hash)[..16].ToLowerInvariant();
    }
}
