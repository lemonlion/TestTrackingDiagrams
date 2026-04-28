using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ScenarioStableIdTests
{
    [Fact]
    public void Same_feature_and_scenario_produce_same_id()
    {
        var id1 = ScenarioStableId.Compute("Orders", "Place order");
        var id2 = ScenarioStableId.Compute("Orders", "Place order");
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Different_scenario_names_produce_different_ids()
    {
        var id1 = ScenarioStableId.Compute("Orders", "Place order");
        var id2 = ScenarioStableId.Compute("Orders", "Cancel order");
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Different_feature_names_produce_different_ids()
    {
        var id1 = ScenarioStableId.Compute("Orders", "Place order");
        var id2 = ScenarioStableId.Compute("Payments", "Place order");
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Returns_16_character_lowercase_hex_string()
    {
        var id = ScenarioStableId.Compute("Orders", "Place order");
        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }

    [Fact]
    public void Parameterized_scenarios_with_same_outlineId_but_different_display_names_produce_different_ids()
    {
        var id1 = ScenarioStableId.Compute("Orders", "Place order (visa)", outlineId: "Place order");
        var id2 = ScenarioStableId.Compute("Orders", "Place order (mastercard)", outlineId: "Place order");
        Assert.NotEqual(id1, id2);
    }

    [Fact]
    public void Null_outlineId_same_as_no_outlineId()
    {
        var id1 = ScenarioStableId.Compute("Orders", "Place order");
        var id2 = ScenarioStableId.Compute("Orders", "Place order", outlineId: null);
        Assert.Equal(id1, id2);
    }

    [Fact]
    public void Id_is_deterministic_across_calls()
    {
        var ids = Enumerable.Range(0, 100)
            .Select(_ => ScenarioStableId.Compute("Feature A", "Scenario B"))
            .Distinct()
            .ToArray();
        Assert.Single(ids);
    }

    [Fact]
    public void Handles_special_characters_in_names()
    {
        var id = ScenarioStableId.Compute("Feature: <special> & \"chars\"", "Scenario with 'quotes' & stuff");
        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }

    [Fact]
    public void Handles_unicode_names()
    {
        var id = ScenarioStableId.Compute("注文機能", "注文を確定する");
        Assert.Equal(16, id.Length);
        Assert.Matches("^[0-9a-f]{16}$", id);
    }
}
