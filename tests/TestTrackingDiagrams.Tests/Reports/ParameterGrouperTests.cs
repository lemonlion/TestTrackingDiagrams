using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class ParameterGrouperTests
{
    [Fact]
    public void Single_scenario_no_params_returns_ungrouped()
    {
        var scenario = MakeScenario("s1", "SomeTest");
        var (groups, ungrouped) = ParameterGrouper.Analyze([scenario]);
        Assert.Empty(groups);
        Assert.Single(ungrouped);
    }

    [Fact]
    public void Two_scenarios_with_same_OutlineId_returns_one_group()
    {
        var s1 = MakeScenario("s1", "Test (UK)", outlineId: "Test", exampleValues: new() { ["region"] = "UK" });
        var s2 = MakeScenario("s2", "Test (US)", outlineId: "Test", exampleValues: new() { ["region"] = "US" });
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
        Assert.Equal("Test", groups[0].GroupDisplayName);
        Assert.Equal(2, groups[0].Scenarios.Length);
        Assert.Equal(ParameterDisplayRule.ScalarColumns, groups[0].Rule);
        Assert.Equal(["region"], groups[0].ParameterNames);
    }

    [Fact]
    public void Three_scenarios_grouped_by_display_name_prefix()
    {
        var s1 = MakeScenario("s1", "Ns.Class.Method(a: 1)");
        var s2 = MakeScenario("s2", "Ns.Class.Method(a: 2)");
        var s3 = MakeScenario("s3", "Ns.Class.Method(a: 3)");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2, s3]);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
        Assert.Equal(3, groups[0].Scenarios.Length);
    }

    [Fact]
    public void Group_with_ExampleDisplayName_uses_fallback_rule()
    {
        var s1 = MakeScenario("s1", "Happy path UK", outlineId: "Test", exampleDisplayName: "Happy path UK");
        var s2 = MakeScenario("s2", "Sad path US", outlineId: "Test", exampleDisplayName: "Sad path US");
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        Assert.Equal(ParameterDisplayRule.Fallback, groups[0].Rule);
    }

    [Fact]
    public void Mixed_grouped_and_ungrouped()
    {
        var s1 = MakeScenario("s1", "Ns.Class.Method(a: 1)");
        var s2 = MakeScenario("s2", "Ns.Class.Method(a: 2)");
        var s3 = MakeScenario("s3", "StandaloneTest");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2, s3]);
        Assert.Single(groups);
        Assert.Single(ungrouped);
        Assert.Equal("StandaloneTest", ungrouped[0].DisplayName);
    }

    [Fact]
    public void Single_member_OutlineId_with_params_treated_as_group()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" });
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1]);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
    }

    [Fact]
    public void Single_member_OutlineId_without_params_treated_as_ungrouped()
    {
        var s1 = MakeScenario("s1", "Test variant A", outlineId: "Test");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1]);
        Assert.Empty(groups);
        Assert.Single(ungrouped);
    }

    [Fact]
    public void Single_member_display_name_with_params_treated_as_group()
    {
        var s1 = MakeScenario("s1", "Calculate(x: 10, y: 20)");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1]);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
    }

    [Fact]
    public void Different_classes_same_method_name_each_become_single_member_groups()
    {
        var s1 = MakeScenario("s1", "Ns.ClassA.Method(a: 1)");
        var s2 = MakeScenario("s2", "Ns.ClassB.Method(a: 2)");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2]);
        // Different prefixes means they don't merge, but each has params so each becomes its own group
        Assert.Equal(2, groups.Length);
        Assert.Empty(ungrouped);
    }

    [Fact]
    public void Identical_diagrams_not_detected_by_default_when_no_diagram_comparer()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" });
        var s2 = MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" });
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.False(groups[0].AllDiagramsIdentical);
    }

    [Fact]
    public void Identical_diagrams_detected_when_comparer_says_identical()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" });
        var s2 = MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" });
        var (groups, _) = ParameterGrouper.Analyze([s1, s2], diagramComparer: _ => true);
        Assert.True(groups[0].AllDiagramsIdentical);
    }

    [Fact]
    public void Non_identical_diagrams_when_comparer_says_different()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" });
        var s2 = MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" });
        var (groups, _) = ParameterGrouper.Analyze([s1, s2], diagramComparer: _ => false);
        Assert.False(groups[0].AllDiagramsIdentical);
    }

    [Fact]
    public void Grouping_disabled_still_groups_by_OutlineId()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)", outlineId: "Test", exampleValues: new() { ["a"] = "1" });
        var s2 = MakeScenario("s2", "Test(a: 2)", outlineId: "Test", exampleValues: new() { ["a"] = "2" });
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2], enabled: false);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
    }

    [Fact]
    public void Grouping_disabled_does_not_group_by_display_name_prefix()
    {
        var s1 = MakeScenario("s1", "Test(a: 1)");
        var s2 = MakeScenario("s2", "Test(a: 2)");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2], enabled: false);
        Assert.Empty(groups);
        Assert.Equal(2, ungrouped.Length);
    }

    [Fact]
    public void Extracts_parameter_names_from_ExampleValues_keys()
    {
        var s1 = MakeScenario("s1", "Test(a: 1, b: 2)", outlineId: "Test",
            exampleValues: new() { ["region"] = "UK", ["amount"] = "100" });
        var s2 = MakeScenario("s2", "Test(a: 3, b: 4)", outlineId: "Test",
            exampleValues: new() { ["region"] = "US", ["amount"] = "200" });
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Contains("region", groups[0].ParameterNames);
        Assert.Contains("amount", groups[0].ParameterNames);
    }

    [Fact]
    public void Falls_back_to_parsed_params_when_no_ExampleValues()
    {
        var s1 = MakeScenario("s1", "Ns.Class.Method(region: \"UK\", amount: 100)");
        var s2 = MakeScenario("s2", "Ns.Class.Method(region: \"US\", amount: 200)");
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        Assert.Contains("region", groups[0].ParameterNames);
        Assert.Contains("amount", groups[0].ParameterNames);
    }

    [Fact]
    public void OutlineId_groups_take_precedence_over_display_name_grouping()
    {
        // s1+s2 share OutlineId "GroupA", s3 has different OutlineId "GroupB" (single member with params → still a group)
        var s1 = MakeScenario("s1", "Test variant A", outlineId: "GroupA", exampleValues: new() { ["x"] = "1" });
        var s2 = MakeScenario("s2", "Test variant B", outlineId: "GroupA", exampleValues: new() { ["x"] = "2" });
        var s3 = MakeScenario("s3", "Test variant C", outlineId: "GroupB", exampleValues: new() { ["x"] = "3" });
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1, s2, s3]);
        Assert.Equal(2, groups.Length); // GroupA (2 members) and GroupB (1 member with params)
        Assert.Empty(ungrouped);
    }

    [Fact]
    public void Bracketed_display_names_grouped_correctly()
    {
        var s1 = MakeScenario("s1", "Scenario name [count: 5]");
        var s2 = MakeScenario("s2", "Scenario name [count: 10]");
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        Assert.Equal("Scenario name", groups[0].GroupDisplayName);
    }

    [Fact]
    public void Multiple_separate_brackets_grouped_correctly()
    {
        var s1 = MakeScenario("s1", "Scenario name [version: \"V1\"] [claimName: \"LivePersonSdes\"]");
        var s2 = MakeScenario("s2", "Scenario name [version: \"V2\"] [claimName: \"OtherClaim\"]");
        var (groups, _) = ParameterGrouper.Analyze([s1, s2]);
        Assert.Single(groups);
        Assert.Equal("Scenario name", groups[0].GroupDisplayName);
        Assert.Contains("version", groups[0].ParameterNames);
        Assert.Contains("claimName", groups[0].ParameterNames);
    }

    [Fact]
    public void Single_scenario_with_multiple_brackets_becomes_group()
    {
        var s1 = MakeScenario("s1", "Scenario name [version: \"V1\"] [claimName: \"LivePersonSdes\"]");
        var (groups, ungrouped) = ParameterGrouper.Analyze([s1]);
        Assert.Single(groups);
        Assert.Empty(ungrouped);
        Assert.Equal("Scenario name", groups[0].GroupDisplayName);
        Assert.Contains("version", groups[0].ParameterNames);
        Assert.Contains("claimName", groups[0].ParameterNames);
    }

    [Fact]
    public void Max_columns_exceeded_uses_fallback()
    {
        var vals = new Dictionary<string, string>();
        for (var i = 0; i < 12; i++) vals[$"col{i}"] = $"val{i}";
        var s1 = MakeScenario("s1", "Test(many params)", outlineId: "Test", exampleValues: new(vals));
        vals = new Dictionary<string, string>();
        for (var i = 0; i < 12; i++) vals[$"col{i}"] = $"val{i + 10}";
        var s2 = MakeScenario("s2", "Test(many params)", outlineId: "Test", exampleValues: new(vals));
        var (groups, _) = ParameterGrouper.Analyze([s1, s2], maxColumns: 10);
        Assert.Single(groups);
        Assert.Equal(ParameterDisplayRule.Fallback, groups[0].Rule);
    }

    private static Scenario MakeScenario(
        string id, string displayName,
        string? outlineId = null,
        Dictionary<string, string>? exampleValues = null,
        string? exampleDisplayName = null,
        ExecutionResult result = ExecutionResult.Passed)
    {
        return new Scenario
        {
            Id = id,
            DisplayName = displayName,
            OutlineId = outlineId,
            ExampleValues = exampleValues,
            ExampleDisplayName = exampleDisplayName,
            Result = result
        };
    }
}
