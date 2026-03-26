using Example.Api.Tests.Integration.Helpers;
using TestTrackingDiagrams;

namespace Example.Api.Tests.Integration.Tests;

/// <summary>
/// Uses xUnit3 project as the primary target for field focus testing.
/// All tests run sequentially to avoid port conflicts.
/// </summary>
[Collection("SequentialTests")]
public class FieldFocusTests
{
    private const string TargetProject = TestProjects.XUnit3;

    // ──────────────────── Emphasis Tests ────────────────────

    [Fact]
    public async Task FocusBoldEmphasis_HighlightsRequestFields()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusColoredEmphasis_HighlightsRequestFields()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Colored",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Colored);
    }

    [Fact]
    public async Task FocusBoldAndColoredEmphasis_CombinesMarkup()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold,Colored",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "Eggs", FocusEmphasis.Bold | FocusEmphasis.Colored);
    }

    [Fact]
    public async Task FocusNoneEmphasis_NoMarkupOnFocusedFields()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task DeEmphasisLightGray_GraysNonFocusedFields()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Fact]
    public async Task DeEmphasisSmallerText_ShrinksNonFocusedFields()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "SmallerText",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.SmallerText);
    }

    [Fact]
    public async Task DeEmphasisHidden_ReplacesNonFocusedWithEllipsis()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task DeEmphasisCombined_LightGrayAndSmallerText()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray,SmallerText",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "Eggs"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray | FocusDeEmphasis.SmallerText);
    }

    [Fact]
    public async Task DeEmphasisNone_NormalRenderingForNonFocused()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusSingleRequestField_OnlyThatFieldEmphasized()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusMultipleRequestFields_AllEmphasized()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusResponseFields_EmphasizesResponseBody()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_RESPONSE_FIELDS"] = "BatchId"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertContainsFocusMarkup(happyPathDiagram, "BatchId", FocusEmphasis.Bold);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Ingredients", FocusDeEmphasis.LightGray);
    }

    [Fact]
    public async Task FocusBothRequestAndResponse_BothEmphasized()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusNonExistentField_AllFieldsDeEmphasized()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray",
            ["TTD_FOCUS_REQUEST_FIELDS"] = "NonExistentField"
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        // All real fields should be de-emphasized since the focused field doesn't exist
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Eggs", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Milk", FocusDeEmphasis.LightGray);
        PlantUmlAssertions.AssertContainsDeEmphasisMarkup(happyPathDiagram, "Flour", FocusDeEmphasis.LightGray);
    }

    [Fact]
    public async Task NoFocusSet_NormalRendering()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
        {
            ["TTD_FOCUS_EMPHASIS"] = "Bold",
            ["TTD_FOCUS_DE_EMPHASIS"] = "LightGray"
            // No TTD_FOCUS_REQUEST_FIELDS or TTD_FOCUS_RESPONSE_FIELDS
        });

        var happyPathDiagram = GetHappyPathDiagram(plantUmlSources);
        PlantUmlAssertions.AssertNoFocusMarkup(happyPathDiagram);
    }

    // ──────────────────── Combined Configuration Tests ────────────────────

    [Fact]
    public async Task FocusWithSeparateSetup_BothApplied()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    [Fact]
    public async Task FocusWithHiddenDeEmphasis_ProductionScenario()
    {
        var plantUmlSources = await RunWithFocusConfig(new Dictionary<string, string>
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

    private static async Task<string[]> RunWithFocusConfig(Dictionary<string, string> envVars)
    {
        // Ensure baseline config
        envVars.TryAdd("TTD_SPECIFICATIONS_TITLE", "Dessert Provider Specifications");

        var result = await TestProjectRunner.RunAsync(TargetProject, envVars);
        Assert.True(result.Success, $"Test run failed:\n{result.StandardError}\n{result.StandardOutput}");

        var reports = ReportParser.GetReportFiles(result.ReportsFolderPath);
        Assert.NotNull(reports.FeaturesReportHtml);

        var plantUmlSources = await ReportParser.ExtractPlantUmlSourcesAsync(reports.FeaturesReportHtml);
        Assert.NotEmpty(plantUmlSources);
        return plantUmlSources;
    }

    private static string GetHappyPathDiagram(string[] plantUmlSources)
    {
        // The happy path diagram is the one with more HTTP interactions (involves milk/eggs/flour + cake)
        // It will contain "Cow Service" participant (the milk endpoint calls an external dependency)
        var happyPath = plantUmlSources
            .Where(p => p.Contains("CowService") || p.Contains("Cow Service"))
            .OrderByDescending(p => p.Length)
            .FirstOrDefault();

        Assert.NotNull(happyPath);
        return happyPath;
    }
}
