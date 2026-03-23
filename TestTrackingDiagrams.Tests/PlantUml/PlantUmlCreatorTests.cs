using System.Net;
using TestTrackingDiagrams.PlantUml;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.PlantUml;

public class PlantUmlCreatorTests
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
        RequestResponseMetaType metaType = RequestResponseMetaType.Default)
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
            MetaType: metaType);
    }

    private static RequestResponseLog MakeResponse(
        string testId = "test-1",
        string testName = "My Test",
        string serviceName = "OrderService",
        string callerName = "WebApp",
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? content = null,
        (string Key, string? Value)[]? headers = null,
        RequestResponseMetaType metaType = RequestResponseMetaType.Default)
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
            MetaType: metaType);
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

    private static string GetPlantUml(IEnumerable<RequestResponseLog> logs)
    {
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        return results.Single().PlantUmls.First().PlainText;
    }

    // ─── Null & empty input ─────────────────────────────────────

    [Fact]
    public void Returns_empty_when_input_is_null()
    {
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(null).ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void Returns_empty_when_input_is_empty_collection()
    {
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId([]).ToList();

        Assert.Empty(results);
    }

    // ─── Default excluded headers ───────────────────────────────

    [Fact]
    public void Default_excluded_headers_contain_cache_control_and_pragma()
    {
        var headers = PlantUmlCreator.DefaultExcludedHeaders;

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

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();

        Assert.Equal(2, results.Count);
        Assert.Contains(results, r => r.TestId == "test-A" && r.TestName == "Test A");
        Assert.Contains(results, r => r.TestId == "test-B" && r.TestName == "Test B");
    }

    // ─── Basic request arrow ────────────────────────────────────

    [Fact]
    public void Request_produces_arrow_with_method_and_path()
    {
        var logs = new[] { MakeRequest(uri: "http://example.com/api/orders?id=1") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains($"webApp -> orderService: GET: /api/orders?id=1{Nl}", plantUml);
    }

    [Fact]
    public void First_caller_is_actor_and_service_is_entity()
    {
        var logs = new[] { MakeRequest(callerName: "User", serviceName: "Api") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("actor \"User\" as user", plantUml);
        Assert.Contains("entity \"Api\" as api", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains($"orderService --> webApp: OK{Nl}", plantUml);
    }

    [Fact]
    public void Http_302_response_appends_redirect_to_status()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.Found),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("orderService --> webApp: Found (Redirect)", plantUml);
    }

    [Fact]
    public void Http_404_response_shows_not_found()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.NotFound),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("orderService --> webApp: Not Found", plantUml);
    }

    [Fact]
    public void Http_500_response_shows_internal_server_error()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(statusCode: HttpStatusCode.InternalServerError),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("orderService --> webApp: Internal Server Error", plantUml);
    }

    // ─── PlantUML structure ─────────────────────────────────────

    [Fact]
    public void Output_begins_with_startuml_and_ends_with_enduml()
    {
        var logs = new[] { MakeRequest() };
        var plantUml = GetPlantUml(logs);

        Assert.StartsWith("@startuml", plantUml);
        Assert.EndsWith($"@enduml{Nl}", plantUml);
    }

    [Fact]
    public void Output_contains_autonumber_directive()
    {
        var logs = new[] { MakeRequest() };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("autonumber 1", plantUml);
    }

    [Fact]
    public void Output_contains_wrap_width_setting()
    {
        var logs = new[] { MakeRequest() };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("skinparam wrapWidth 800", plantUml);
    }

    // ─── Headers ────────────────────────────────────────────────

    [Fact]
    public void Request_headers_appear_in_note_left()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("Authorization", "Bearer token123")]),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("note left", plantUml);
        Assert.Contains("[Authorization=Bearer token123]", plantUml);
    }

    [Fact]
    public void Response_headers_appear_in_note_right()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(headers: [("X-Correlation-Id", "abc-123")]),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("note right", plantUml);
        Assert.Contains("[X-Correlation-Id=abc-123]", plantUml);
    }

    [Fact]
    public void Default_excluded_headers_are_omitted_from_notes()
    {
        var logs = new[]
        {
            MakeRequest(headers:
            [
                ("Cache-Control", "no-cache"),
                ("Pragma", "no-cache"),
                ("Accept", "application/json")
            ]),
        };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("Cache-Control", plantUml);
        Assert.DoesNotContain("Pragma", plantUml);
        Assert.Contains("[Accept=application/json]", plantUml);
    }

    [Fact]
    public void Custom_excluded_headers_are_respected()
    {
        var logs = new[]
        {
            MakeRequest(headers:
            [
                ("Accept", "application/json"),
                ("Authorization", "Bearer xyz")
            ]),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            excludedHeaders: ["Authorization"]).ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.DoesNotContain("Authorization", plantUml);
        Assert.Contains("[Accept=application/json]", plantUml);
    }

    [Fact]
    public void Headers_are_sorted_alphabetically()
    {
        var logs = new[]
        {
            MakeRequest(headers:
            [
                ("Zebra", "z"),
                ("Alpha", "a"),
                ("Middle", "m")
            ]),
        };
        var plantUml = GetPlantUml(logs);

        var alphaPos = plantUml.IndexOf("[Alpha=a]", StringComparison.Ordinal);
        var middlePos = plantUml.IndexOf("[Middle=m]", StringComparison.Ordinal);
        var zebraPos = plantUml.IndexOf("[Zebra=z]", StringComparison.Ordinal);

        Assert.True(alphaPos < middlePos, "Alpha should appear before Middle");
        Assert.True(middlePos < zebraPos, "Middle should appear before Zebra");
    }

    // ─── JSON content formatting ────────────────────────────────

    [Fact]
    public void Json_request_body_is_pretty_printed_in_note()
    {
        var json = """{"name":"Alice","age":30}""";
        var logs = new[] { MakeRequest(content: json) };
        var plantUml = GetPlantUml(logs);

        // Pretty-printed JSON should contain line breaks
        Assert.Contains("\"name\": \"Alice\"", plantUml);
        Assert.Contains("\"age\": 30", plantUml);
    }

    [Fact]
    public void Json_array_body_is_pretty_printed()
    {
        var json = """[{"id":1},{"id":2}]""";
        var logs = new[] { MakeRequest(content: json) };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("\"id\": 1", plantUml);
        Assert.Contains("\"id\": 2", plantUml);
    }

    [Fact]
    public void Json_response_body_is_pretty_printed_in_note()
    {
        var json = """{"result":"success"}""";
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: json),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("note right", plantUml);
        Assert.Contains("\"result\": \"success\"", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains("plain text body", plantUml);
    }

    [Fact]
    public void Form_url_encoded_request_body_is_split_on_ampersands()
    {
        var body = "grant_type=client_credentials&client_id=myapp&scope=openid";
        var logs = new[] { MakeRequest(content: body) };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("grant_type=client_credentials", plantUml);
        Assert.Contains("client_id=myapp", plantUml);
        Assert.Contains("scope=openid", plantUml);
    }

    [Fact]
    public void Empty_request_body_does_not_produce_note()
    {
        var logs = new[] { MakeRequest(content: null, headers: []) };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("note left", plantUml);
    }

    [Fact]
    public void Empty_response_body_and_no_headers_does_not_produce_note()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: null, headers: []),
        };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("note right", plantUml);
    }

    // ─── Long URLs ──────────────────────────────────────────────

    [Fact]
    public void Long_url_is_broken_into_chunks_at_max_url_length()
    {
        var longPath = "/api/" + new string('x', 150);
        var logs = new[] { MakeRequest(uri: "http://example.com" + longPath) };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs, maxUrlLength: 50).ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        // The path should be split using \\n (line continuation in PlantUML)
        Assert.Contains("\\n", plantUml);
    }

    [Fact]
    public void Short_url_is_not_broken_into_chunks()
    {
        var logs = new[] { MakeRequest(uri: "http://example.com/api/orders") };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs, maxUrlLength: 200).ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.DoesNotContain("\\n", plantUml);
    }

    // ─── Multiple entities ──────────────────────────────────────

    [Fact]
    public void Second_caller_is_entity_not_actor()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "User", serviceName: "Gateway"),
            MakeResponse(callerName: "User", serviceName: "Gateway"),
            MakeRequest(callerName: "Gateway", serviceName: "Backend"),
            MakeResponse(callerName: "Gateway", serviceName: "Backend"),
        };
        var plantUml = GetPlantUml(logs);

        // "User" is the first caller, so it's actor
        Assert.Contains("actor \"User\" as user", plantUml);
        // "Gateway" first appears as a service, then as a caller — entity both times
        Assert.Contains("entity \"Gateway\" as gateway", plantUml);
        Assert.Contains("entity \"Backend\" as backend", plantUml);
        // "Gateway" should only appear once in the entity declarations
        var gatewayEntityCount = plantUml.Split("entity \"Gateway\" as gateway").Length - 1;
        Assert.Equal(1, gatewayEntityCount);
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
        var plantUml = GetPlantUml(logs);

        var actorCount = plantUml.Split("actor \"Client\"").Length - 1;
        var entityCount = plantUml.Split("entity \"Api\"").Length - 1;
        Assert.Equal(1, actorCount);
        Assert.Equal(1, entityCount);
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
        var plantUml = GetPlantUml(logs);

        // The arrows for the overridden traces should not appear
        Assert.DoesNotContain("user -> hiddenService:", plantUml);
        Assert.DoesNotContain("hiddenService --> user:", plantUml);
        // But visible service interactions should exist
        Assert.Contains("user -> visibleService:", plantUml);
        Assert.Contains("visibleService --> user:", plantUml);
    }

    [Fact]
    public void Override_with_custom_plantuml_inserts_the_markup()
    {
        var customMarkup = "hnote across: Custom Note";
        var logs = new[]
        {
            MakeRequest(callerName: "User", serviceName: "Api"),
            MakeOverrideStart(plantUml: customMarkup),
            MakeOverrideEnd(),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("Custom Note", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains("first-start", plantUml);
        Assert.DoesNotContain("nested-start", plantUml);
        Assert.Contains("end-override", plantUml);
    }

    [Fact]
    public void Override_end_without_start_still_emits_plantuml()
    {
        var logs = new[]
        {
            MakeOverrideEnd(plantUml: "end-markup"),
            MakeRequest(callerName: "User", serviceName: "Api"),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("end-markup", plantUml);
    }

    // ─── Event meta type styling ────────────────────────────────

    [Fact]
    public void Event_meta_type_adds_style_block_and_note_class()
    {
        var logs = new[]
        {
            MakeRequest(content: "event payload", metaType: RequestResponseMetaType.Event),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("<<eventNote>>", plantUml);
        Assert.Contains(".eventNote", plantUml);
        Assert.Contains("BackgroundColor #cfecf7", plantUml);
    }

    [Fact]
    public void Default_meta_type_does_not_add_event_style_block()
    {
        var logs = new[]
        {
            MakeRequest(metaType: RequestResponseMetaType.Default),
        };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("<<eventNote>>", plantUml);
        Assert.DoesNotContain(".eventNote", plantUml);
    }

    [Fact]
    public void Event_note_class_appears_on_response_notes_too()
    {
        var logs = new[]
        {
            MakeRequest(content: "event request", metaType: RequestResponseMetaType.Event),
            MakeResponse(content: """{"event":true}""", metaType: RequestResponseMetaType.Event),
        };
        var plantUml = GetPlantUml(logs);

        var eventNoteOccurrences = plantUml.Split("<<eventNote>>").Length - 1;
        Assert.Equal(2, eventNoteOccurrences);
    }

    // ─── Pre/post formatting processors ─────────────────────────

    [Fact]
    public void Request_pre_formatting_processor_transforms_content_before_formatting()
    {
        var logs = new[]
        {
            MakeRequest(content: "SENSITIVE_DATA"),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            requestPreFormattingProcessor: c => c.Replace("SENSITIVE_DATA", "REDACTED")).ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.Contains("REDACTED", plantUml);
        Assert.DoesNotContain("SENSITIVE_DATA", plantUml);
    }

    [Fact]
    public void Request_post_formatting_processor_transforms_content_after_formatting()
    {
        var json = """{"key":"value"}""";
        var logs = new[]
        {
            MakeRequest(content: json),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            requestPostFormattingProcessor: c => c + "\n-- END --").ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.Contains("-- END --", plantUml);
    }

    [Fact]
    public void Response_pre_formatting_processor_transforms_content_before_formatting()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: "SECRET"),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            responsePreFormattingProcessor: c => c.Replace("SECRET", "[HIDDEN]")).ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.Contains("[HIDDEN]", plantUml);
        Assert.DoesNotContain("SECRET", plantUml);
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

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs,
            responsePostFormattingProcessor: c => c + "\n-- FOOTER --").ToList();
        var plantUml = results.Single().PlantUmls.First().PlainText;

        Assert.Contains("-- FOOTER --", plantUml);
    }

    // ─── Image tags ─────────────────────────────────────────────

    [Fact]
    public void Image_tags_use_default_plantuml_server_url()
    {
        var logs = new[] { MakeRequest() };
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var imageTag = results.Single().ImageTags.First();

        Assert.StartsWith("<img src=\"https://www.plantuml.com/plantuml/png/", imageTag);
        Assert.EndsWith("\">", imageTag);
    }

    [Fact]
    public void Image_tags_use_custom_plantuml_server_url()
    {
        var logs = new[] { MakeRequest() };
        var customUrl = "http://my-plantuml-server/svg";
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs, plantUmlServerRendererUrl: customUrl).ToList();
        var imageTag = results.Single().ImageTags.First();

        Assert.StartsWith("<img src=\"http://my-plantuml-server/svg/", imageTag);
    }

    [Fact]
    public void Trailing_slash_on_server_url_is_not_doubled()
    {
        var logs = new[] { MakeRequest() };
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(
            logs, plantUmlServerRendererUrl: "http://server/png/").ToList();
        var imageTag = results.Single().ImageTags.First();

        Assert.DoesNotContain("png//", imageTag);
    }

    // ─── Encoded PlantUML ───────────────────────────────────────

    [Fact]
    public void PlantUml_encoded_value_is_nonblank()
    {
        var logs = new[] { MakeRequest() };
        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var encoded = results.Single().PlantUmls.First().PlantUmlEncoded;

        Assert.NotNull(encoded);
        Assert.NotEmpty(encoded);
    }

    [Fact]
    public void Same_input_produces_same_encoded_output()
    {
        var logs = new[] { MakeRequest(testId: "stable", testName: "Stable") };

        var run1 = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).Single().PlantUmls.First().PlantUmlEncoded;
        var run2 = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).Single().PlantUmls.First().PlantUmlEncoded;

        Assert.Equal(run1, run2);
    }

    // ─── PlantUmlForTest record ─────────────────────────────────

    [Fact]
    public void Result_contains_test_id_test_name_and_original_traces()
    {
        var request = MakeRequest(testId: "my-test", testName: "My Test Name");
        var response = MakeResponse(testId: "my-test", testName: "My Test Name");
        var logs = new[] { request, response };

        var result = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).Single();

        Assert.Equal("my-test", result.TestId);
        Assert.Equal("My Test Name", result.TestName);
        Assert.Equal(2, result.Traces.Count());
    }

    // ─── Large response splitting ───────────────────────────────

    [Fact]
    public void Very_large_response_body_is_split_across_multiple_diagrams()
    {
        // 15_000 chars per chunk; need >~15_000 to trigger splitting
        var largeBody = new string('A', 20_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var diagramCount = results.Single().PlantUmls.Count();

        Assert.True(diagramCount >= 2, $"Expected at least 2 diagrams but got {diagramCount}");
    }

    [Fact]
    public void Split_diagrams_contain_continuation_markers()
    {
        var largeBody = new string('B', 20_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var diagrams = results.Single().PlantUmls.ToList();

        var firstDiagram = diagrams.First().PlainText;
        Assert.Contains("..Continued On Next Diagram..", firstDiagram);

        if (diagrams.Count > 1)
        {
            var secondDiagram = diagrams[1].PlainText;
            Assert.Contains("..Continued From Previous Diagram..", secondDiagram);
        }
    }

    [Fact]
    public void Each_split_diagram_has_startuml_and_enduml()
    {
        var largeBody = new string('C', 20_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var diagrams = results.Single().PlantUmls.ToList();

        foreach (var diagram in diagrams)
        {
            Assert.Contains("@startuml", diagram.PlainText);
            Assert.Contains("@enduml", diagram.PlainText);
        }
    }

    // ─── Diagram splitting on encoded length ────────────────────

    [Fact]
    public void Long_trace_sequence_produces_multiple_diagrams_when_encoded_exceeds_limit()
    {
        // Generate enough request/response pairs with large payloads to exceed the
        // 2000-char encoded PlantUML limit that triggers diagram splitting
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

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var diagramCount = results.Single().PlantUmls.Count();

        Assert.True(diagramCount > 1, $"Expected multiple diagrams from long sequence but got {diagramCount}");
    }

    [Fact]
    public void Continuation_diagrams_have_correct_autonumber_sequence()
    {
        var logs = new List<RequestResponseLog>();
        for (var i = 0; i < 50; i++)
        {
            logs.Add(MakeRequest(content: $"{{\"i\": \"{new string('X', 100)}\"}}"));
            logs.Add(MakeResponse(content: $"{{\"o\": \"{new string('Y', 100)}\"}}"));
        }

        var results = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).ToList();
        var diagrams = results.Single().PlantUmls.ToList();

        // First diagram should start at 1
        Assert.Contains("autonumber 1", diagrams.First().PlainText);

        // Subsequent diagrams should start at a higher number
        if (diagrams.Count > 1)
        {
            var secondDiagram = diagrams[1].PlainText;
            Assert.DoesNotContain("autonumber 1\n", secondDiagram);
        }
    }

    // ─── Camelized names ────────────────────────────────────────

    [Fact]
    public void Multi_word_service_names_are_camelized_for_aliases()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "web app", serviceName: "order service"),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("webApp -> orderService", plantUml);
    }

    [Fact]
    public void Service_display_names_remain_original_in_entity_declarations()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "Web App", serviceName: "Order Service"),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("\"Web App\"", plantUml);
        Assert.Contains("\"Order Service\"", plantUml);
    }

    [Fact]
    public void Service_names_with_colons_produce_valid_aliases()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "localhost:80", serviceName: "localhost:5001"),
            MakeResponse(callerName: "localhost:80", serviceName: "localhost:5001"),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("\"localhost:80\" as localhost_80", plantUml);
        Assert.Contains("\"localhost:5001\" as localhost_5001", plantUml);
        Assert.Contains("localhost_80 -> localhost_5001", plantUml);
        Assert.Contains("localhost_5001 --> localhost_80", plantUml);
        Assert.DoesNotContain("as localhost:80", plantUml);
        Assert.DoesNotContain("as localhost:5001", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains("@startuml", plantUml);
        Assert.Contains("actor \"Browser\" as browser", plantUml);
        Assert.Contains("entity \"Api\" as api", plantUml);
        Assert.Contains("browser -> api: POST: /users", plantUml);
        Assert.Contains("note left", plantUml);
        Assert.Contains("\"name\": \"Alice\"", plantUml);
        Assert.Contains("api --> browser: Created", plantUml);
        Assert.Contains("note right", plantUml);
        Assert.Contains("\"id\": 1", plantUml);
        Assert.Contains("@enduml", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains("client -> gateway", plantUml);
        Assert.Contains("gateway -> backend", plantUml);
        Assert.Contains("backend --> gateway", plantUml);
        Assert.Contains("gateway --> client", plantUml);
    }

    // ─── Color function ─────────────────────────────────────────

    [Fact]
    public void Color_function_is_defined_in_output()
    {
        var logs = new[] { MakeRequest() };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("!function $color($value)", plantUml);
        Assert.Contains("!return \"<color:\"+$value+\" >\"", plantUml);
    }

    [Fact]
    public void Headers_use_gray_color_function()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("Accept", "text/html")]),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("$color(gray)", plantUml);
    }

    // ─── Invalid JSON is treated as plain text ──────────────────

    [Fact]
    public void Malformed_json_starting_with_brace_is_treated_as_plain_text()
    {
        var badJson = "{not valid json at all";
        var logs = new[] { MakeRequest(content: badJson) };
        var plantUml = GetPlantUml(logs);

        // Should still appear in the output without crashing
        Assert.Contains("{not valid json at all", plantUml);
    }

    [Fact]
    public void Malformed_json_starting_with_bracket_is_treated_as_plain_text()
    {
        var badJson = "[not valid json at all";
        var logs = new[] { MakeRequest(content: badJson) };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("[not valid json at all", plantUml);
    }

    // ─── Long header values ─────────────────────────────────────

    [Fact]
    public void Long_header_value_is_chunked_across_multiple_lines()
    {
        var longValue = new string('V', 200);
        var logs = new[]
        {
            MakeRequest(headers: [("X-Long", longValue)]),
        };
        var plantUml = GetPlantUml(logs);

        // Each chunk gets its own $color(gray) prefix, so >100 chars means multiple lines
        var colorGrayCount = plantUml.Split("$color(gray)").Length - 1;
        Assert.True(colorGrayCount >= 2, $"Expected at least 2 $color(gray) lines for long header, got {colorGrayCount}");
    }

    // ─── Null header value ──────────────────────────────────────

    [Fact]
    public void Null_header_value_renders_without_crashing()
    {
        var logs = new[]
        {
            MakeRequest(headers: [("X-NullVal", null)]),
        };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("[X-NullVal=]", plantUml);
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
        var plantUml = GetPlantUml(logs);

        // Should produce an arrow even though status is null
        Assert.Contains("orderService --> webApp:", plantUml);
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
        var plantUml = GetPlantUml(logs);

        Assert.Contains("orderService --> webApp: Custom Status", plantUml);
    }

    // ─── Override with null PlantUml ────────────────────────────

    [Fact]
    public void Override_start_with_null_plantuml_emits_nothing_extra()
    {
        var logs = new[]
        {
            MakeOverrideStart(plantUml: null),
            MakeOverrideEnd(plantUml: null),
            MakeRequest(callerName: "User", serviceName: "Api"),
        };
        var plantUml = GetPlantUml(logs);

        // Should produce a valid diagram with just the final request
        Assert.Contains("user -> api:", plantUml);
        Assert.Contains("@startuml", plantUml);
        Assert.Contains("@enduml", plantUml);
    }

    // ─── Long form-URL-encoded segment ──────────────────────────

    [Fact]
    public void Long_form_url_encoded_segment_is_chunked()
    {
        var longParam = "key=" + new string('v', 200);
        var logs = new[] { MakeRequest(content: longParam) };
        var plantUml = GetPlantUml(logs);

        // The value exceeds 100 chars, so ChunksUpTo(100) should split it
        // Each chunk appears on its own line, which means multiple lines in the note
        Assert.Contains("key=", plantUml);
        Assert.Contains(new string('v', 100), plantUml);
    }

    // ─── ImageTags count matches PlantUmls count ────────────────

    [Fact]
    public void Image_tags_count_matches_plant_umls_count()
    {
        var largeBody = new string('Z', 20_000);
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: largeBody),
        };

        var result = PlantUmlCreator.GetPlantUmlImageTagsPerTestId(logs).Single();

        Assert.Equal(result.PlantUmls.Count(), result.ImageTags.Length);
        Assert.True(result.ImageTags.Length >= 2, "Should have multiple diagrams and matching image tags");
    }

    // ─── Empty string content vs null content ───────────────────

    [Fact]
    public void Empty_string_request_body_does_not_produce_note()
    {
        var logs = new[] { MakeRequest(content: "", headers: []) };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("note left", plantUml);
    }

    [Fact]
    public void Empty_string_response_body_does_not_produce_note()
    {
        var logs = new[]
        {
            MakeRequest(),
            MakeResponse(content: "", headers: []),
        };
        var plantUml = GetPlantUml(logs);

        Assert.DoesNotContain("note right", plantUml);
    }

    // ─── Plain text request content (non-JSON, non-form) ────────

    [Fact]
    public void Plain_text_request_body_without_ampersands_appears_in_note()
    {
        var logs = new[] { MakeRequest(content: "plain request text") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("note left", plantUml);
        Assert.Contains("plain request text", plantUml);
    }

    [Fact]
    public void Xml_request_body_is_treated_as_plain_text_with_ampersand_splitting()
    {
        var xml = "<root><item>value</item></root>";
        var logs = new[] { MakeRequest(content: xml) };
        var plantUml = GetPlantUml(logs);

        // It's not JSON, so it goes through the form-url-encoded path which splits on &
        // Since there's no &, the whole thing appears as-is
        Assert.Contains("<root><item>value</item></root>", plantUml);
    }

    // ─── actorDefined flag behavior ─────────────────────────────

    [Fact]
    public void Two_distinct_callers_that_first_appear_as_callers_are_both_rendered()
    {
        // First caller "Alice" calls "Api", then "Bob" calls "Api"
        // Both are new callers that weren't previously seen as services
        var logs = new[]
        {
            MakeRequest(callerName: "Alice", serviceName: "Api"),
            MakeResponse(callerName: "Alice", serviceName: "Api"),
            MakeRequest(callerName: "Bob", serviceName: "Api"),
            MakeResponse(callerName: "Bob", serviceName: "Api"),
        };
        var plantUml = GetPlantUml(logs);

        // "Alice" is the first caller seen, so it gets "actor"
        Assert.Contains("actor \"Alice\" as alice", plantUml);
        // "Bob" appears later as a fresh caller not previously seen as a service.
        // The code never sets actorDefined=true, so Bob also gets "actor" —
        // this documents the actual behavior.
        Assert.Contains("\"Bob\" as bob", plantUml);
    }

    // ─── HTTP method variations ─────────────────────────────────

    [Fact]
    public void Post_request_shows_POST_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "POST", uri: "http://example.com/api/items") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("POST: /api/items", plantUml);
    }

    [Fact]
    public void Put_request_shows_PUT_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "PUT", uri: "http://example.com/api/items/1") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("PUT: /api/items/1", plantUml);
    }

    [Fact]
    public void Delete_request_shows_DELETE_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "DELETE", uri: "http://example.com/api/items/1") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("DELETE: /api/items/1", plantUml);
    }

    [Fact]
    public void Patch_request_shows_PATCH_method_on_arrow()
    {
        var logs = new[] { MakeRequest(method: "PATCH", uri: "http://example.com/api/items/1") };
        var plantUml = GetPlantUml(logs);

        Assert.Contains("PATCH: /api/items/1", plantUml);
    }
}
