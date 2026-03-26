using Example.Api.Tests.Integration.Helpers;

namespace Example.Api.Tests.Integration.Tests;

[Collection("SequentialTests")]
public class ReportGenerationTests
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
    public async Task Project_generates_all_report_files(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}\n{result.StandardOutput}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);

        Assert.NotNull(reports.SpecificationsHtml);
        Assert.NotNull(reports.FeaturesReportHtml);
        Assert.NotNull(reports.SpecificationsYaml);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task Project_report_contains_expected_scenarios(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.FeaturesReportHtml);

        var scenarios = await ReportParser.ExtractScenariosAsync(reports.FeaturesReportHtml);
        Assert.True(scenarios.Length >= 2, $"Expected at least 2 scenarios, got {scenarios.Length}");
        Assert.Contains(scenarios, s => s.IsHappyPath);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task Project_plantuml_contains_expected_participants_and_arrows(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.FeaturesReportHtml);

        var plantUmlSources = await ReportParser.ExtractPlantUmlSourcesAsync(reports.FeaturesReportHtml);
        Assert.NotEmpty(plantUmlSources);

        // All diagrams should contain the expected service participants
        foreach (var puml in plantUmlSources)
        {
            PlantUmlAssertions.AssertContainsParticipants(puml, "Dessert Provider");
        }

        // The happy path diagram involves the Cow Service (milk call)
        var happyPathDiagram = plantUmlSources.First(p => p.Contains("Cow Service", StringComparison.OrdinalIgnoreCase));
        PlantUmlAssertions.AssertContainsParticipants(happyPathDiagram, "Dessert Provider", "Cow Service");
        PlantUmlAssertions.AssertContainsSequenceArrow(happyPathDiagram, "DessertProvider", "CowService");
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task Project_yaml_spec_has_valid_structure(string projectName)
    {
        var result = await TestProjectRunner.RunAsync(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_SPECIFICATIONS_TITLE"] = "Dessert Provider Specifications"
        });

        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.SpecificationsYaml);

        var yaml = await ReportParser.ReadYamlAsync(reports.SpecificationsYaml);
        Assert.Contains("Title: Dessert Provider Specifications", yaml);
        Assert.Contains("Features:", yaml);
        Assert.Contains("Scenarios:", yaml);
        Assert.Contains("IsHappyPath: true", yaml);
    }
}
