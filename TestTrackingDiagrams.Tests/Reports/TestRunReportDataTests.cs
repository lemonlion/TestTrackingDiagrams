using System.Text.Json;
using System.Xml.Linq;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class TestRunReportDataTests
{
    [Fact]
    public void GenerateTestRunReportData_produces_json_by_default()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Place order", Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2) },
                    new Scenario { Id = "s2", DisplayName = "Cancel order", Result = ExecutionResult.Failed, ErrorMessage = "timeout", Duration = TimeSpan.FromSeconds(1) }
                ]
            }
        };

        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_json.json", DataFormat.Json);
        var content = File.ReadAllText(path);

        var doc = JsonDocument.Parse(content);
        var root = doc.RootElement;

        Assert.Equal("2026-01-01T10:00:00Z", root.GetProperty("startTime").GetString());
        Assert.Equal("2026-01-01T10:05:00Z", root.GetProperty("endTime").GetString());
        Assert.Equal(1, root.GetProperty("features").GetArrayLength());

        var feature = root.GetProperty("features")[0];
        Assert.Equal("Orders", feature.GetProperty("name").GetString());
        Assert.Equal(2, feature.GetProperty("scenarios").GetArrayLength());

        var s1 = feature.GetProperty("scenarios")[0];
        Assert.Equal("Place order", s1.GetProperty("name").GetString());
        Assert.Equal("Passed", s1.GetProperty("result").GetString());
        Assert.Equal(2.0, s1.GetProperty("durationSeconds").GetDouble());

        var s2 = feature.GetProperty("scenarios")[1];
        Assert.Equal("Failed", s2.GetProperty("result").GetString());
        Assert.Equal("timeout", s2.GetProperty("errorMessage").GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_produces_xml()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_xml.xml", DataFormat.Xml);
        var content = File.ReadAllText(path);

        var doc = XDocument.Parse(content);
        Assert.Equal("TestRunReport", doc.Root!.Name.LocalName);
        Assert.Equal("2026-01-01T10:00:00Z", doc.Root.Element("StartTime")!.Value);
        Assert.NotNull(doc.Root.Element("Features"));
    }

    [Fact]
    public void GenerateTestRunReportData_produces_yaml()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var start = new DateTime(2026, 1, 1, 10, 0, 0, DateTimeKind.Utc);
        var end = new DateTime(2026, 1, 1, 10, 5, 0, DateTimeKind.Utc);

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_yaml.yml", DataFormat.Yaml);
        var content = File.ReadAllText(path);

        Assert.Contains("StartTime:", content);
        Assert.Contains("EndTime:", content);
        Assert.Contains("Features:", content);
        Assert.Contains("Orders", content);
        Assert.Contains("Place order", content);
        Assert.Contains("Passed", content);
    }

    [Fact]
    public void GenerateTestRunReportData_includes_steps_with_status()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Passed,
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "a thing", Status = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(100) },
                            new ScenarioStep { Keyword = "When", Text = "action", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "Then", Text = "result", Status = ExecutionResult.Failed }
                        ]
                    }
                ]
            }
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_steps.json", DataFormat.Json);
        var content = File.ReadAllText(path);

        var doc = JsonDocument.Parse(content);
        var steps = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("steps");
        Assert.Equal(3, steps.GetArrayLength());
        Assert.Equal("Given", steps[0].GetProperty("keyword").GetString());
        Assert.Equal("a thing", steps[0].GetProperty("text").GetString());
        Assert.Equal("Passed", steps[0].GetProperty("status").GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_includes_labels_categories_and_error_stack_trace()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Labels = ["api"],
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S1", Result = ExecutionResult.Failed,
                        ErrorMessage = "NullRef", ErrorStackTrace = "at Foo.Bar()",
                        Labels = ["smoke"], Categories = ["Integration"],
                        IsHappyPath = true
                    }
                ]
            }
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_meta.json", DataFormat.Json);
        var content = File.ReadAllText(path);

        var doc = JsonDocument.Parse(content);
        var feature = doc.RootElement.GetProperty("features")[0];
        Assert.Equal("api", feature.GetProperty("labels")[0].GetString());

        var scenario = feature.GetProperty("scenarios")[0];
        Assert.Equal("smoke", scenario.GetProperty("labels")[0].GetString());
        Assert.Equal("Integration", scenario.GetProperty("categories")[0].GetString());
        Assert.True(scenario.GetProperty("isHappyPath").GetBoolean());
        Assert.Equal("NullRef", scenario.GetProperty("errorMessage").GetString());
        Assert.Equal("at Foo.Bar()", scenario.GetProperty("errorStackTrace").GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_includes_substeps()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "s1", DisplayName = "S1",
                        Steps =
                        [
                            new ScenarioStep
                            {
                                Keyword = "Given", Text = "parent", Status = ExecutionResult.Passed,
                                SubSteps =
                                [
                                    new ScenarioStep { Keyword = "And", Text = "child1", Status = ExecutionResult.Passed },
                                    new ScenarioStep { Keyword = "And", Text = "child2", Status = ExecutionResult.Failed }
                                ]
                            }
                        ]
                    }
                ]
            }
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_sub.json", DataFormat.Json);
        var content = File.ReadAllText(path);

        var doc = JsonDocument.Parse(content);
        var step = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("steps")[0];
        var subSteps = step.GetProperty("subSteps");
        Assert.Equal(2, subSteps.GetArrayLength());
        Assert.Equal("child1", subSteps[0].GetProperty("text").GetString());
    }
}
