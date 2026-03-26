using Example.Api.Tests.Integration.Helpers;

namespace Example.Api.Tests.Integration.Tests;

[Collection("SequentialTests")]
public class ConfigurationOverrideTests
{
    private const string TargetProject = TestProjects.XUnit3;

    [Fact]
    public async Task SeparateSetupTrue_DiagramContainsSetupPartition()
    {
        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        PlantUmlAssertions.AssertContainsSetupPartition(happyPathDiagram, highlighted: true);
    }

    [Fact]
    public async Task SeparateSetupFalse_NoSetupPartition()
    {
        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "false",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        foreach (var puml in plantUmlSources)
            PlantUmlAssertions.AssertNoSetupPartition(puml);
    }

    [Fact]
    public async Task HighlightSetupTrue_PartitionHasColor()
    {
        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        Assert.Contains("#E2E2F0", happyPathDiagram);
    }

    [Fact]
    public async Task HighlightSetupFalse_PartitionNoColor()
    {
        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "false",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

        var plantUmlSources = await GetPlantUmlSources(result.ReportsFolderPath);
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("CowService") || p.Contains("Cow Service"));
        Assert.Contains("partition Setup", happyPathDiagram);
        Assert.DoesNotContain("#E2E2F0", happyPathDiagram);
    }

    [Fact]
    public async Task CustomSpecificationsTitle_AppearsInReport()
    {
        const string customTitle = "My Custom Integration Title";

        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SPECIFICATIONS_TITLE"] = customTitle,
            ["TTD_SEPARATE_SETUP"] = "true"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.SpecificationsHtml);

        var title = await ReportParser.ExtractTitleAsync(reports.SpecificationsHtml);
        Assert.Equal(customTitle, title);
    }

    [Fact]
    public async Task ExcludedHeaders_NotInDiagram()
    {
        var result = await TestProjectRunner.RunAsync(TargetProject, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications",
            ["TTD_EXCLUDED_HEADERS"] = "traceparent,Request-Id"
        });

        Assert.True(result.Success, $"Failed:\n{result.StandardError}");

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
