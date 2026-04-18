namespace TestTrackingDiagrams.Reports;

public enum CiEnvironment
{
    None,
    GitHubActions,
    AzureDevOps
}

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
