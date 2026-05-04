using System.Text;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

/// <summary>
/// Tests for rendering complex parameterized test data (nested records, collections)
/// through the string-based (LightBDD) and raw-object (xUnit3) parameter rendering paths.
/// Uses real-world data structures matching BreakfastProvider's MuffinRecipeTestData.
/// </summary>
public class MuffinParameterRenderingTests
{
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

    [Fact]
    public void LightBDD_path_renders_nested_records_as_subtables_and_cleans_collection_types()
    {
        // LightBDD has no raw values - only string-based (ToString()) ExampleValues.
        // The renderer uses TryRenderFromParsedString which parses "TypeName { Prop = Val, ... }" patterns.
        var recipeToString = ClassicRecipe.ToString()!;

        var body = new StringBuilder();
        var rendered = ParameterValueRenderer.TryRenderFromParsedString(body, recipeToString);
        var html = body.ToString();

        // Should successfully render
        Assert.True(rendered);

        // Should contain a top-level sub-table
        Assert.Contains("cell-subtable", html);

        // Should contain the property names as headers
        Assert.Contains("<th>Ingredients</th>", html);
        Assert.Contains("<th>Baking</th>", html);
        Assert.Contains("<th>Toppings</th>", html);

        // Nested records (Ingredients, Baking) should each render as nested sub-tables
        Assert.Contains("<th>Flour</th>", html);
        Assert.Contains("<th>Apples</th>", html);
        Assert.Contains("<th>Cinnamon</th>", html);
        Assert.Contains("Plain Flour", html);
        Assert.Contains("Granny Smith", html);
        Assert.Contains("Ceylon", html);

        // Baking has Temperature, DurationMinutes, PanType
        Assert.Contains("<th>Temperature</th>", html);
        Assert.Contains("<th>DurationMinutes</th>", html);
        Assert.Contains("<th>PanType</th>", html);

        // Toppings should NOT show the ugly System.Collections.Generic.List`1[...] name
        Assert.DoesNotContain("System.Collections.Generic", html);
        Assert.DoesNotContain("`1[", html);

        // Should show cleaned type name with mono class
        Assert.Contains("List&lt;ToppingData&gt;", html);
        Assert.Contains("class=\"mono\"", html);
    }

    [Fact]
    public void XUnit3_path_renders_complex_recipe_as_expandable()
    {
        // xUnit3 has raw values, so complex objects go through R4 (expandable) path
        object?[] args = ["Classic", ClassicRecipe, ClassicExpectation];
        string?[] paramNames = ["recipeName", "recipe", "expected"];
        var result = ParameterParser.ExtractStructuredParametersWithRaw(args, paramNames)!;

        var (groups, _) = ParameterGrouper.Analyze([
            new Scenario
            {
                Id = "s1", DisplayName = "Test Classic", OutlineId = "Test",
                ExampleValues = result.Value.StringValues,
                ExampleRawValues = result.Value.RawValues,
                Result = ExecutionResult.Passed
            },
            new Scenario
            {
                Id = "s2", DisplayName = "Test Spiced", OutlineId = "Test",
                ExampleValues = ParameterParser.ExtractStructuredParametersWithRaw(
                    ["Spiced Deluxe", SpicedRecipe, SpicedExpectation], paramNames)!.Value.StringValues,
                ExampleRawValues = ParameterParser.ExtractStructuredParametersWithRaw(
                    ["Spiced Deluxe", SpicedRecipe, SpicedExpectation], paramNames)!.Value.RawValues,
                Result = ExecutionResult.Passed
            }
        ]);

        Assert.Single(groups);
        var group = groups[0];
        Assert.Equal(ParameterDisplayRule.ScalarColumns, group.Rule);
        Assert.Equal(new[] { "recipeName", "recipe", "expected" }, group.ParameterNames);

        // recipe is complex (has List<ToppingData>), so should be classified as complex
        var rawRecipe = group.Scenarios[0].ExampleRawValues!["recipe"];
        Assert.True(ParameterValueRenderer.IsComplexValue(rawRecipe));

        // Render it as expandable (R4)
        var body = new StringBuilder();
        ParameterValueRenderer.RenderExpandable(body, rawRecipe!);
        var html = body.ToString();

        Assert.Contains("param-expand", html);
        Assert.Contains("Toppings", html);
    }

