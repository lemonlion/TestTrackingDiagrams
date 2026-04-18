namespace TestTrackingDiagrams.Reports;

public record CiMetadata(
    CiEnvironment Provider,
    string? BuildNumber,
    string? Branch,
    string? CommitSha,
    string? PipelineUrl,
    string? Repository,
    string? RunId);

public static class CiMetadataDetector
{
    public static CiMetadata? Detect() => Detect(Environment.GetEnvironmentVariable);

    internal static CiMetadata? Detect(Func<string, string?> getEnvVar)
    {
        var provider = CiEnvironmentDetector.Detect(getEnvVar);
        return provider switch
        {
            CiEnvironment.GitHubActions => DetectGitHub(getEnvVar),
            CiEnvironment.AzureDevOps => DetectAzureDevOps(getEnvVar),
            _ => null
        };
    }

    private static CiMetadata DetectGitHub(Func<string, string?> getEnvVar)
    {
        var runId = getEnvVar("GITHUB_RUN_ID");
        var serverUrl = getEnvVar("GITHUB_SERVER_URL");
        var repo = getEnvVar("GITHUB_REPOSITORY");

        string? pipelineUrl = null;
        if (!string.IsNullOrEmpty(serverUrl) && !string.IsNullOrEmpty(repo) && !string.IsNullOrEmpty(runId))
            pipelineUrl = $"{serverUrl}/{repo}/actions/runs/{runId}";

        return new CiMetadata(
            Provider: CiEnvironment.GitHubActions,
            BuildNumber: getEnvVar("GITHUB_RUN_NUMBER"),
            Branch: getEnvVar("GITHUB_REF_NAME"),
            CommitSha: getEnvVar("GITHUB_SHA"),
            PipelineUrl: pipelineUrl,
            Repository: repo,
            RunId: runId);
    }

    private static CiMetadata DetectAzureDevOps(Func<string, string?> getEnvVar)
    {
        var buildId = getEnvVar("BUILD_BUILDID");
        var serverUri = getEnvVar("SYSTEM_TEAMFOUNDATIONSERVERURI");
        var teamProject = getEnvVar("SYSTEM_TEAMPROJECT");

        string? pipelineUrl = null;
        if (!string.IsNullOrEmpty(serverUri) && !string.IsNullOrEmpty(teamProject) && !string.IsNullOrEmpty(buildId))
            pipelineUrl = $"{serverUri.TrimEnd('/')}/{teamProject}/_build/results?buildId={buildId}";

        return new CiMetadata(
            Provider: CiEnvironment.AzureDevOps,
            BuildNumber: getEnvVar("BUILD_BUILDNUMBER"),
            Branch: getEnvVar("BUILD_SOURCEBRANCH"),
            CommitSha: getEnvVar("BUILD_SOURCEVERSION"),
            PipelineUrl: pipelineUrl,
            Repository: getEnvVar("BUILD_REPOSITORY_NAME"),
            RunId: buildId);
    }
}
