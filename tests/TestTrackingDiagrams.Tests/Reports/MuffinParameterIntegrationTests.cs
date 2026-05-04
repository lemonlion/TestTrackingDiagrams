using System.Text;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Full end-to-end integration test that exercises the EXACT rendering pipeline
/// for complex parameterized test data (MuffinRecipeTestData with nested records and List&lt;T&gt;).
/// Logs every decision point to expose truncation/missing data.
/// </summary>
public class MuffinParameterIntegrationTests
{
    private readonly StringBuilder _log = new();

    private void Log(string msg) => _log.AppendLine(msg);

    // Exact replicas of BreakfastProvider model types
    private record IngredientSet
    {
        public required string Flour { get; init; }
        public required string Apples { get; init; }
        public required string Cinnamon { get; init; }
    }

    private record BakingProfileData
    {
        public required int Temperature { get; init; }
        public required int DurationMinutes { get; init; }
        public required string PanType { get; init; }
    }

    private record ToppingData
    {
        public required string Name { get; init; }
        public required string Amount { get; init; }
    }

    private record MuffinRecipeTestData
    {
        public required IngredientSet Ingredients { get; init; }
        public required BakingProfileData Baking { get; init; }
        public required List<ToppingData> Toppings { get; init; }
    }

    private record MuffinBatchExpectation
    {
        public required int ExpectedIngredientCount { get; init; }
        public required int ExpectedToppingCount { get; init; }
        public required bool HasBakingInfo { get; init; }
    }

    private static readonly MuffinRecipeTestData ClassicRecipe = new()
    {
        Ingredients = new IngredientSet { Flour = "Plain Flour", Apples = "Granny Smith", Cinnamon = "Ceylon" },
        Baking = new BakingProfileData { Temperature = 180, DurationMinutes = 25, PanType = "Standard" },
        Toppings =
        [
            new ToppingData { Name = "Streusel", Amount = "Light" },
            new ToppingData { Name = "Icing Glaze", Amount = "Drizzle" }
        ]
    };

    private static readonly MuffinRecipeTestData SpicedRecipe = new()
    {
        Ingredients = new IngredientSet { Flour = "Almond Flour", Apples = "Pink Lady", Cinnamon = "Saigon" },
        Baking = new BakingProfileData { Temperature = 190, DurationMinutes = 20, PanType = "Silicone" },
        Toppings =
        [
            new ToppingData { Name = "Cinnamon Sugar", Amount = "Heavy" },
            new ToppingData { Name = "Cream Cheese Swirl", Amount = "Thick" }
        ]
    };

    private static readonly MuffinBatchExpectation ClassicExpectation = new()
    {
        ExpectedIngredientCount = 5, ExpectedToppingCount = 2, HasBakingInfo = true
    };

    private static readonly MuffinBatchExpectation SpicedExpectation = new()
    {
        ExpectedIngredientCount = 5, ExpectedToppingCount = 2, HasBakingInfo = true
    };

    /// <summary>
    /// This test exercises the EXACT xUnit3 rendering path as it would execute
    /// when ExampleRawValues is null (which happens when xUnit3's TestMethodArguments
    /// is not available). This reproduces the user's reported bug where Toppings
    /// is missing and Baking is truncated.
    /// </summary>
    [Fact]
    public void XUnit3_string_path_full_rendering_pipeline_with_diagnostics()
    {
        Log("=== xUnit3 STRING PATH (ExampleRawValues = null) ===");
        Log("This simulates the scenario where raw objects are NOT available.");
        Log("The renderer must fall through to TryRenderFromParsedString.");
        Log("");

        // STEP 1: Show what MuffinRecipeTestData.ToString() produces
        var recipeToString = ClassicRecipe.ToString()!;
        var expectedToString = ClassicExpectation.ToString()!;
        Log($"STEP 1: MuffinRecipeTestData.ToString():");
        Log($"  Length: {recipeToString.Length} chars");
        Log($"  Full value: [{recipeToString}]");
        Log($"  EndsWith(\" }}\") = {recipeToString.EndsWith(" }")}");
        Log("");

