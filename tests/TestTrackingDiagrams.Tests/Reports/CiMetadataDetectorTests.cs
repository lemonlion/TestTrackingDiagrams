using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiMetadataDetectorTests
{
    [Fact]
    public void Detect_returns_null_when_not_ci()
    {
        var result = CiMetadataDetector.Detect(_ => null);
        Assert.Null(result);
    }

    [Fact]
    public void Detect_returns_github_metadata_from_env_vars()
    {
        var envVars = new Dictionary<string, string?>
        {
            ["GITHUB_ACTIONS"] = "true",
            ["GITHUB_RUN_NUMBER"] = "42",
            ["GITHUB_REF_NAME"] = "main",
            ["GITHUB_SHA"] = "abc123def456789",
            ["GITHUB_SERVER_URL"] = "https://github.com",
            ["GITHUB_REPOSITORY"] = "owner/repo",
            ["GITHUB_RUN_ID"] = "12345"
        };

        var result = CiMetadataDetector.Detect(key => envVars.GetValueOrDefault(key));

        Assert.NotNull(result);
        Assert.Equal(CiEnvironment.GitHubActions, result.Provider);
        Assert.Equal("42", result.BuildNumber);
        Assert.Equal("main", result.Branch);
        Assert.Equal("abc123def456789", result.CommitSha);
        Assert.Equal("https://github.com/owner/repo/actions/runs/12345", result.PipelineUrl);
        Assert.Equal("owner/repo", result.Repository);
        Assert.Equal("12345", result.RunId);
    }

    [Fact]
    public void Detect_returns_azure_devops_metadata_from_env_vars()
    {
        var envVars = new Dictionary<string, string?>
        {
            ["TF_BUILD"] = "True",
            ["BUILD_BUILDNUMBER"] = "20240101.1",
            ["BUILD_SOURCEBRANCH"] = "refs/heads/main",
            ["BUILD_SOURCEVERSION"] = "abc123def456789",
            ["SYSTEM_TEAMFOUNDATIONSERVERURI"] = "https://dev.azure.com/org/",
            ["SYSTEM_TEAMPROJECT"] = "MyProject",
            ["BUILD_BUILDID"] = "999",
            ["BUILD_REPOSITORY_NAME"] = "MyRepo"
        };

        var result = CiMetadataDetector.Detect(key => envVars.GetValueOrDefault(key));

        Assert.NotNull(result);
        Assert.Equal(CiEnvironment.AzureDevOps, result.Provider);
        Assert.Equal("20240101.1", result.BuildNumber);
        Assert.Equal("refs/heads/main", result.Branch);
        Assert.Equal("abc123def456789", result.CommitSha);
        Assert.Equal("https://dev.azure.com/org/MyProject/_build/results?buildId=999", result.PipelineUrl);
        Assert.Equal("MyRepo", result.Repository);
        Assert.Equal("999", result.RunId);
    }
}
