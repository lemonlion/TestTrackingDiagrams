using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiEnvironmentDetectorTests
{
    [Fact]
    public void Detect_returns_GitHubActions_when_GITHUB_ACTIONS_set()
    {
        var result = CiEnvironmentDetector.Detect(name => name == "GITHUB_ACTIONS" ? "true" : null);

        Assert.Equal(CiEnvironment.GitHubActions, result);
    }

    [Fact]
    public void Detect_returns_AzureDevOps_when_TF_BUILD_set()
    {
        var result = CiEnvironmentDetector.Detect(name => name == "TF_BUILD" ? "True" : null);

        Assert.Equal(CiEnvironment.AzureDevOps, result);
    }

    [Fact]
    public void Detect_returns_None_when_no_ci_vars_set()
    {
        var result = CiEnvironmentDetector.Detect(_ => null);

        Assert.Equal(CiEnvironment.None, result);
    }

    [Fact]
    public void Detect_prefers_GitHubActions_when_both_set()
    {
        var result = CiEnvironmentDetector.Detect(name => name switch
        {
            "GITHUB_ACTIONS" => "true",
            "TF_BUILD" => "True",
            _ => null
        });

        Assert.Equal(CiEnvironment.GitHubActions, result);
    }
}
