using System.Net;
using TestTrackingDiagrams.Mermaid;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Mermaid;

public class MermaidCreatorTests
{
    // ─── Helpers ────────────────────────────────────────────────

    private static readonly string Nl = Environment.NewLine;

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string testName = "My Test",
        string serviceName = "OrderService",
        string callerName = "WebApp",
        string method = "GET",
        string uri = "http://example.com/api/orders",
        string? content = null,
        (string Key, string? Value)[]? headers = null,
        RequestResponseMetaType metaType = RequestResponseMetaType.Default,
        string[]? focusFields = null)
    {
        return new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: content,
            Uri: new Uri(uri),
            Headers: headers ?? [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false,
            MetaType: metaType)
        {
            FocusFields = focusFields
        };
    }

    private static RequestResponseLog MakeResponse(
        string testId = "test-1",
        string testName = "My Test",
        string serviceName = "OrderService",
        string callerName = "WebApp",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? content = null,
        (string Key, string? Value)[]? headers = null,
        RequestResponseMetaType metaType = RequestResponseMetaType.Default,
        string[]? focusFields = null)
    {
        return new RequestResponseLog(
            TestName: testName,
            TestId: testId,
            Method: HttpMethod.Get,
            Content: content,
            Uri: new Uri("http://example.com/api/orders"),
            Headers: headers ?? [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false,
            StatusCode: statusCode,
            MetaType: metaType)
        {
            FocusFields = focusFields
        };
    }

    private static RequestResponseLog MakeOverrideStart(
        string testId = "test-1",
        string? plantUml = null)
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
            IsOverrideStart = true,
            PlantUml = plantUml
        };
    }

    private static RequestResponseLog MakeOverrideEnd(
        string testId = "test-1",
        string? plantUml = null)
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
            IsOverrideEnd = true,
            PlantUml = plantUml
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

    private static string GetMermaid(IEnumerable<RequestResponseLog> logs, bool separateSetup = false, bool highlightSetup = true)
    {
        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs, separateSetup: separateSetup, highlightSetup: highlightSetup).ToList();
        return results.Single().Diagrams.First();
    }

    private static void AssertBalancedRects(string mermaid)
    {
        var lines = mermaid.Split(Environment.NewLine);
        var opens = lines.Count(l => l.TrimStart().StartsWith("rect "));
        var closes = lines.Count(l => l.Trim() == "end");
        Assert.Equal(opens, closes);
    }

    // ─── Null & empty input ─────────────────────────────────────

    [Fact]
    public void Returns_empty_when_input_is_null()
    {
        var results = MermaidCreator.GetMermaidDiagramsPerTestId(null).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Returns_empty_when_input_is_empty_collection()
    {
        var results = MermaidCreator.GetMermaidDiagramsPerTestId([]).ToList();

        Assert.Empty(results);
    }

    // ─── Default excluded headers ───────────────────────────────

    [Fact]
    public void Default_excluded_headers_contain_cache_control_and_pragma()
    {
        var headers = MermaidCreator.DefaultExcludedHeaders;

        Assert.Contains("Cache-Control", headers);
        Assert.Contains("Pragma", headers);
    }

    // ─── Grouping by TestId ─────────────────────────────────────

    [Fact]
    public void Groups_traces_by_test_id_returning_one_result_per_test()
    {
        var logs = new[]
        {
            MakeRequest(testId: "test-A", testName: "Test A"),
            MakeResponse(testId: "test-A", testName: "Test A"),
            MakeRequest(testId: "test-B", testName: "Test B"),
            MakeResponse(testId: "test-B", testName: "Test B"),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.TestId == "test-A" && r.TestName == "Test A");
        Assert.Contains(results, r => r.TestId == "test-B" && r.TestName == "Test B");
    }

    // ─── Mermaid structure ──────────────────────────────────────

    [Fact]
    public void Output_begins_with_sequenceDiagram()
    {
        var logs = new[] { MakeRequest() };
        var mermaid = GetMermaid(logs);

        Assert.StartsWith("sequenceDiagram", mermaid);
    }

    [Fact]
    public void Output_contains_autonumber_directive()
    {
        var logs = new[] { MakeRequest() };
        var mermaid = GetMermaid(logs);

        Assert.Contains("autonumber", mermaid);
    }

    [Fact]
    public void Output_does_not_contain_startuml_or_enduml()
    {
        var logs = new[] { MakeRequest() };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("@startuml", mermaid);
        Assert.DoesNotContain("@enduml", mermaid);
    }

    // ─── Basic request arrow ────────────────────────────────────

    [Fact]
    public void Request_produces_arrow_with_method_and_path()
    {
        var logs = new[] { MakeRequest(uri: "http://example.com/api/orders?id=1") };
        var mermaid = GetMermaid(logs);

        Assert.Contains($"webApp->>orderService: GET: /api/orders?id=1{Nl}", mermaid);
    }

    [Fact]
    public void First_caller_is_actor_and_service_is_participant()
    {
        var logs = new[] { MakeRequest(callerName: "User", serviceName: "Api") };
        var mermaid = GetMermaid(logs);

        Assert.Contains("actor user as User", mermaid);
        Assert.Contains("participant api as Api", mermaid);
    }

    // ─── Basic response arrow ───────────────────────────────────

    [Fact]
    public void Response_produces_return_arrow_with_status_text()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.OK),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains($"orderService-->>webApp: OK{Nl}", mermaid);
    }

    [Fact]
    public void Http_302_response_appends_redirect_to_status()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.Found),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("orderService-->>webApp: Found (Redirect)", mermaid);
    }

    [Fact]
    public void Http_404_response_shows_not_found()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.NotFound),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("orderService-->>webApp: Not Found", mermaid);
    }

    [Fact]
    public void Http_500_response_shows_internal_server_error()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.InternalServerError),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("orderService-->>webApp: Internal Server Error", mermaid);
    }

    // ─── Headers ────────────────────────────────────────────────

    [Fact]
    public void Request_headers_appear_in_note_left()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("Authorization", "Bearer token123")]),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Note left of", mermaid);
        Assert.Contains("[Authorization=Bearer token123]", mermaid);
    }

    [Fact]
    public void Response_headers_appear_in_note_right()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(headers: [("X-Request-Id", "abc123")]),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Note right of", mermaid);
        Assert.Contains("[X-Request-Id=abc123]", mermaid);
    }

    [Fact]
    public void Excluded_headers_are_omitted_from_notes()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("Cache-Control", "no-cache"), ("Accept", "text/html")]),
        };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("Cache-Control", mermaid);
        Assert.Contains("[Accept=text/html]", mermaid);
    }

    [Fact]
    public void Headers_are_sorted_alphabetically()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("Zebra", "z"), ("Alpha", "a")]),
        };
        var mermaid = GetMermaid(logs);

        var alphaIdx = mermaid.IndexOf("[Alpha=a]");
        var zebraIdx = mermaid.IndexOf("[Zebra=z]");
        Assert.True(alphaIdx < zebraIdx, $"Headers should be sorted: Alpha@{alphaIdx} should be before Zebra@{zebraIdx}");
    }

    // ─── JSON formatting ────────────────────────────────────────

    [Fact]
    public void Json_request_body_is_pretty_printed()
    {
        var json = """{"name":"Alice","age":30}""";
        var logs = new[] { MakeRequest(content: json) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Note left of", mermaid);
        // Pretty-printed JSON has line breaks, which become <br/> in Mermaid notes
        Assert.Contains("\"name\"", mermaid);
        Assert.Contains("\"Alice\"", mermaid);
    }

    [Fact]
    public void Json_response_body_is_pretty_printed()
    {
        var json = """{"result":"success"}""";
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: json),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Note right of", mermaid);
        Assert.Contains("\"result\"", mermaid);
        Assert.Contains("\"success\"", mermaid);
    }

    // ─── Non-JSON content formatting ────────────────────────────

    [Fact]
    public void Plain_text_response_body_appears_as_is()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: "plain text body"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("plain text body", mermaid);
    }

    [Fact]
    public void Form_url_encoded_request_body_is_split_on_ampersands()
    {
        var body = "grant_type=client_credentials&client_id=myapp&scope=openid";
        var logs = new[] { MakeRequest(content: body) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("grant_type=client_credentials", mermaid);
        Assert.Contains("client_id=myapp", mermaid);
        Assert.Contains("scope=openid", mermaid);
    }

    [Fact]
    public void Empty_request_body_does_not_produce_note()
    {
        var logs = new[] { MakeRequest(content: null, headers: []) };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("Note left of", mermaid);
    }

    [Fact]
    public void Empty_response_body_and_no_headers_does_not_produce_note()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: null, headers: []),
        };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("Note right of", mermaid);
    }

    // ─── Long URLs ──────────────────────────────────────────────

    [Fact]
    public void Long_url_is_broken_into_chunks_at_max_url_length()
    {
        var longPath = "/api/" + new string('x', 150);
        var logs = new[] { MakeRequest(uri: "http://example.com" + longPath) };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs, maxUrlLength: 50).ToList();
        var mermaid = results.Single().Diagrams.First();

        Assert.Contains("<br/>", mermaid);
    }

    [Fact]
    public void Short_url_is_not_broken_into_chunks()
    {
        var logs = new[] { MakeRequest(uri: "http://example.com/api/orders") };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs, maxUrlLength: 200).ToList();
        var mermaid = results.Single().Diagrams.First();

        // The only <br/> should be from notes/headers, not from the URL
        var arrowLine = mermaid.Split(Nl).First(l => l.Contains("->>"));
        Assert.DoesNotContain("<br/>", arrowLine);
    }

    // ─── Multiple entities ──────────────────────────────────────

    [Fact]
    public void Second_caller_is_participant_not_actor()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "User", serviceName: "Gateway"),
            MakeResponse(callerName: "User", serviceName: "Gateway"),
            MakeRequest(callerName: "Gateway", serviceName: "Backend"),
            MakeResponse(callerName: "Gateway", serviceName: "Backend"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("actor user as User", mermaid);
        Assert.Contains("participant gateway as Gateway", mermaid);
        Assert.Contains("participant backend as Backend", mermaid);
        // Gateway should only appear once in the participant declarations
        var gatewayCount = mermaid.Split("participant gateway as Gateway").Length - 1;
        Assert.Equal(1, gatewayCount);
    }

    [Fact]
    public void Each_unique_participant_is_declared_only_once()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Client", serviceName: "Api"),
            MakeResponse(callerName: "Client", serviceName: "Api"),
            MakeRequest(callerName: "Client", serviceName: "Api"),
            MakeResponse(callerName: "Client", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs);

        var actorCount = mermaid.Split("actor client").Length - 1;
        var participantCount = mermaid.Split("participant api").Length - 1;
        Assert.Equal(1, actorCount);
        Assert.Equal(1, participantCount);
    }

    // ─── Overrides ──────────────────────────────────────────────

    [Fact]
    public void Override_suppresses_interaction_arrows_between_start_and_end()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "User", serviceName: "Api"),
            MakeResponse(callerName: "User", serviceName: "Api"),
            MakeOverrideStart(),
            MakeRequest(callerName: "User", serviceName: "HiddenService"),
            MakeResponse(callerName: "User", serviceName: "HiddenService"),
            MakeOverrideEnd(),
            MakeRequest(callerName: "User", serviceName: "VisibleService"),
            MakeResponse(callerName: "User", serviceName: "VisibleService"),
        };
        var mermaid = GetMermaid(logs);

        // The arrows for the overridden traces should not appear
        Assert.DoesNotContain("->>hiddenService:", mermaid);
        Assert.DoesNotContain("hiddenService-->>", mermaid);
        // But visible service interactions should exist
        Assert.Contains("user->>visibleService:", mermaid);
        Assert.Contains("visibleService-->>user:", mermaid);
    }

    [Fact]
    public void Override_with_custom_markup_inserts_the_content()
    {
        var customMarkup = "Note over user: Custom Note";
        var logs = new[]
        {
            MakeRequest(callerName: "User", serviceName: "Api"),
            MakeOverrideStart(plantUml: customMarkup),
            MakeOverrideEnd(),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Custom Note", mermaid);
    }

    [Fact]
    public void Nested_override_start_is_ignored()
    {
        var logs = new[]
        {
            MakeOverrideStart(plantUml: "first-start"),
            MakeOverrideStart(plantUml: "nested-start"),
            MakeOverrideEnd(plantUml: "end-override"),
            MakeRequest(callerName: "User", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("first-start", mermaid);
        Assert.DoesNotContain("nested-start", mermaid);
        Assert.Contains("end-override", mermaid);
    }

    [Fact]
    public void Override_end_without_start_still_emits_content()
    {
        var logs = new[]
        {
            MakeOverrideEnd(plantUml: "end-markup"),
            MakeRequest(callerName: "User", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("end-markup", mermaid);
    }

    // ─── Event meta type styling ────────────────────────────────

    [Fact]
    public void Event_meta_type_wraps_in_rect()
    {
        var logs = new[]
        {
            MakeRequest(content: "event payload", metaType: RequestResponseMetaType.Event),
            MakeResponse(metaType: RequestResponseMetaType.Event),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("rect rgb(207, 236, 247)", mermaid);
        AssertBalancedRects(mermaid);
    }

    [Fact]
    public void Default_meta_type_does_not_wrap_in_event_rect()
    {
        var logs = new[]
        {
            MakeRequest(metaType: RequestResponseMetaType.Default),
            MakeResponse(metaType: RequestResponseMetaType.Default),
        };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("rect rgb(207, 236, 247)", mermaid);
    }

    // ─── Pre/post formatting processors ─────────────────────────

    [Fact]
    public void Request_pre_formatting_processor_transforms_content_before_formatting()
    {
        var logs = new[]
        {
            MakeRequest(content: "SENSITIVE_DATA"),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(
            logs,
            requestPreFormattingProcessor: c => c.Replace("SENSITIVE_DATA", "REDACTED")).ToList();
        var mermaid = results.Single().Diagrams.First();

        Assert.Contains("REDACTED", mermaid);
        Assert.DoesNotContain("SENSITIVE_DATA", mermaid);
    }

    [Fact]
    public void Request_post_formatting_processor_transforms_content_after_formatting()
    {
        var json = """{"key":"value"}""";
        var logs = new[]
        {
            MakeRequest(content: json),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(
            logs,
            requestPostFormattingProcessor: c => c + "\n-- END --").ToList();
        var mermaid = results.Single().Diagrams.First();

        Assert.Contains("-- END --", mermaid);
    }

    [Fact]
    public void Response_pre_formatting_processor_transforms_content_before_formatting()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: "SECRET"),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(
            logs,
            responsePreFormattingProcessor: c => c.Replace("SECRET", "[HIDDEN]")).ToList();
        var mermaid = results.Single().Diagrams.First();

        Assert.Contains("[HIDDEN]", mermaid);
        Assert.DoesNotContain("SECRET", mermaid);
    }

    [Fact]
    public void Response_post_formatting_processor_transforms_content_after_formatting()
    {
        var json = """{"data":"test"}""";
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: json),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(
            logs,
            responsePostFormattingProcessor: c => c + "\n-- FOOTER --").ToList();
        var mermaid = results.Single().Diagrams.First();

        Assert.Contains("-- FOOTER --", mermaid);
    }

    // ─── MermaidForTest record ──────────────────────────────────

    [Fact]
    public void Result_contains_test_id_test_name_and_original_traces()
    {
        var request = MakeRequest(testId: "my-test", testName: "My Test Name");
        var response = MakeResponse(testId: "my-test", testName: "My Test Name");
        var logs = new[] { request, response };

        var result = MermaidCreator.GetMermaidDiagramsPerTestId(logs).Single();

        Assert.Equal("my-test", result.TestId);
        Assert.Equal("My Test Name", result.TestName);
        Assert.Equal(2, result.Traces.Count());
    }

    // ─── Large response splitting ───────────────────────────────

    [Fact]
    public void Very_large_response_body_is_split_across_multiple_diagrams()
    {
        var largeBody = new string('A', 8_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();
        var diagramCount = results.Single().Diagrams.Length;

        Assert.True(diagramCount >= 2, $"Expected at least 2 diagrams but got {diagramCount}");
    }

    [Fact]
    public void Split_diagrams_contain_continuation_markers()
    {
        var largeBody = new string('B', 8_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();
        var diagrams = results.Single().Diagrams;

        Assert.Contains("...Continued On Next Diagram...", diagrams.First());

        if (diagrams.Length > 1)
            Assert.Contains("...Continued From Previous Diagram...", diagrams[1]);
    }

    [Fact]
    public void Each_split_diagram_starts_with_sequenceDiagram()
    {
        var largeBody = new string('C', 8_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();
        var diagrams = results.Single().Diagrams;

        foreach (var diagram in diagrams)
            Assert.StartsWith("sequenceDiagram", diagram);
    }

    // ─── Diagram splitting on plain text length ─────────────────

    [Fact]
    public void Long_trace_sequence_produces_multiple_diagrams_when_plain_text_exceeds_limit()
    {
        var logs = new List<RequestResponseLog>();
        for (var i = 0; i < 200; i++)
        {
            logs.Add(MakeRequest(
                content: $"{{\"payload_{i}\": \"{new string('X', 300)}\"}}",
                headers: [("X-Trace", $"trace-{i}-{new string('H', 100)}")]));
            logs.Add(MakeResponse(
                content: $"{{\"result_{i}\": \"{new string('Y', 300)}\"}}",
                headers: [("X-Response", $"resp-{i}-{new string('R', 100)}")]));
        }

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();
        var diagramCount = results.Single().Diagrams.Length;

        Assert.True(diagramCount > 1, $"Expected multiple diagrams from long sequence but got {diagramCount}");
    }

    // ─── Camelized names ────────────────────────────────────────

    [Fact]
    public void Multi_word_service_names_are_camelized_for_aliases()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "web app", serviceName: "order service"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("webApp->>orderService", mermaid);
    }

    [Fact]
    public void Service_display_names_remain_original_in_participant_declarations()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Web App", serviceName: "Order Service"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("as Web App", mermaid);
        Assert.Contains("as Order Service", mermaid);
    }

    [Fact]
    public void Service_names_with_colons_produce_valid_aliases()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "localhost:80", serviceName: "localhost:5001"),
            MakeResponse(callerName: "localhost:80", serviceName: "localhost:5001"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("localhost_80 as localhost:80", mermaid);
        Assert.Contains("localhost_5001 as localhost:5001", mermaid);
        Assert.Contains("localhost_80->>localhost_5001", mermaid);
        Assert.Contains("localhost_5001-->>localhost_80", mermaid);
    }

    // ─── Full request-response flow ─────────────────────────────

    [Fact]
    public void Complete_request_response_pair_produces_valid_sequence_diagram()
    {
        var logs = new[]
        {
            MakeRequest(
                callerName: "Browser",
                serviceName: "Api",
                method: "POST",
                uri: "http://api.example.com/users",
                content: """{"name":"Alice"}""",
                headers: [("Content-Type", "application/json")]),
            MakeResponse(
                callerName: "Browser",
                serviceName: "Api",
                statusCode: HttpStatusCode.Created,
                content: """{"id":1,"name":"Alice"}""",
                headers: [("Location", "/users/1")]),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("sequenceDiagram", mermaid);
        Assert.Contains("actor browser as Browser", mermaid);
        Assert.Contains("participant api as Api", mermaid);
        Assert.Contains("browser->>api: POST: /users", mermaid);
        Assert.Contains("Note left of", mermaid);
        Assert.Contains("\"name\"", mermaid);
        Assert.Contains("api-->>browser: Created", mermaid);
        Assert.Contains("Note right of", mermaid);
        Assert.Contains("\"id\"", mermaid);
    }

    // ─── Multiple services chain ────────────────────────────────

    [Fact]
    public void Multi_service_chain_shows_all_participants_and_interactions()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Client", serviceName: "Gateway", method: "GET", uri: "http://gateway/api"),
            MakeRequest(callerName: "Gateway", serviceName: "Backend", method: "GET", uri: "http://backend/internal"),
            MakeResponse(callerName: "Gateway", serviceName: "Backend", statusCode: HttpStatusCode.OK),
            MakeResponse(callerName: "Client", serviceName: "Gateway", statusCode: HttpStatusCode.OK),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("client->>gateway", mermaid);
        Assert.Contains("gateway->>backend", mermaid);
        Assert.Contains("backend-->>gateway", mermaid);
        Assert.Contains("gateway-->>client", mermaid);
    }

    // ─── Escaping ───────────────────────────────────────────────

    [Fact]
    public void Hash_characters_are_escaped()
    {
        Assert.Equal("#35;", MermaidCreator.EscapeMermaid("#"));
    }

    [Fact]
    public void Semicolons_are_escaped()
    {
        Assert.Equal("#59;", MermaidCreator.EscapeMermaid(";"));
    }

    [Fact]
    public void Hash_and_semicolons_together_are_escaped_correctly()
    {
        Assert.Equal("#35;59#59;", MermaidCreator.EscapeMermaid("#59;"));
    }

    [Fact]
    public void Plain_text_is_not_modified_by_escape()
    {
        Assert.Equal("hello world", MermaidCreator.EscapeMermaid("hello world"));
    }

    // ─── ToMermaidNote ──────────────────────────────────────────

    [Fact]
    public void Newlines_are_replaced_with_br_tags()
    {
        Assert.Equal("line1<br/>line2", MermaidCreator.ToMermaidNote("line1\nline2"));
    }

    [Fact]
    public void Colons_are_escaped_in_notes()
    {
        Assert.Equal("key#colon; value", MermaidCreator.ToMermaidNote("key: value"));
    }

    // ─── Invalid JSON is treated as plain text ──────────────────

    [Fact]
    public void Malformed_json_starting_with_brace_is_treated_as_plain_text()
    {
        var badJson = "{not valid json at all";
        var logs = new[] { MakeRequest(content: badJson) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("{not valid json at all", mermaid);
    }

    [Fact]
    public void Malformed_json_starting_with_bracket_is_treated_as_plain_text()
    {
        var badJson = "[not valid json at all";
        var logs = new[] { MakeRequest(content: badJson) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("[not valid json at all", mermaid);
    }

    // ─── Null header value ──────────────────────────────────────

    [Fact]
    public void Null_header_value_renders_without_crashing()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("X-NullVal", null)]),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("[X-NullVal=]", mermaid);
    }

    // ─── Null StatusCode ────────────────────────────────────────

    [Fact]
    public void Response_with_null_status_code_does_not_crash()
    {
        var response = new RequestResponseLog(
            TestName: "My Test",
            TestId: "test-1",
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://example.com/api"),
            Headers: [],
            ServiceName: "OrderService",
            CallerName: "WebApp",
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false,
            StatusCode: null);

        var logs = new[] { MakeRequest(), response };
        var mermaid = GetMermaid(logs);

        Assert.Contains("orderService-->>webApp:", mermaid);
    }

    // ─── String status code via OneOf ───────────────────────────

    [Fact]
    public void Response_with_string_status_code_renders_the_string()
    {
        var response = new RequestResponseLog(
            TestName: "My Test",
            TestId: "test-1",
            Method: HttpMethod.Get,
            Content: null,
            Uri: new Uri("http://example.com/api"),
            Headers: [],
            ServiceName: "OrderService",
            CallerName: "WebApp",
            Type: RequestResponseType.Response,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false,
            StatusCode: "CustomStatus");

        var logs = new[] { MakeRequest(), response };
        var mermaid = GetMermaid(logs);

        Assert.Contains("orderService-->>webApp: Custom Status", mermaid);
    }

    // ─── Override with null PlantUml ────────────────────────────

    [Fact]
    public void Override_start_with_null_content_emits_nothing_extra()
    {
        var logs = new[]
        {
            MakeOverrideStart(plantUml: null),
            MakeOverrideEnd(plantUml: null),
            MakeRequest(callerName: "User", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs);

        Assert.Contains("user->>api:", mermaid);
        Assert.Contains("sequenceDiagram", mermaid);
    }

    // ─── Long form-URL-encoded segment ──────────────────────────

    [Fact]
    public void Long_form_url_encoded_segment_is_chunked()
    {
        var longParam = "key=" + new string('v', 200);
        var logs = new[] { MakeRequest(content: longParam) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("key=", mermaid);
        Assert.Contains(new string('v', 100), mermaid);
    }

    // ─── Empty string content vs null content ───────────────────

    [Fact]
    public void Empty_string_request_body_does_not_produce_note()
    {
        var logs = new[] { MakeRequest(content: "", headers: []) };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("Note left of", mermaid);
    }

    [Fact]
    public void Empty_string_response_body_does_not_produce_note()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: "", headers: []),
        };
        var mermaid = GetMermaid(logs);

        Assert.DoesNotContain("Note right of", mermaid);
    }

    // ─── Plain text request content (non-JSON, non-form) ────────

    [Fact]
    public void Plain_text_request_body_without_ampersands_appears_in_note()
    {
        var logs = new[] { MakeRequest(content: "plain request text") };
        var mermaid = GetMermaid(logs);

        Assert.Contains("Note left of", mermaid);
        Assert.Contains("plain request text", mermaid);
    }

    [Fact]
    public void Xml_request_body_is_treated_as_plain_text_with_ampersand_splitting()
    {
        var xml = "<root><item>value</item></root>";
        var logs = new[] { MakeRequest(content: xml) };
        var mermaid = GetMermaid(logs);

        Assert.Contains("<root><item>value</item></root>", mermaid);
    }

    // ─── HTTP method variations ─────────────────────────────────

    [Fact]
    public void Post_request_shows_POST_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "POST", uri: "http://example.com/api/items") };
        var mermaid = GetMermaid(logs);

        Assert.Contains("POST: /api/items", mermaid);
    }

    [Fact]
    public void Put_request_shows_PUT_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "PUT", uri: "http://example.com/api/items/1") };
        var mermaid = GetMermaid(logs);

        Assert.Contains("PUT: /api/items/1", mermaid);
    }

    [Fact]
    public void Delete_request_shows_DELETE_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "DELETE", uri: "http://example.com/api/items/1") };
        var mermaid = GetMermaid(logs);

        Assert.Contains("DELETE: /api/items/1", mermaid);
    }

    // ─── Setup separation ───────────────────────────────────────

    [Fact]
    public void SeparateSetup_with_action_start_creates_setup_rect()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Client", serviceName: "Setup"),
            MakeResponse(callerName: "Client", serviceName: "Setup"),
            MakeActionStart(),
            MakeRequest(callerName: "Client", serviceName: "Api"),
            MakeResponse(callerName: "Client", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs, separateSetup: true);

        Assert.Contains("rect rgb(226, 226, 240)", mermaid);
        AssertBalancedRects(mermaid);
    }

    [Fact]
    public void SeparateSetup_with_highlight_false_uses_light_rect()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Client", serviceName: "Setup"),
            MakeResponse(callerName: "Client", serviceName: "Setup"),
            MakeActionStart(),
            MakeRequest(callerName: "Client", serviceName: "Api"),
            MakeResponse(callerName: "Client", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs, separateSetup: true, highlightSetup: false);

        Assert.Contains("rect rgb(245, 245, 245)", mermaid);
        AssertBalancedRects(mermaid);
    }

    [Fact]
    public void SeparateSetup_without_action_start_does_not_add_rect()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(),
        };
        var mermaid = GetMermaid(logs, separateSetup: true);

        Assert.DoesNotContain("rect rgb(226, 226, 240)", mermaid);
    }

    [Fact]
    public void Override_inside_setup_closes_rect_before_override()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Client", serviceName: "Setup"),
            MakeResponse(callerName: "Client", serviceName: "Setup"),
            MakeOverrideStart(plantUml: "Note over client: override"),
            MakeOverrideEnd(),
            MakeActionStart(),
            MakeRequest(callerName: "Client", serviceName: "Api"),
            MakeResponse(callerName: "Client", serviceName: "Api"),
        };
        var mermaid = GetMermaid(logs, separateSetup: true);

        AssertBalancedRects(mermaid);
    }

    // ─── SanitizeAlias ──────────────────────────────────────────

    [Fact]
    public void SanitizeAlias_camelizes_and_replaces_special_chars()
    {
        Assert.Equal("myService", MermaidCreator.SanitizeAlias("My Service"));
        Assert.Equal("localhost_5001", MermaidCreator.SanitizeAlias("localhost:5001"));
    }

    // ─── Rect balanced across split diagrams ────────────────────

    [Fact]
    public void Rect_is_closed_and_reopened_across_split_diagrams()
    {
        // Generate enough event traces to trigger a split with open rects
        var logs = new List<RequestResponseLog>();
        for (var i = 0; i < 100; i++)
        {
            logs.Add(MakeRequest(
                content: $"{{\"event_{i}\": \"{new string('E', 200)}\"}}",
                metaType: RequestResponseMetaType.Event));
            logs.Add(MakeResponse(
                content: $"{{\"ack_{i}\": \"{new string('A', 200)}\"}}",
                metaType: RequestResponseMetaType.Event));
        }

        var results = MermaidCreator.GetMermaidDiagramsPerTestId(logs).ToList();
        var diagrams = results.Single().Diagrams;

        foreach (var diagram in diagrams)
            AssertBalancedRects(diagram);
    }
}
