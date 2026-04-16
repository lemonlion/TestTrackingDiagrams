using System.Net;
using System.Text.Json;
using System.Xml.Linq;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

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

    [Fact]
    public void GenerateTestRunReportData_includes_plantuml_diagrams_per_scenario()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed },
                    new Scenario { Id = "test-2", DisplayName = "Cancel order", Result = ExecutionResult.Failed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("test-1", "img-src-1", "@startuml\nAlice -> Bob : place order\n@enduml"),
            new DiagramAsCode("test-2", "img-src-2", "@startuml\nAlice -> Bob : cancel order\n@enduml")
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_diagrams.json", DataFormat.Json, diagrams);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var s1 = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0];
        var s1Diagrams = s1.GetProperty("diagrams");
        Assert.Equal(1, s1Diagrams.GetArrayLength());
        Assert.Equal("@startuml\nAlice -> Bob : place order\n@enduml", s1Diagrams[0].GetString());

        var s2 = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[1];
        var s2Diagrams = s2.GetProperty("diagrams");
        Assert.Equal(1, s2Diagrams.GetArrayLength());
        Assert.Equal("@startuml\nAlice -> Bob : cancel order\n@enduml", s2Diagrams[0].GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_includes_multiple_diagrams_for_same_scenario()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("test-1", "img1", "@startuml\ndiagram-1\n@enduml"),
            new DiagramAsCode("test-1", "img2", "@startuml\ndiagram-2\n@enduml")
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_multi_diagrams.json", DataFormat.Json, diagrams);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var scenarioDiagrams = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("diagrams");
        Assert.Equal(2, scenarioDiagrams.GetArrayLength());
        Assert.Equal("@startuml\ndiagram-1\n@enduml", scenarioDiagrams[0].GetString());
        Assert.Equal("@startuml\ndiagram-2\n@enduml", scenarioDiagrams[1].GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_includes_empty_diagrams_when_no_match()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("other-test", "img", "@startuml\nother\n@enduml")
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_no_match.json", DataFormat.Json, diagrams);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var scenarioDiagrams = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("diagrams");
        Assert.Equal(0, scenarioDiagrams.GetArrayLength());
    }

    [Fact]
    public void GenerateTestRunReportData_includes_http_interactions_per_scenario()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var traceId = Guid.NewGuid();
        var reqId = Guid.NewGuid();
        var timestamp = DateTimeOffset.UtcNow;

        var logs = new[]
        {
            new RequestResponseLog(
                TestName: "Place order",
                TestId: "test-1",
                Method: HttpMethod.Post,
                Content: "{\"item\":\"widget\"}",
                Uri: new Uri("https://api.example.com/orders"),
                Headers: [("Content-Type", "application/json")],
                ServiceName: "OrderService",
                CallerName: "TestClient",
                Type: RequestResponseType.Request,
                TraceId: traceId,
                RequestResponseId: reqId,
                TrackingIgnore: false)
            { Timestamp = timestamp },
            new RequestResponseLog(
                TestName: "Place order",
                TestId: "test-1",
                Method: HttpMethod.Post,
                Content: "{\"id\":1}",
                Uri: new Uri("https://api.example.com/orders"),
                Headers: [("Content-Type", "application/json")],
                ServiceName: "OrderService",
                CallerName: "TestClient",
                Type: RequestResponseType.Response,
                TraceId: traceId,
                RequestResponseId: reqId,
                TrackingIgnore: false,
                StatusCode: HttpStatusCode.Created)
            { Timestamp = timestamp.AddMilliseconds(50) }
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_http.json", DataFormat.Json, trackedLogs: logs);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var interactions = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("httpInteractions");
        Assert.Equal(2, interactions.GetArrayLength());

        var request = interactions[0];
        Assert.Equal("Request", request.GetProperty("type").GetString());
        Assert.Equal("POST", request.GetProperty("method").GetString());
        Assert.Equal("https://api.example.com/orders", request.GetProperty("uri").GetString());
        Assert.Equal("OrderService", request.GetProperty("serviceName").GetString());
        Assert.Equal("TestClient", request.GetProperty("callerName").GetString());
        Assert.Equal("{\"item\":\"widget\"}", request.GetProperty("content").GetString());

        var response = interactions[1];
        Assert.Equal("Response", response.GetProperty("type").GetString());
        Assert.Equal("Created", response.GetProperty("statusCode").GetString());
        Assert.Equal("{\"id\":1}", response.GetProperty("content").GetString());
    }

    [Fact]
    public void GenerateTestRunReportData_empty_interactions_when_no_matching_logs()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var logs = new[]
        {
            new RequestResponseLog(
                TestName: "Other test",
                TestId: "other-test",
                Method: HttpMethod.Get,
                Content: null,
                Uri: new Uri("https://api.example.com/other"),
                Headers: [],
                ServiceName: "Svc",
                CallerName: "Caller",
                Type: RequestResponseType.Request,
                TraceId: Guid.NewGuid(),
                RequestResponseId: Guid.NewGuid(),
                TrackingIgnore: false)
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_no_http.json", DataFormat.Json, trackedLogs: logs);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var interactions = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("httpInteractions");
        Assert.Equal(0, interactions.GetArrayLength());
    }

    [Fact]
    public void GenerateTestRunReportData_diagrams_and_interactions_null_when_not_provided()
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

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_nodiag.json", DataFormat.Json);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var scenario = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0];

        // When no diagrams/logs provided, the properties should not be present
        Assert.False(scenario.TryGetProperty("diagrams", out _));
        Assert.False(scenario.TryGetProperty("httpInteractions", out _));
    }

    [Fact]
    public void GenerateTestRunReportData_xml_includes_diagrams_and_interactions()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("test-1", "img", "@startuml\nAlice -> Bob\n@enduml")
        };

        var traceId = Guid.NewGuid();
        var logs = new[]
        {
            new RequestResponseLog(
                TestName: "Place order",
                TestId: "test-1",
                Method: HttpMethod.Get,
                Content: null,
                Uri: new Uri("https://api.example.com/orders"),
                Headers: [],
                ServiceName: "OrderService",
                CallerName: "TestClient",
                Type: RequestResponseType.Request,
                TraceId: traceId,
                RequestResponseId: Guid.NewGuid(),
                TrackingIgnore: false)
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_xml_diag.xml", DataFormat.Xml, diagrams, logs);
        var content = File.ReadAllText(path);
        var doc = XDocument.Parse(content);

        var scenario = doc.Root!.Element("Features")!.Element("Feature")!.Element("Scenarios")!.Element("Scenario")!;

        var diagramsEl = scenario.Element("Diagrams");
        Assert.NotNull(diagramsEl);
        Assert.Equal("@startuml\nAlice -> Bob\n@enduml", diagramsEl!.Element("Diagram")!.Value);

        var interactionsEl = scenario.Element("HttpInteractions");
        Assert.NotNull(interactionsEl);
        var interaction = interactionsEl!.Element("HttpInteraction")!;
        Assert.Equal("Request", interaction.Element("Type")!.Value);
        Assert.Equal("GET", interaction.Element("Method")!.Value);
        Assert.Equal("OrderService", interaction.Element("ServiceName")!.Value);
    }

    [Fact]
    public void GenerateTestRunReportData_yaml_includes_diagrams_and_interactions()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Orders",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "Place order", Result = ExecutionResult.Passed }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("test-1", "img", "@startuml\nAlice -> Bob\n@enduml")
        };

        var logs = new[]
        {
            new RequestResponseLog(
                TestName: "Place order",
                TestId: "test-1",
                Method: HttpMethod.Post,
                Content: "{\"data\":1}",
                Uri: new Uri("https://api.example.com/orders"),
                Headers: [],
                ServiceName: "OrderService",
                CallerName: "TestClient",
                Type: RequestResponseType.Request,
                TraceId: Guid.NewGuid(),
                RequestResponseId: Guid.NewGuid(),
                TrackingIgnore: false)
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_yaml_diag.yml", DataFormat.Yaml, diagrams, logs);
        var content = File.ReadAllText(path);

        Assert.Contains("Diagrams:", content);
        Assert.Contains("@startuml", content);
        Assert.Contains("HttpInteractions:", content);
        Assert.Contains("OrderService", content);
        Assert.Contains("POST", content);
    }

    [Fact]
    public void GenerateTestRunReportData_http_interaction_includes_headers()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "F1",
                Scenarios =
                [
                    new Scenario { Id = "test-1", DisplayName = "S1", Result = ExecutionResult.Passed }
                ]
            }
        };

        var logs = new[]
        {
            new RequestResponseLog(
                TestName: "S1",
                TestId: "test-1",
                Method: HttpMethod.Get,
                Content: null,
                Uri: new Uri("https://api.example.com/items"),
                Headers: [("Authorization", "Bearer token"), ("Accept", "application/json")],
                ServiceName: "ItemService",
                CallerName: "Client",
                Type: RequestResponseType.Request,
                TraceId: Guid.NewGuid(),
                RequestResponseId: Guid.NewGuid(),
                TrackingIgnore: false)
        };

        var start = DateTime.UtcNow;
        var end = DateTime.UtcNow;

        var path = ReportGenerator.GenerateTestRunReportData(features, start, end, "TestRunData_headers.json", DataFormat.Json, trackedLogs: logs);
        var content = File.ReadAllText(path);
        var doc = JsonDocument.Parse(content);

        var interaction = doc.RootElement.GetProperty("features")[0].GetProperty("scenarios")[0].GetProperty("httpInteractions")[0];
        var headers = interaction.GetProperty("headers");
        Assert.Equal(2, headers.GetArrayLength());
        Assert.Equal("Authorization", headers[0].GetProperty("key").GetString());
        Assert.Equal("Bearer token", headers[0].GetProperty("value").GetString());
    }
}
