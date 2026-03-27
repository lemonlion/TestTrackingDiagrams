using Example.Api.Tests.Integration.Helpers;

namespace Example.Api.Tests.Integration.Tests;

[Collection("SequentialTests")]
public class ConfigurationOverrideTests
{
    public static TheoryData<string> AllProjects()
    {
        var data = new TheoryData<string>();
        foreach (var project in TestProjects.All)
            data.Add(project);
        return data;
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task SeparateSetupTrue_DiagramContainsSetupPartition(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        PlantUmlAssertions.AssertContainsSetupPartition(happyPathDiagram, highlighted: true);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task SeparateSetupFalse_NoSetupPartition(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "false",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        foreach (var puml in plantUmlSources)
            PlantUmlAssertions.AssertNoSetupPartition(puml);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task HighlightSetupTrue_PartitionHasColor(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        Assert.Contains("#E2E2F0", happyPathDiagram);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task HighlightSetupFalse_PartitionNoColor(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "false",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        Assert.Contains("partition Setup", happyPathDiagram);
        Assert.DoesNotContain("#E2E2F0", happyPathDiagram);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task CustomSpecificationsTitle_AppearsInReport(string projectName)
    {
        const string customTitle = "My Custom Integration Title";

        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SPECIFICATIONS_TITLE"] = customTitle,
            ["TTD_SEPARATE_SETUP"] = "true"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.SpecificationsHtml);

        var title = await ReportParser.ExtractTitleAsync(reports.SpecificationsHtml);
        Assert.NotNull(title);
        Assert.Contains(customTitle, title);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task ExcludedHeaders_NotInDiagram(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications",
            ["TTD_EXCLUDED_HEADERS"] = "traceparent,Request-Id"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        foreach (var puml in plantUmlSources)
        {
            Assert.DoesNotContain("[traceparent=", puml);
            Assert.DoesNotContain("[Request-Id=", puml);
        }
    }

    private static async Task<string[]> GetPlantUmlSources(string reportsFolderPath)
    {
        var reports = ReportParser.GetReportFiles(reportsFolderPath);
        Assert.NotNull(reports.FeaturesReportHtml);
        var sources = await ReportParser.ExtractPlantUmlSourcesAsync(reports.FeaturesReportHtml);
        Assert.NotEmpty(sources);
        return sources;
    }
}
