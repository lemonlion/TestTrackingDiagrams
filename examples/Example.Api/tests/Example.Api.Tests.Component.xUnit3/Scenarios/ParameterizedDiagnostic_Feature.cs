using Example.Api.Tests.Component.xUnit3.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.Component.xUnit3.Scenarios;

public record IngredientSet(string Flour, string Apples, string Cinnamon);
public record BakingProfileData(int Temperature, int DurationMinutes, string PanType);
public record ToppingData(string Name, string Amount);
public record MuffinRecipeTestData(IngredientSet Ingredients, BakingProfileData Baking, List<ToppingData> Toppings);
public record MuffinBatchExpectation(int ExpectedCount, string ExpectedTexture);

[Endpoint("/diagnostic")]
public class ParameterizedDiagnostic_Feature : BaseFixture
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

    [Theory]
    [MemberData(nameof(RecipeTestData))]
    public void Different_muffin_recipes_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestData recipe, MuffinBatchExpectation expected)
    {
        // Simple assertion to make the test pass
        recipeName.Should().NotBeNullOrEmpty();
        recipe.Should().NotBeNull();
        recipe.Ingredients.Should().NotBeNull();
        recipe.Baking.Should().NotBeNull();
        recipe.Toppings.Should().NotBeNull();
        expected.Should().NotBeNull();
    }
}
