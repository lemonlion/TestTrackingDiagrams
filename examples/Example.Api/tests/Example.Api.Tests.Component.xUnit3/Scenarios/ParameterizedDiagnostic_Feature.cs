using Example.Api.Tests.Component.xUnit3.Infrastructure;
using FluentAssertions;
using TestTrackingDiagrams.xUnit3;

namespace Example.Api.Tests.Component.xUnit3.Scenarios;

public record IngredientSet(string Flour, string Apples, string Cinnamon);
public record BakingProfileData(int Temperature, int DurationMinutes, string PanType);
public record ToppingData(string Name, string Amount);
public record MuffinRecipeTestData(IngredientSet Ingredients, BakingProfileData Baking, List<ToppingData> Toppings);
public record MuffinBatchExpectation(int ExpectedIngredientCount, int ExpectedToppingCount, bool HasBakingInfo);

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
                [new ToppingData("Streusel", "Light"), new ToppingData("Icing Glaze", "Drizzle")]),
            new MuffinBatchExpectation(5, 2, true)
        ];
        yield return
        [
            "Rustic Wholesome",
            new MuffinRecipeTestData(
                new IngredientSet("Whole Wheat", "Honeycrisp", "Cassia"),
                new BakingProfileData(175, 30, "Cast Iron"),
                [new ToppingData("Brown Sugar Crumb", "Heavy"), new ToppingData("Maple Drizzle", "Light")]),
            new MuffinBatchExpectation(5, 2, true)
        ];
        yield return
        [
            "Spiced Deluxe",
            new MuffinRecipeTestData(
                new IngredientSet("Almond Flour", "Pink Lady", "Saigon"),
                new BakingProfileData(190, 20, "Silicone"),
                [new ToppingData("Cinnamon Sugar", "Heavy"), new ToppingData("Cream Cheese Swirl", "Thick")]),
            new MuffinBatchExpectation(5, 2, true)
        ];
    }

    [Theory]
    [MemberData(nameof(RecipeTestData))]
    public void Different_muffin_recipes_should_produce_the_expected_batch(
        string recipeName, MuffinRecipeTestData recipe, MuffinBatchExpectation expected)
    {
        recipeName.Should().NotBeNullOrEmpty();
        recipe.Should().NotBeNull();
        recipe.Ingredients.Should().NotBeNull();
        recipe.Baking.Should().NotBeNull();
        recipe.Baking.Temperature.Should().BeGreaterThan(0);
        recipe.Toppings.Should().HaveCount(expected.ExpectedToppingCount);
        expected.HasBakingInfo.Should().BeTrue();
    }
}
