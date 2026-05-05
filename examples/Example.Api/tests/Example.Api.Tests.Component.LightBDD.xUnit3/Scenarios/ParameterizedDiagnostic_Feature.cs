using Example.Api.Tests.Component.LightBDD.xUnit3.Infrastructure;
using FluentAssertions;
using LightBDD.Framework;
using LightBDD.Framework.Scenarios;
using LightBDD.XUnit3;

namespace Example.Api.Tests.Component.LightBDD.xUnit3.Scenarios;

public class IngredientSet
{
    public required string Flour { get; init; }
    public required string Apples { get; init; }
    public required string Cinnamon { get; init; }
}

public class BakingProfileData
{
    public required int Temperature { get; init; }
    public required int DurationMinutes { get; init; }
    public required string PanType { get; init; }
}

public class ToppingData
{
    public required string Name { get; init; }
    public required string Amount { get; init; }
}

public class MuffinRecipeTestData
{
    public required IngredientSet Ingredients { get; init; }
    public required BakingProfileData Baking { get; init; }
    public required List<ToppingData> Toppings { get; init; }
}

public class MuffinBatchExpectation
{
    public required int ExpectedIngredientCount { get; init; }
    public required int ExpectedToppingCount { get; init; }
    public required bool HasBakingInfo { get; init; }
}

[FeatureDescription("Parameterized Diagnostic")]
public partial class ParameterizedDiagnostic_Feature : BaseFixture
{
    public static IEnumerable<object[]> RecipeTestData()
    {
        yield return
        [
            "Classic",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Plain Flour",
                    Apples = "Granny Smith",
                    Cinnamon = "Ceylon"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 180,
                    DurationMinutes = 25,
                    PanType = "Standard"
                },
                Toppings =
                [
                    new ToppingData { Name = "Streusel", Amount = "Light" },
                    new ToppingData { Name = "Icing Glaze", Amount = "Drizzle" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        ];
        yield return
        [
            "Rustic Wholesome",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Whole Wheat",
                    Apples = "Honeycrisp",
                    Cinnamon = "Cassia"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 175,
                    DurationMinutes = 30,
                    PanType = "Cast Iron"
                },
                Toppings =
                [
                    new ToppingData { Name = "Brown Sugar Crumb", Amount = "Heavy" },
                    new ToppingData { Name = "Maple Drizzle", Amount = "Light" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
        ];
        yield return
        [
            "Spiced Deluxe",
            new MuffinRecipeTestData
            {
                Ingredients = new IngredientSet
                {
                    Flour = "Almond Flour",
                    Apples = "Pink Lady",
                    Cinnamon = "Saigon"
                },
                Baking = new BakingProfileData
                {
                    Temperature = 190,
                    DurationMinutes = 20,
                    PanType = "Silicone"
                },
                Toppings =
                [
                    new ToppingData { Name = "Cinnamon Sugar", Amount = "Heavy" },
                    new ToppingData { Name = "Cream Cheese Swirl", Amount = "Thick" }
                ]
            },
            new MuffinBatchExpectation
            {
                ExpectedIngredientCount = 5,
                ExpectedToppingCount = 2,
                HasBakingInfo = true
            }
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
        expected.ExpectedIngredientCount.Should().BeGreaterThan(0);
    }
}
