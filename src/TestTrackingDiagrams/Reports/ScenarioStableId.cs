using System.Security.Cryptography;
using System.Text;

namespace TestTrackingDiagrams.Reports;

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
