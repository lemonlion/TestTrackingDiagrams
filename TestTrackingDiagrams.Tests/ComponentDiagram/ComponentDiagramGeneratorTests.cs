using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class ComponentDiagramGeneratorTests
{
    // ─── Helpers ────────────────────────────────────────────────

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string testName = "My Test",
        string serviceName = "OrderService",
        string callerName = "Caller",
        string method = "GET",
        string uri = "http://example.com/api/orders",
        bool trackingIgnore = false,
        RequestResponseMetaType metaType = RequestResponseMetaType.Default)
    {
        OneOf<HttpMethod, string> parsedMethod = metaType == RequestResponseMetaType.Event
            ? method
            : HttpMethod.Parse(method);

        return new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: parsedMethod,
            Content: null,
            Uri: new Uri(uri),
            Headers: [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: trackingIgnore,
            MetaType: metaType);
    }

    private static RequestResponseLog MakeResponse(
        string testId = "test-1",
        string testName = "My Test",
        string serviceName = "OrderService",
        string callerName = "Caller",
        HttpStatusCode statusCode = HttpStatusCode.OK)
    {
        return new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://example.com/api/orders"),
            Headers: [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false,
            StatusCode: statusCode);
    }

    private static RequestResponseLog MakeOverrideStart(string testId = "test-1")
    {
        return new RequestResponseLog(
            TestName: testId,
            TestId: testId,
            Method: "",
            Content: "",
            Uri: new Uri("http://override.com"),
            Headers: [],
            ServiceName: "",
            CallerName: "",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false)
        {
            IsOverrideStart = true
        };
    }

    private static RequestResponseLog MakeOverrideEnd(string testId = "test-1")
    {
        return new RequestResponseLog(
            TestName: testId,
            TestId: testId,
            Method: "",
            Content: "",
            Uri: new Uri("http://override.com"),
            Headers: [],
            ServiceName: "",
            CallerName: "",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false)
        {
            IsOverrideEnd = true
        };
    }

    private static RequestResponseLog MakeActionStart(string testId = "test-1")
    {
        return new RequestResponseLog(
            TestName: testId,
            TestId: testId,
            Method: "",
            Content: "",
            Uri: new Uri("http://action.com"),
            Headers: [],
            ServiceName: "",
            CallerName: "",
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false)
        {
            IsActionStart = true
        };
    }

    // ═══════════════════════════════════════════════════════════
    // ExtractRelationships Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void ExtractRelationships_EmptyLogs_ReturnsNoRelationships()
    {
        var result = ComponentDiagramGenerator.ExtractRelationships([]);
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractRelationships_SingleRequest_ExtractsOneRelationship()
    {
        var logs = new[] { MakeRequest() };
        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal("Caller", result[0].Caller);
        Assert.Equal("OrderService", result[0].Service);
        Assert.Equal("HTTP", result[0].Protocol);
        Assert.Contains("GET", result[0].Methods);
        Assert.Equal(1, result[0].CallCount);
        Assert.Equal(1, result[0].TestCount);
    }

    [Fact]
    public void ExtractRelationships_DuplicateCallerService_DeduplicatesRelationship()
    {
        var logs = new[]
        {
            MakeRequest(testId: "test-1"),
            MakeRequest(testId: "test-1")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal(2, result[0].CallCount);
        Assert.Equal(1, result[0].TestCount);
    }

    [Fact]
    public void ExtractRelationships_SameRelationshipAcrossTests_AccumulatesCountsAndTests()
    {
        var logs = new[]
        {
            MakeRequest(testId: "test-1"),
            MakeRequest(testId: "test-2"),
            MakeRequest(testId: "test-2")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal(3, result[0].CallCount);
        Assert.Equal(2, result[0].TestCount);
    }

    [Fact]
    public void ExtractRelationships_MultipleProtocols_TrackedSeparately()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "OrderService", serviceName: "PaymentService"),
            MakeRequest(callerName: "OrderService", serviceName: "Kafka", method: "Publish", metaType: RequestResponseMetaType.Event)
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Equal(2, result.Length);
        Assert.Contains(result, r => r.Protocol == "HTTP" && r.Service == "PaymentService");
        Assert.Contains(result, r => r.Protocol == "Publish" && r.Service == "Kafka");
    }

    [Fact]
    public void ExtractRelationships_IgnoredTracking_Excluded()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeRequest(trackingIgnore: true, serviceName: "IgnoredService")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal("OrderService", result[0].Service);
    }

    [Fact]
    public void ExtractRelationships_OverrideMarkers_Excluded()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeOverrideStart(),
            MakeOverrideEnd()
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal("OrderService", result[0].Service);
    }

    [Fact]
    public void ExtractRelationships_ActionStartMarkers_Excluded()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeActionStart()
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
    }

    [Fact]
    public void ExtractRelationships_ResponseLogs_Excluded()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse()
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal(1, result[0].CallCount);
    }

    [Fact]
    public void ExtractRelationships_MultipleMethods_AggregatedInMethods()
    {
        var logs = new[]
        {
            MakeRequest(method: "GET"),
            MakeRequest(method: "POST"),
            MakeRequest(method: "GET")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs);

        Assert.Single(result);
        Assert.Equal(2, result[0].Methods.Count);
        Assert.Contains("GET", result[0].Methods);
        Assert.Contains("POST", result[0].Methods);
        Assert.Equal(3, result[0].CallCount);
    }

    [Fact]
    public void ExtractRelationships_ParticipantFilter_ExcludesMatchingParticipants()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Caller", serviceName: "OrderService"),
            MakeRequest(callerName: "OrderService", serviceName: "InternalHelper")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs,
            participantFilter: name => name != "InternalHelper");

        Assert.Single(result);
        Assert.Equal("OrderService", result[0].Service);
    }

    [Fact]
    public void ExtractRelationships_ParticipantFilter_ExcludesCallerMatch()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "HiddenCaller", serviceName: "OrderService"),
            MakeRequest(callerName: "Caller", serviceName: "OrderService")
        };

        var result = ComponentDiagramGenerator.ExtractRelationships(logs,
            participantFilter: name => name != "HiddenCaller");

        Assert.Single(result);
        Assert.Equal("Caller", result[0].Caller);
    }

    // ═══════════════════════════════════════════════════════════
    // GeneratePlantUml Tests
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GeneratePlantUml_NoRelationships_GeneratesMinimalDiagram()
    {
        var result = ComponentDiagramGenerator.GeneratePlantUml([]);

        Assert.Contains("@startuml", result);
        Assert.Contains("@enduml", result);
        Assert.Contains("Component Diagram", result);
    }

    [Fact]
    public void GeneratePlantUml_SingleRelationship_GeneratesPersonAndSystem()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 5, 3)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("Person(", result);
        Assert.Contains("Caller", result);
        Assert.Contains("System(", result);
        Assert.Contains("OrderService", result);
        Assert.Contains("Rel(", result);
    }

    [Fact]
    public void GeneratePlantUml_MultipleRelationships_AllParticipantsAndRelsGenerated()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET", "POST"], 10, 5),
            new ComponentRelationship("OrderService", "PaymentService", "HTTP", ["POST"], 4, 2)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("Caller", result);
        Assert.Contains("OrderService", result);
        Assert.Contains("PaymentService", result);
        // Should have exactly 2 Rel lines
        var relCount = result.Split("Rel(").Length - 1;
        Assert.Equal(2, relCount);
    }

    [Fact]
    public void GeneratePlantUml_ServiceThatIsAlsoCaller_RenderedAsSystem()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1),
            new ComponentRelationship("OrderService", "PaymentService", "HTTP", ["POST"], 1, 1)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        // Caller should be Person (only appears as caller, never as service)
        // OrderService appears as both caller and service — should be System
        var personCount = result.Split("Person(").Length - 1;
        Assert.Equal(1, personCount);

        // OrderService and PaymentService should be System
        var systemCount = result.Split("System(").Length - 1;
        Assert.Equal(2, systemCount);
    }

    [Fact]
    public void GeneratePlantUml_HttpProtocol_ShowsMethodsInLabel()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET", "POST"], 10, 5)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("GET", result);
        Assert.Contains("POST", result);
    }

    [Fact]
    public void GeneratePlantUml_EventProtocol_ShowsProtocolInLabel()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "Kafka", "Publish", ["Publish"], 3, 2)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("Publish", result);
    }

    [Fact]
    public void GeneratePlantUml_CustomTheme_IncludedInOutput()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1)
        };

        var options = new ComponentDiagramOptions { PlantUmlTheme = "cerulean" };
        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        Assert.Contains("!theme cerulean", result);
    }

    [Fact]
    public void GeneratePlantUml_NoTheme_NoThemeDirective()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.DoesNotContain("!theme", result);
    }

    [Fact]
    public void GeneratePlantUml_CustomTitle_IncludedInOutput()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1)
        };

        var options = new ComponentDiagramOptions { Title = "My Architecture" };
        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        Assert.Contains("My Architecture", result);
    }

    [Fact]
    public void GeneratePlantUml_RelationshipLabelFormatter_Applied()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 5, 3)
        };

        var options = new ComponentDiagramOptions
        {
            RelationshipLabelFormatter = rel => $"Custom: {rel.Protocol} ({rel.CallCount} calls)"
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, options);

        Assert.Contains("Custom: HTTP (5 calls)", result);
    }

    [Fact]
    public void GeneratePlantUml_CallCountAndTestCount_ShownInLabel()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 14, 8)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("14 calls", result);
        Assert.Contains("8 tests", result);
    }

    [Fact]
    public void GeneratePlantUml_ParticipantAliases_AreSanitized()
    {
        var relationships = new[]
        {
            new ComponentRelationship("My Caller App", "Order-Service.API", "HTTP", ["GET"], 1, 1)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        // Should contain display names in quotes
        Assert.Contains("\"My Caller App\"", result);
        Assert.Contains("\"Order-Service.API\"", result);
        // Should be valid PlantUML
        Assert.Contains("@startuml", result);
        Assert.Contains("@enduml", result);
    }

    [Fact]
    public void GeneratePlantUml_IncludesC4Directives()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("!include https://raw.githubusercontent.com/plantuml-stdlib/C4-PlantUML/master/C4_Context.puml", result);
        Assert.DoesNotContain("C4_Component", result);
    }

    [Fact]
    public void GeneratePlantUml_DefaultLabel_UsesHyphenNotEmDash()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 5, 3)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("HTTP: GET - 5 calls across 3 tests", result);
        Assert.DoesNotContain("\u2014", result); // no em-dash
    }

    [Fact]
    public void GeneratePlantUml_StartsWithStartuml()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 1, 1)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.StartsWith("@startuml", result.TrimStart());
    }

    // ═══════════════════════════════════════════════════════════
    // End-to-end: ExtractRelationships → GeneratePlantUml
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void EndToEnd_MultiServiceArchitecture_GeneratesCompleteDiagram()
    {
        var logs = new[]
        {
            MakeRequest(testId: "t1", callerName: "WebApp", serviceName: "OrderService", method: "POST"),
            MakeResponse(testId: "t1", callerName: "WebApp", serviceName: "OrderService"),
            MakeRequest(testId: "t1", callerName: "OrderService", serviceName: "PaymentService", method: "POST"),
            MakeResponse(testId: "t1", callerName: "OrderService", serviceName: "PaymentService"),
            MakeRequest(testId: "t2", callerName: "WebApp", serviceName: "OrderService", method: "GET"),
            MakeResponse(testId: "t2", callerName: "WebApp", serviceName: "OrderService"),
            MakeRequest(testId: "t2", callerName: "OrderService", serviceName: "Kafka", method: "Publish",
                metaType: RequestResponseMetaType.Event),
        };

        var relationships = ComponentDiagramGenerator.ExtractRelationships(logs);
        var puml = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        // WebApp only appears as caller → Person
        Assert.Contains("Person(", puml);
        Assert.Contains("WebApp", puml);

        // OrderService, PaymentService, Kafka → System
        Assert.Contains("OrderService", puml);
        Assert.Contains("PaymentService", puml);
        Assert.Contains("Kafka", puml);

        // 3 distinct relationships
        var relCount = puml.Split("Rel(").Length - 1;
        Assert.Equal(3, relCount);

        // Valid PlantUML wrapper
        Assert.Contains("@startuml", puml);
        Assert.Contains("@enduml", puml);
    }

    // ═══════════════════════════════════════════════════════════
    // GeneratePlantUml with stats — labels, links, styling
    // ═══════════════════════════════════════════════════════════

    [Fact]
    public void GeneratePlantUml_with_stats_includes_percentiles_in_label()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 10, 5)
        };
        var stats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-Caller-OrderService"] = new(10, 5, 50.0, 45.0, 120.0, 250.0, 5.0, 300.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, stats: stats);

        Assert.Contains("P50: 45ms", result);
        Assert.Contains("P95: 120ms", result);
        Assert.Contains("P99: 250ms", result);
    }

    [Fact]
    public void GeneratePlantUml_with_stats_includes_iflow_link()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 10, 5)
        };
        var stats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-Caller-OrderService"] = new(10, 5, 50.0, 45.0, 120.0, 250.0, 5.0, 300.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, stats: stats);

        Assert.Contains("[[#iflow-rel-Caller-OrderService", result);
    }

    [Fact]
    public void GeneratePlantUml_without_stats_uses_existing_format()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 5, 3)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships);

        Assert.Contains("HTTP: GET - 5 calls across 3 tests", result);
        Assert.DoesNotContain("P50:", result);
        Assert.DoesNotContain("[[#iflow-rel-", result);
    }

    [Fact]
    public void GeneratePlantUml_with_stats_includes_error_rate_when_nonzero()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "OrderService", "HTTP", ["GET"], 10, 5)
        };
        var stats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-Caller-OrderService"] = new(10, 5, 50.0, 45.0, 120.0, 250.0, 5.0, 300.0,
                0.15, new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, stats: stats);

        Assert.Contains("15%", result); // error rate shown
    }

    [Fact]
    public void GeneratePlantUml_hotspot_colors_arrows_by_p95()
    {
        var relationships = new[]
        {
            new ComponentRelationship("A", "FastService", "HTTP", ["GET"], 10, 5),
            new ComponentRelationship("A", "SlowService", "HTTP", ["GET"], 10, 5),
            new ComponentRelationship("A", "MediumService", "HTTP", ["GET"], 10, 5)
        };
        var stats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-A-FastService"] = new(10, 5, 10.0, 10.0, 30.0, 40.0, 5.0, 50.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, false),
            ["iflow-rel-A-SlowService"] = new(10, 5, 300.0, 280.0, 500.0, 600.0, 100.0, 700.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, false),
            ["iflow-rel-A-MediumService"] = new(10, 5, 100.0, 90.0, 150.0, 180.0, 50.0, 200.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, false)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, stats: stats);

        // Should contain skinparam or styling for different latency levels
        Assert.Contains("#Green", result);    // Fast (<50ms P95)
        Assert.Contains("#Red", result);      // Slow (>200ms P95)
        Assert.Contains("#Orange", result);   // Medium (50-200ms P95)
    }

    [Fact]
    public void GeneratePlantUml_low_coverage_uses_dashed_line()
    {
        var relationships = new[]
        {
            new ComponentRelationship("Caller", "RareService", "HTTP", ["GET"], 1, 1)
        };
        var stats = new Dictionary<string, RelationshipStats>
        {
            ["iflow-rel-Caller-RareService"] = new(1, 1, 50.0, 50.0, 50.0, 50.0, 50.0, 50.0,
                0.0, new Dictionary<HttpStatusCode, int>(), [], null, null, true)
        };

        var result = ComponentDiagramGenerator.GeneratePlantUml(relationships, stats: stats);

        // Low coverage should use dashed line or warning indicator
        Assert.Contains("..>", result); // dashed arrow in PlantUML
    }
}
