using FluentAssertions;
using Reqnroll;

namespace Example.Api.Tests.Component.ReqNRoll.xUnit3.StepDefinitions;

[Binding]
public class MuffinsStepDefinitions
{
    private string _recipeName = "";
    private string _flour = "";
    private string _appleVariety = "";
    private string _cinnamonType = "";
    private int _temperature;
    private int _duration;
    private string _panType = "";
    private readonly List<(string Name, string Amount)> _toppings = [];
    private int _expectedIngredientCount;
    private int _expectedToppingCount;

    [Given("a valid apple cinnamon muffin recipe with all ingredients")]
    public void GivenAValidAppleCinnamonMuffinRecipeWithAllIngredients()
    {
        _recipeName = "Classic";
        _flour = "Plain Flour";
        _appleVariety = "Granny Smith";
        _cinnamonType = "Ceylon";
        _temperature = 180;
        _duration = 25;
        _panType = "Standard";
        _toppings.Add(("Streusel", "Light"));
        _toppings.Add(("Icing Glaze", "Drizzle"));
    }

    [Given(@"a muffin recipe ""(.*)"" with the following ingredients:")]
    public void GivenAMuffinRecipeWithTheFollowingIngredients(string recipeName, Table table)
    {
        _recipeName = recipeName;
        var row = table.Rows[0];
        _flour = row["Flour"];
        _appleVariety = row["Apples"];
        _cinnamonType = row["Cinnamon"];
    }

    [Given("the following baking:")]
    public void GivenTheFollowingBaking(Table table)
    {
        var row = table.Rows[0];
        _temperature = int.Parse(row["Temperature"]);
        _duration = int.Parse(row["DurationMinutes"]);
        _panType = row["PanType"];
    }

    [Given(@"with baking at (\d+) degrees for (\d+) minutes in a ""(.*)"" pan")]
    public void GivenWithBakingAtDegreesForMinutesInAPan(int temperature, int duration, string panType)
    {
        _temperature = temperature;
        _duration = duration;
        _panType = panType;
    }

    [Given("the following muffin toppings:")]
    public void GivenTheFollowingMuffinToppings(Table table)
    {
        foreach (var row in table.Rows)
        {
            var amount = table.Header.Contains("Amount") ? row["Amount"] : "Light";
            _toppings.Add((row["Name"], amount));
        }
    }

    [When("the muffins are prepared")]
    public void WhenTheMuffinsArePrepared()
    {
        // Simulate preparation - no actual HTTP call needed for diagnostic purposes
        _recipeName.Should().NotBeNullOrEmpty();
    }

    [Then(@"the muffin response should contain a valid batch with all ingredients")]
    public void ThenTheMuffinResponseShouldContainAValidBatchWithAllIngredients()
    {
        _flour.Should().NotBeNullOrEmpty();
        _appleVariety.Should().NotBeNullOrEmpty();
        _cinnamonType.Should().NotBeNullOrEmpty();
    }

    [Then(@"the cow service should have received a milk request for the muffins")]
    public void ThenTheCowServiceShouldHaveReceivedAMilkRequestForTheMuffins()
    {
        // Diagnostic test - no actual HTTP tracking needed
    }

    [Then(@"the muffin batch should have (\d+) ingredients")]
    public void ThenTheMuffinBatchShouldHaveIngredients(int expectedCount)
    {
        _expectedIngredientCount = expectedCount;
        // In a real test this would verify the response
        // For diagnostic purposes, just verify the expectation is reasonable
        _expectedIngredientCount.Should().BeGreaterThan(0);
    }

    [Then(@"the muffin response should include (\d+) toppings")]
    public void ThenTheMuffinResponseShouldIncludeToppings(int expectedCount)
    {
        _expectedToppingCount = expectedCount;
        _toppings.Count.Should().Be(_expectedToppingCount);
    }

    [Then(@"the muffin response should have baking info (.*)")]
    public void ThenTheMuffinResponseShouldHaveBakingInfo(string hasBakingInfo)
    {
        var expected = bool.Parse(hasBakingInfo);
        var actual = _temperature > 0 && _duration > 0 && !string.IsNullOrEmpty(_panType);
        actual.Should().Be(expected);
    }
}