        // STEP 2: Build ExampleValues as xUnit3 adapter would (ToString() on each arg)
        var exampleValues = new Dictionary<string, string>
        {
            ["recipeName"] = "Classic",
            ["recipe"] = recipeToString,
            ["expected"] = expectedToString
        };

        Log("STEP 2: ExampleValues dictionary:");
        foreach (var kv in exampleValues)
            Log($"  [{kv.Key}] = [{kv.Value}] (len={kv.Value.Length})");
        Log("");

        // STEP 3: Build scenarios WITHOUT ExampleRawValues (null)
        var s1 = new Scenario
        {
            Id = "s1",
            DisplayName = "Muffins_creation_using_recipe Classic",
            OutlineId = "Muffins_creation_using_recipe",
            ExampleValues = exampleValues,
            ExampleRawValues = null, // KEY: no raw values available
            Result = ExecutionResult.Passed
        };

        var s2 = new Scenario
        {
            Id = "s2",
            DisplayName = "Muffins_creation_using_recipe Spiced Deluxe",
            OutlineId = "Muffins_creation_using_recipe",
            ExampleValues = new Dictionary<string, string>
            {
                ["recipeName"] = "Spiced Deluxe",
                ["recipe"] = SpicedRecipe.ToString()!,
                ["expected"] = SpicedExpectation.ToString()!
            },
            ExampleRawValues = null,
            Result = ExecutionResult.Passed
        };

        Log("STEP 3: Scenarios built (ExampleRawValues = null)");
        Log($"  s1.ExampleRawValues is null: {s1.ExampleRawValues is null}");
        Log("");

