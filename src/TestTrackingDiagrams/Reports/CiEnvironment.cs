namespace TestTrackingDiagrams.Reports;

/// <summary>
/// The CI/CD platform where the test run is executing.
/// </summary>
public enum CiEnvironment
{
    /// <summary>Not running in a known CI environment.</summary>
    None,

    /// <summary>GitHub Actions.</summary>
    GitHubActions,

    /// <summary>Azure DevOps Pipelines.</summary>
    AzureDevOps
}

/// <summary>
/// Detects the current CI environment from environment variables.
/// </summary>
public static class CiEnvironmentDetector
{
    public static CiEnvironment Detect() => Detect(Environment.GetEnvironmentVariable);

    internal static CiEnvironment Detect(Func<string, string?> getEnvVar)
    {
        if (!string.IsNullOrEmpty(getEnvVar("GITHUB_ACTIONS")))
            return CiEnvironment.GitHubActions;
        if (!string.IsNullOrEmpty(getEnvVar("TF_BUILD")))
            return CiEnvironment.AzureDevOps;
        return CiEnvironment.None;
    }
}