    [Fact]
    public void XUnit3_path_renders_small_expectation_as_subtable()
    {
        // MuffinBatchExpectation has 3 scalar props → should be IsSmallComplexObject
        Assert.True(ParameterValueRenderer.IsSmallComplexObject(ClassicExpectation));

        var body = new StringBuilder();
        ParameterValueRenderer.RenderSubTable(body, ClassicExpectation);
        var html = body.ToString();

        Assert.Contains("cell-subtable", html);
        Assert.Contains("<th>ExpectedIngredientCount</th>", html);
        Assert.Contains("<td>5</td>", html);
        Assert.Contains("<th>ExpectedToppingCount</th>", html);
        Assert.Contains("<td>2</td>", html);
        Assert.Contains("<th>HasBakingInfo</th>", html);
        Assert.Contains("<td>True</td>", html);
    }

    [Fact]
    public void TryCleanCollectionTypeName_cleans_generic_list_type()
    {
        var result = ParameterValueRenderer.TryCleanCollectionTypeName(
            "System.Collections.Generic.List`1[TestTrackingDiagrams.Tests.Reports.MuffinParameterRenderingTests+ToppingData]");
        Assert.Equal("List<ToppingData>", result);
    }

    [Fact]
    public void TryCleanCollectionTypeName_cleans_simple_namespace()
    {
        var result = ParameterValueRenderer.TryCleanCollectionTypeName(
            "System.Collections.Generic.List`1[MyApp.Models.Item]");
        Assert.Equal("List<Item>", result);
    }

    [Fact]
    public void TryCleanCollectionTypeName_returns_null_for_non_collection()
    {
        Assert.Null(ParameterValueRenderer.TryCleanCollectionTypeName("hello world"));
        Assert.Null(ParameterValueRenderer.TryCleanCollectionTypeName("BakingProfileData { Temperature = 180 }"));
        Assert.Null(ParameterValueRenderer.TryCleanCollectionTypeName(""));
    }

    [Fact]
    public void TryCleanCollectionTypeName_handles_non_generic_collection()
    {
        var result = ParameterValueRenderer.TryCleanCollectionTypeName("System.Collections.ArrayList");
        Assert.Equal("ArrayList", result);
    }

    [Fact]
    public void RenderParsedValue_renders_nested_record_as_subtable()
    {
        var body = new StringBuilder();
        ParameterValueRenderer.RenderParsedValue(body, "BakingProfileData { Temperature = 180, DurationMinutes = 25, PanType = Standard }");
        var html = body.ToString();

        Assert.Contains("cell-subtable", html);
        Assert.Contains("<th>Temperature</th>", html);
        Assert.Contains("<td>180</td>", html);
        Assert.Contains("<th>PanType</th>", html);
        Assert.Contains("<td>Standard</td>", html);
    }

    [Fact]
    public void RenderParsedValue_renders_collection_type_as_mono_span()
    {
        var body = new StringBuilder();
        ParameterValueRenderer.RenderParsedValue(body, "System.Collections.Generic.List`1[MyApp.ToppingData]");
        var html = body.ToString();

        Assert.Contains("class=\"mono\"", html);
        Assert.Contains("List&lt;ToppingData&gt;", html);
        Assert.DoesNotContain("System.Collections", html);
    }

    [Fact]
    public void RenderParsedValue_renders_scalar_as_plain_text()
    {
        var body = new StringBuilder();
        ParameterValueRenderer.RenderParsedValue(body, "Plain Flour");
        Assert.Equal("Plain Flour", body.ToString());
    }

    [Fact]
    public void RenderParsedValue_html_encodes_special_characters()
    {
        var body = new StringBuilder();
        ParameterValueRenderer.RenderParsedValue(body, "<script>alert('xss')</script>");
        Assert.DoesNotContain("<script>", body.ToString());
        Assert.Contains("&lt;script&gt;", body.ToString());
    }
}
