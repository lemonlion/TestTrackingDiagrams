using System.Runtime.CompilerServices;
using Example.Api.Tests.Integration.Helpers;
using TestTrackingDiagrams;

namespace Example.Api.Tests.Integration.Tests;

/// <summary>
/// Tests diagram field focus features across all component test projects.
/// All tests run sequentially to avoid port conflicts.
/// </summary>
[Collection("SequentialTests")]
public class FieldFocusTests
{
    public static TheoryData<string> AllProjects()
    {
        var data = new TheoryData<string>();
        foreach (var project in TestProjects.All)
            data.Add(project);
        return data;
    }

    // ──────────────────── Emphasis Tests ────────────────────

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusBoldEmphasis_HighlightsRequestFields(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusColoredEmphasis_HighlightsRequestFields(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Colored",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Colored);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusBoldAndColoredEmphasis_CombinesMarkup(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold,Colored",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold | FocusEmphasis.Colored);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusNoneEmphasis_NoMarkupOnFocusedFields(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "None",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        // Focused field should have no emphasis markup
        PlantUmlAssertions.AssertFieldHasNoEmphasisMarkup(happyPathDiagram, "Eggs");
        // Non-focused fields should still be de-emphasized
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
    }

    // ──────────────────── De-emphasis Tests ────────────────────

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task DeEmphasisLightGray_GraysNonFocusedFields(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task DeEmphasisSmallerText_ShrinksNonFocusedFields(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "SmallerText",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.SmallerText);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task DeEmphasisHidden_ReplacesNonFocusedWithEllipsis(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "Hidden",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        // Non-focused fields should be replaced with "..."
        PlantUmlAssertions.AssertHiddenDeEmphasis(happyPathDiagram, "Milk");
        PlantUmlAssertions.AssertHiddenDeEmphasis(happyPathDiagram, "Flour");
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task DeEmphasisCombined_LightGrayAndSmallerText(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray,SmallerText",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray | FocusDeEmphasis.SmallerText);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task DeEmphasisNone_NormalRenderingForNonFocused(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "None",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        // Focused field still emphasized
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        // Non-focused fields should have no de-emphasis markup
        PlantUmlAssertions.AssertFieldHasNoEmphasisMarkup(happyPathDiagram, "Milk");
    }

    // ──────────────────── Field Targeting Tests ────────────────────

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusSingleRequestField_OnlyThatFieldEmphasized(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusMultipleRequestFields_AllEmphasized(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs,Milk"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Milk", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusResponseFields_EmphasizesResponseBody(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_RESPONSE_FIELDS"] = "BatchId"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "BatchId", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Ingredients", FocusDeEmphasis.LightGray);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusBothRequestAndResponse_BothEmphasized(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs",
            ["TTD_FOCUS_RESPONSE_FIELDS"] = "BatchId"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "BatchId", FocusEmphasis.Bold);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusNonExistentField_AllFieldsDeEmphasized(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "NonExistentField"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        // When the focused field doesn't exist in the body, the library returns JSON unchanged
        // (no fields matched → no markup applied). Verify no focus markup is present.
        PlantUmlAssertions.AssertNoFocusMarkup(happyPathDiagram);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task NoFocusSet_NormalRendering(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray"
            // No TTD_FOCUS_REQUEST_FIELDS or TTD_FOCUS_RESPONSE_FIELDS
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertNoFocusMarkup(happyPathDiagram);
    }

    // ──────────────────── Combined Configuration Tests ────────────────────

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusWithSeparateSetup_BothApplied(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_SEPARATE_SETUP"] = "true",
            ["TTD_HIGHLIGHT_SETUP"] = "true",
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsSetupPartition(happyPathDiagram, highlighted: true);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
    }

    [Theory]
    [MemberData(nameof(AllProjects))]
    public async Task FocusWithHiddenDeEmphasis_ProductionScenario(string projectName)
    {
        var plantUmlSources = await RunWithFocusConfig(projectName, new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "Hidden",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertHiddenDeEmphasis(happyPathDiagram, "Milk");
        PlantUmlAssertions.AssertHiddenDeEmphasis(happyPathDiagram, "Flour");
    }

    // ──────────────────── Helpers ────────────────────

    private static async Task<string[]> RunWithFocusConfig(
        string projectName,
        Dictionary<string, string> envVars,
        [CallerMemberName] string callerName = "")
    {
        // Ensure baseline config
        envVars.TryAdd("TTD_SPECIFICATIONS_TITLE", "Dessert Provider Specifications");

        var result = await TestProjectRunner.RunAsync(projectName, envVars, runLabel: callerName);
        Assert.True(result.Success, $"{projectName} failed:\n{result.StandardError}\n{result.StandardOutput}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.FeaturesReportHtml);

        var plantUmlSources = await ReportParser.ExtractPlantUmlSourcesAsync(reports.FeaturesReportHtml);
        Assert.NotEmpty(plantUmlSources);
        return plantUmlSources;
    }

    private static string GetHappyPathDiagram(string[] plantUmlSources)
    {
        // The happy path diagram contains "Cow Service" (external dependency) and "batchId" (successful response).
        // The batchId filter distinguishes it from error-path diagrams that also call Cow Service.
        var happyPath = plantUmlSources
            .Where(p => p.Contains("CowService") || p.Contains("Cow Service"))
            .Where(p => p.Contains("batchId", StringComparison.OrdinalIgnoreCase))
            .OrderByDescending(p => p.Length)
            .FirstOrDefault();

        Assert.NotNull(happyPath);
        return happyPath;
    }
}
