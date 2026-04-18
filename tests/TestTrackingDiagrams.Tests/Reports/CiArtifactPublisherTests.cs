using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiArtifactPublisherTests
{
    [Fact]
    public void Publish_AzureDevOps_emits_vso_upload_for_each_file()
    {
        var lines = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html", "/reports/Specs.yml"],
            CiEnvironment.AzureDevOps,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: _ => null,
            appendFile: (_, _) => { },
            writeLine: line => lines.Add(line),
            fileExists: _ => true);

        Assert.Equal(2, lines.Count);
        Assert.Contains("##vso[artifact.upload containerfolder=TestReports;artifactname=TestReports]/reports/FeaturesReport.html", lines[0]);
        Assert.Contains("##vso[artifact.upload containerfolder=TestReports;artifactname=TestReports]/reports/Specs.yml", lines[1]);
    }

    [Fact]
    public void Publish_AzureDevOps_uses_custom_artifact_name()
    {
        var lines = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.AzureDevOps,
            artifactName: "MyReports",
            retentionDays: 1,
            getEnvVar: _ => null,
            appendFile: (_, _) => { },
            writeLine: line => lines.Add(line),
            fileExists: _ => true);

        Assert.Single(lines);
        Assert.Contains("artifactname=MyReports", lines[0]);
        Assert.Contains("containerfolder=MyReports", lines[0]);
    }

    [Fact]
    public void Publish_GitHubActions_writes_reports_path_to_github_output()
    {
        string? writtenPath = null;
        var writtenContents = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.GitHubActions,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: name => name == "GITHUB_OUTPUT" ? "/tmp/output" : null,
            appendFile: (path, content) => { writtenPath = path; writtenContents.Add(content); },
            writeLine: _ => { },
            fileExists: _ => true);

        Assert.Equal("/tmp/output", writtenPath);
        Assert.Contains(writtenContents, c => c.StartsWith("reports-path=") && c.TrimEnd('\n').EndsWith("reports"));
    }

    [Fact]
    public void Publish_GitHubActions_does_nothing_when_output_path_missing()
    {
        var called = false;

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.GitHubActions,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: _ => null,
            appendFile: (_, _) => called = true,
            writeLine: _ => called = true,
            fileExists: _ => true);

        Assert.False(called);
    }

    [Fact]
    public void Publish_None_does_nothing()
    {
        var called = false;

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.None,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: _ => null,
            appendFile: (_, _) => called = true,
            writeLine: _ => called = true,
            fileExists: _ => true);

        Assert.False(called);
    }

    [Fact]
    public void Publish_AzureDevOps_skips_files_that_do_not_exist()
    {
        var lines = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html", "/reports/Missing.html"],
            CiEnvironment.AzureDevOps,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: _ => null,
            appendFile: (_, _) => { },
            writeLine: line => lines.Add(line),
            fileExists: path => path == "/reports/FeaturesReport.html");

        Assert.Single(lines);
        Assert.Contains("FeaturesReport.html", lines[0]);
    }

    [Fact]
    public void Publish_GitHubActions_writes_retention_days_output()
    {
        var writtenContents = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.GitHubActions,
            artifactName: "TestReports",
            retentionDays: 1,
            getEnvVar: name => name == "GITHUB_OUTPUT" ? "/tmp/output" : null,
            appendFile: (_, content) => writtenContents.Add(content),
            writeLine: _ => { },
            fileExists: _ => true);

        Assert.Contains(writtenContents, c => c.Contains("reports-retention-days=1"));
    }

    [Fact]
    public void Publish_GitHubActions_uses_custom_retention_days()
    {
        var writtenContents = new List<string>();

        CiArtifactPublisher.Publish(
            ["/reports/FeaturesReport.html"],
            CiEnvironment.GitHubActions,
            artifactName: "TestReports",
            retentionDays: 7,
            getEnvVar: name => name == "GITHUB_OUTPUT" ? "/tmp/output" : null,
            appendFile: (_, content) => writtenContents.Add(content),
            writeLine: _ => { },
            fileExists: _ => true);

        Assert.Contains(writtenContents, c => c.Contains("reports-retention-days=7"));
    }
}