        // STEP 4: Run through ParameterGrouper.Analyze
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2]);

        Log("STEP 4: ParameterGrouper.Analyze result:");
        Log($"  Groups: {groups.Length}");
        Log($"  Ungrouped: {ungrouped.Length}");
        Assert.Single(groups);

        var group = groups[0];
        Log($"  Group Rule: {group.Rule}");
        Log($"  Group ParameterNames: [{string.Join(", ", group.ParameterNames)}]");
        Log($"  Group Scenarios count: {group.Scenarios.Length}");
        Log("");

        // STEP 5: Verify the grouped scenarios still have the right values
        var firstScenario = group.Scenarios[0];
        Log("STEP 5: First grouped scenario data:");
        Log($"  ExampleValues is null: {firstScenario.ExampleValues is null}");
        Log($"  ExampleRawValues is null: {firstScenario.ExampleRawValues is null}");
        if (firstScenario.ExampleValues is not null)
            foreach (var kv in firstScenario.ExampleValues)
                Log($"  ExampleValues[{kv.Key}] = [{kv.Value}] (len={kv.Value.Length})");
        Log("");

        // STEP 6: Simulate the ReportGenerator rendering logic EXACTLY
        Log("STEP 6: Cell-by-cell rendering (simulating ReportGenerator):");
        var renderedCells = new Dictionary<string, string>();

        foreach (var name in group.ParameterNames)
        {
            var rawValue = firstScenario.ExampleRawValues?.GetValueOrDefault(name);
            var stringValue = firstScenario.ExampleValues?.GetValueOrDefault(name, "") ?? "";

            Log($"  --- Parameter [{name}] ---");
            Log($"    rawValue: {(rawValue is null ? "NULL" : rawValue.GetType().Name)}");
            Log($"    stringValue length: {stringValue.Length}");

            var cellHtml = new StringBuilder();

            if (rawValue is not null && ParameterValueRenderer.IsSmallComplexObject(rawValue))
            {
                Log($"    DECISION: R3 SubTable (raw small complex)");
                cellHtml.Append("<td>");
                ParameterValueRenderer.RenderSubTable(cellHtml, rawValue);
                cellHtml.Append("</td>");
            }
            else if (rawValue is not null && ParameterValueRenderer.IsComplexValue(rawValue))
            {
                Log($"    DECISION: R4 Expandable (raw complex)");
                cellHtml.Append("<td>");
                ParameterValueRenderer.RenderExpandable(cellHtml, rawValue);
                cellHtml.Append("</td>");
            }
            else
            {
                Log($"    rawValue is null, trying string-based rendering...");

                // TryRenderFromParsedString
                var tdBody = new StringBuilder();
                if (ParameterValueRenderer.TryRenderFromParsedString(tdBody, stringValue))
                {
                    Log($"    DECISION: String-based R3/R4 (TryRenderFromParsedString succeeded)");
                    cellHtml.Append("<td>");
                    cellHtml.Append(tdBody);
                    cellHtml.Append("</td>");
                }
                else
                {
                    Log($"    DECISION: Plain scalar text (all rendering failed)");
                    cellHtml.Append($"<td class=\"mono\">{System.Net.WebUtility.HtmlEncode(stringValue)}</td>");
                }
            }

            renderedCells[name] = cellHtml.ToString();
            Log($"    HTML output length: {cellHtml.Length}");
            Log($"    HTML output: [{cellHtml}]");
            Log("");
        }

        // STEP 7: Detailed analysis of the recipe cell parsing
        Log("STEP 7: Detailed TryParseRecordToString analysis for recipe:");
        var parsed = ParameterParser.TryParseRecordToString(recipeToString);
        Log($"  TryParseRecordToString returned: {(parsed is null ? "NULL" : $"{parsed.Count} properties")}");
        if (parsed is not null)
        {
            foreach (var kv in parsed)
                Log($"    [{kv.Key}] = [{kv.Value}] (len={kv.Value.Length})");
        }
        Log("");

        // Write full log to temp file for inspection
        var logPath = Path.Combine(Path.GetTempPath(), "muffin-xunit3-integration.txt");
        File.WriteAllText(logPath, _log.ToString());

        // ASSERTIONS: Verify the recipe cell HTML contains all 3 properties
        var recipeHtml = renderedCells["recipe"];

        // Must contain all 3 property names as sub-table headers
        Assert.Contains("<th>Ingredients</th>", recipeHtml);
        Assert.Contains("<th>Baking</th>", recipeHtml);
        Assert.Contains("<th>Toppings</th>", recipeHtml);

        // TryParseRecordToString must extract all 3 properties
        Assert.NotNull(parsed);
        Assert.Equal(3, parsed!.Count);
        Assert.True(parsed.ContainsKey("Ingredients"), "Parsed dict must have Ingredients key");
        Assert.True(parsed.ContainsKey("Baking"), "Parsed dict must have Baking key");
        Assert.True(parsed.ContainsKey("Toppings"), "Parsed dict must have Toppings key");
    }

    /// <summary>
    /// This test exercises the xUnit3 rendering path when ExampleRawValues IS populated
    /// (the raw MuffinRecipeTestData object is available). This should render as expandable (R4).
    /// </summary>
    [Fact]
    public void XUnit3_raw_path_full_rendering_pipeline_with_diagnostics()
    {
        Log("=== xUnit3 RAW PATH (ExampleRawValues populated) ===");
        Log("This simulates the scenario where raw objects ARE available via TestMethodArguments.");
        Log("");

        // Build structured params the way xUnit3 adapter does
        object?[] args = ["Classic", ClassicRecipe, ClassicExpectation];
        string?[] paramNames = ["recipeName", "recipe", "expected"];
        var structured = ParameterParser.ExtractStructuredParametersWithRaw(args, paramNames)!;

        Log("STEP 1: ExtractStructuredParametersWithRaw result:");
        Log($"  StringValues count: {structured.Value.StringValues.Count}");
        Log($"  RawValues count: {structured.Value.RawValues.Count}");
        foreach (var kv in structured.Value.RawValues)
            Log($"  RawValues[{kv.Key}] type = {kv.Value?.GetType().FullName ?? "null"}");
        Log("");

        // Build scenarios WITH raw values
        var s1 = new Scenario
        {
            Id = "s1",
            DisplayName = "Muffins_creation_using_recipe Classic",
            OutlineId = "Muffins_creation_using_recipe",
            ExampleValues = structured.Value.StringValues,
            ExampleRawValues = structured.Value.RawValues,
            Result = ExecutionResult.Passed
        };

        var structured2 = ParameterParser.ExtractStructuredParametersWithRaw(
            ["Spiced Deluxe", SpicedRecipe, SpicedExpectation], paramNames)!;
        var s2 = new Scenario
        {
            Id = "s2",
            DisplayName = "Muffins_creation_using_recipe Spiced Deluxe",
            OutlineId = "Muffins_creation_using_recipe",
            ExampleValues = structured2.Value.StringValues,
            ExampleRawValues = structured2.Value.RawValues,
            Result = ExecutionResult.Passed
        };

        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        var group = groups[0];
        var firstScenario = group.Scenarios[0];

        Log("STEP 2: After ParameterGrouper.Analyze:");
        Log($"  Group Rule: {group.Rule}");
        Log($"  ExampleRawValues is null: {firstScenario.ExampleRawValues is null}");
        if (firstScenario.ExampleRawValues is not null)
            foreach (var kv in firstScenario.ExampleRawValues)
                Log($"  ExampleRawValues[{kv.Key}] type = {kv.Value?.GetType().Name ?? "null"}");
        Log("");

        // Render cells
        Log("STEP 3: Cell rendering decisions:");
        var renderedCells = new Dictionary<string, string>();

        foreach (var name in group.ParameterNames)
        {
            var rawValue = firstScenario.ExampleRawValues?.GetValueOrDefault(name);
            var stringValue = firstScenario.ExampleValues?.GetValueOrDefault(name, "") ?? "";

            Log($"  --- Parameter [{name}] ---");
            Log($"    rawValue type: {rawValue?.GetType().Name ?? "NULL"}");
            Log($"    IsSmallComplexObject: {(rawValue is not null ? ParameterValueRenderer.IsSmallComplexObject(rawValue) : "N/A (null)")}");
            Log($"    IsComplexValue: {(rawValue is not null ? ParameterValueRenderer.IsComplexValue(rawValue) : "N/A (null)")}");

            var cellHtml = new StringBuilder();
            string decision;

            if (rawValue is not null && ParameterValueRenderer.IsSmallComplexObject(rawValue))
            {
                decision = "R3 SubTable (raw small complex)";
                cellHtml.Append("<td>");
                ParameterValueRenderer.RenderSubTable(cellHtml, rawValue);
                cellHtml.Append("</td>");
            }
            else if (rawValue is not null && ParameterValueRenderer.IsComplexValue(rawValue))
            {
                decision = "R4 Expandable (raw complex)";
                cellHtml.Append("<td>");
                ParameterValueRenderer.RenderExpandable(cellHtml, rawValue);
                cellHtml.Append("</td>");
            }
            else
            {
                var tdBody = new StringBuilder();
                if (ParameterValueRenderer.TryRenderFromParsedString(tdBody, stringValue))
                {
                    decision = "String-based R3/R4";
                    cellHtml.Append("<td>");
                    cellHtml.Append(tdBody);
                    cellHtml.Append("</td>");
                }
                else
                {
                    decision = "Plain scalar text";
                    cellHtml.Append($"<td class=\"mono\">{System.Net.WebUtility.HtmlEncode(stringValue)}</td>");
                }
            }

            Log($"    DECISION: {decision}");
            Log($"    HTML length: {cellHtml.Length}");
            renderedCells[name] = cellHtml.ToString();
            Log("");
        }

        // Write log
        var logPath = Path.Combine(Path.GetTempPath(), "muffin-xunit3-raw-integration.txt");
        File.WriteAllText(logPath, _log.ToString());

        // ASSERTIONS for raw path:
        // recipeName should be scalar
        Assert.Contains("class=\"mono\"", renderedCells["recipeName"]);

        // recipe should be expandable (R4) since it has nested complex properties
        var recipeHtml = renderedCells["recipe"];
        Assert.Contains("param-expand", recipeHtml);
        Assert.Contains("<details", recipeHtml);
        Assert.Contains("Ingredients", recipeHtml);
        Assert.Contains("Baking", recipeHtml);
        Assert.Contains("Toppings", recipeHtml);

        // expected should be sub-table (R3) since it has all scalar properties
        var expectedHtml = renderedCells["expected"];
        Assert.Contains("cell-subtable", expectedHtml);
        Assert.Contains("<th>ExpectedIngredientCount</th>", expectedHtml);
    }
}
