using Example.Api.Tests.Component.LightBDD.xUnit3.Infrastructure;
using FluentAssertions;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace Example.Api.Tests.Component.LightBDD.xUnit3.Scenarios;

public record IngredientSet(string Flour, string Apples, string Cinnamon);
public record BakingProfileData(int Temperature, int DurationMinutes, string PanType);
public record ToppingData(string Name, string Amount);
public record MuffinRecipeTestData(IngredientSet Ingredients, BakingProfileData Baking, List<ToppingData> Toppings);
public record MuffinBatchExpectation(int ExpectedCount, string ExpectedTexture);

[FeatureDescription("Parameterized Diagnostic")]
public partial class ParameterizedDiagnostic_Feature : BaseFixture
{
    public static IEnumerable<object[]> RecipeTestData()
    {
        yield return
        [
            "Classic",
            new MuffinRecipeTestData(
                new IngredientSet("Plain Flour", "Granny Smith", "Ceylon"),
                new BakingProfileData(180, 25, "Standard"),
                [new ToppingData("Streusel", "Light"), new ToppingData("Icing Sugar", "Dusting")]),
            new MuffinBatchExpectation(12, "Fluffy")
        ];
        yield return
        [
            "Healthy",
            new MuffinRecipeTestData(
                new IngredientSet("Whole Wheat", "Honeycrisp", "Cassia"),
                new BakingProfileData(170, 30, "Silicone"),
                [new ToppingData("Oats", "Heavy")]),
            new MuffinBatchExpectation(6, "Dense")
        ];
    }

    [Scenario]
    [MemberData(nameof(RecipeTestData))]
    public async Task Different_muffin_recipes_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestData recipe, MuffinBatchExpectation expected)
    {
        await Runner.RunScenarioAsync(
            _ => The_recipe_name_is_valid(recipeName),
            _ => The_recipe_data_is_complete(recipe),
            _ => The_expected_batch_is_specified(expected));
    }

    private async Task The_recipe_name_is_valid(string recipeName)
    {
        recipeName.Should().NotBeNullOrEmpty();
    }

    private async Task The_recipe_data_is_complete(MuffinRecipeTestData recipe)
    {
        recipe.Should().NotBeNull();
        recipe.Ingredients.Should().NotBeNull();
        recipe.Baking.Should().NotBeNull();
        recipe.Toppings.Should().NotBeNull();
    }

    private async Task The_expected_batch_is_specified(MuffinBatchExpectation expected)
    {
        expected.Should().NotBeNull();
        expected.ExpectedCount.Should().BeGreaterThan(0);
    }
}
