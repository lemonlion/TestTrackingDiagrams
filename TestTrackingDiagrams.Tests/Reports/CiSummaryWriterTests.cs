using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class CiSummaryWriterTests
{
    [Fact]
    public void Write_GitHubActions_appends_to_step_summary_file()
    {
        string? writtenPath = null;
        string? writtenContent = null;

        CiSummaryWriter.Write(
            "# Summary",
            CiEnvironment.GitHubActions,
            getEnvVar: name => name == "GITHUB_STEP_SUMMARY" ? "/tmp/summary.md" : null,
            appendFile: (path, content) => { writtenPath = path; writtenContent = content; },
            writeLine: _ => { });

        Assert.Equal("/tmp/summary.md", writtenPath);
        Assert.Equal("# Summary", writtenContent);
    }

    [Fact]
    public void Write_AzureDevOps_writes_file_and_emits_vso_command()
    {
        string? writtenLine = null;
        string? writtenPath = null;
        string? writtenContent = null;

        CiSummaryWriter.Write(
            "# Summary",
            CiEnvironment.AzureDevOps,
            getEnvVar: _ => null,
            appendFile: (path, content) => { writtenPath = path; writtenContent = content; },
            writeLine: line => writtenLine = line);

        Assert.NotNull(writtenPath);
        Assert.EndsWith(".md", writtenPath);
        Assert.Equal("# Summary", writtenContent);
        Assert.NotNull(writtenLine);
        Assert.Contains("##vso[task.uploadsummary]", writtenLine);
        Assert.Contains(writtenPath, writtenLine);
    }

    [Fact]
    public void Write_None_does_nothing()
    {
        var called = false;

        CiSummaryWriter.Write(
            "# Summary",
            CiEnvironment.None,
            getEnvVar: _ => null,
            appendFile: (_, _) => called = true,
            writeLine: _ => called = true);

        Assert.False(called);
    }

    [Fact]
    public void Write_GitHubActions_does_nothing_when_summary_path_missing()
    {
        var called = false;

        CiSummaryWriter.Write(
            "# Summary",
            CiEnvironment.GitHubActions,
            getEnvVar: _ => null,
            appendFile: (_, _) => called = true,
            writeLine: _ => { });

        Assert.False(called);
    }

    [Fact]
    public void Write_AzureDevOps_vso_path_contains_md_extension()
    {
        string? writtenLine = null;

        CiSummaryWriter.Write(
            "# Summary",
            CiEnvironment.AzureDevOps,
            getEnvVar: _ => null,
            appendFile: (_, _) => { },
            writeLine: line => writtenLine = line);

        Assert.NotNull(writtenLine);
        Assert.Contains(".md", writtenLine);
    }
}
