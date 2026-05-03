using TestTrackingDiagrams.Reports;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.EndToEnd;

/// <summary>
/// Shared helper for generating test reports with diagrams for Selenium tests.
/// </summary>
public static class ReportTestHelper
{
    private const string PlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        participant "Database" as db

        caller -> svc : POST /api/orders
        note left
        Content-Type: application/json
        {"item":"Widget","qty":2}
        end note

        svc -> db : INSERT INTO Orders
        note left
        INSERT INTO Orders (Item, Qty)
        VALUES ('Widget', 2)
        end note
        db --> svc : OK
        svc --> caller : 201 Created
        note left
        {"id":"abc-123","status":"created"}
        end note
        @enduml
        """;

    /// <summary>
    /// A wide PlantUML diagram with many participants that exceeds typical container widths.
    /// Used by zoom tests that need a diagram wider than the viewport.
    /// </summary>
    private const string WidePlantUmlSource = """
        @startuml
        participant "AuthenticationService" as a1
        participant "AuthorizationEngine" as a2
        participant "UserProfileManager" as a3
        participant "OrderProcessingUnit" as a4
        participant "InventoryTracker" as a5
        participant "PaymentGateway" as a6
        participant "NotificationHub" as a7
        participant "AuditLogService" as a8
        participant "CacheManager" as a9
        participant "ExternalApiClient" as a10
        participant "ReportingEngine" as a11
        participant "DataWarehouse" as a12
        participant "EventStreamProcessor" as a13
        participant "ConfigurationStore" as a14

        a1 -> a2 : validatePermissions
        a2 -> a3 : getUserProfile
        a3 -> a4 : processOrder
        a4 -> a5 : checkInventory
        a5 -> a6 : processPayment
        a6 -> a7 : sendNotification
        a7 -> a8 : logActivity
        a8 -> a9 : updateCache
        a9 -> a10 : callExternalApi
        a10 -> a11 : generateReport
        a11 -> a12 : storeResults
        a12 -> a13 : processEventStream
        a13 -> a14 : getConfiguration
        a14 --> a1 : complete
        @enduml
        """;

    public static (Feature[] Features, DiagramAsCode[] Diagrams) CreateTestData()
    {
        var features = new[]
        {
            new Feature
            {
                DisplayName = "Order Feature",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "t1", DisplayName = "Create order successfully", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(2),
                        Categories = ["Smoke", "API"],
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the system is running", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I create an order", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "Then", Text = "the order is created", Status = ExecutionResult.Passed }
                        ]
                    },
                    new Scenario
                    {
                        Id = "t2", DisplayName = "Delete order fails gracefully", IsHappyPath = false,
                        Result = ExecutionResult.Failed, Duration = TimeSpan.FromSeconds(5),
                        Categories = ["API"],
                        Steps =
                        [
                            new ScenarioStep { Keyword = "Given", Text = "the system is running", Status = ExecutionResult.Passed },
                            new ScenarioStep { Keyword = "When", Text = "I delete a non-existent order", Status = ExecutionResult.Failed },
                            new ScenarioStep { Keyword = "Then", Text = "an error is returned", Status = ExecutionResult.Skipped }
                        ]
                    },
                    new Scenario
                    {
                        Id = "t3", DisplayName = "List orders returns paginated results", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromSeconds(1),
                        Categories = ["Smoke"]
                    }
                ]
            },
            new Feature
            {
                DisplayName = "Payment Feature",
                Scenarios =
                [
                    new Scenario
                    {
                        Id = "t4", DisplayName = "Process payment", IsHappyPath = true,
                        Result = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(500)
                    },
                    new Scenario
                    {
                        Id = "t5", DisplayName = "Refund payment", IsHappyPath = false,
                        Result = ExecutionResult.Skipped, Duration = TimeSpan.FromMilliseconds(100)
                    }
                ]
            }
        };

        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", PlantUmlSource),
            new DiagramAsCode("t2", "", PlantUmlSource)
        };

        return (features, diagrams);
    }

    public static string GenerateReport(string tempDir, string outputDir, string fileName)
    {
        var (features, diagrams) = CreateTestData();

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    public static string GenerateReportWithWideDiagram(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", WidePlantUmlSource),
            new DiagramAsCode("t2", "", WidePlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// A wide PlantUML diagram that also contains notes (for testing zoom + note interaction).
    /// </summary>
    private const string WideWithNotesPlantUmlSource = """
        @startuml
        participant "AuthenticationService" as a1
        participant "AuthorizationEngine" as a2
        participant "UserProfileManager" as a3
        participant "OrderProcessingUnit" as a4
        participant "InventoryTracker" as a5
        participant "PaymentGateway" as a6
        participant "NotificationHub" as a7
        participant "AuditLogService" as a8
        participant "CacheManager" as a9
        participant "ExternalApiClient" as a10
        participant "ReportingEngine" as a11
        participant "DataWarehouse" as a12
        participant "EventStreamProcessor" as a13
        participant "ConfigurationStore" as a14

        a1 -> a2 : validatePermissions
        note left
        Authorization request
        {"user":"admin","action":"create"}
        end note
        a2 -> a3 : getUserProfile
        a3 -> a4 : processOrder
        note left
        Order payload
        {"item":"Widget","qty":2}
        end note
        a4 -> a5 : checkInventory
        a5 -> a6 : processPayment
        a6 -> a7 : sendNotification
        a7 -> a8 : logActivity
        a8 -> a9 : updateCache
        a9 -> a10 : callExternalApi
        a10 -> a11 : generateReport
        a11 -> a12 : storeResults
        a12 -> a13 : processEventStream
        a13 -> a14 : getConfiguration
        a14 --> a1 : complete
        @enduml
        """;

    public static string GenerateReportWithWideNoteDiagram(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", WideWithNotesPlantUmlSource),
            new DiagramAsCode("t2", "", WideWithNotesPlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// Generates a report with an embedded component diagram for testing
    /// the dependency-type coloring and embedded component diagram section.
    /// </summary>
    public static string GenerateReportWithEmbeddedComponentDiagram(string tempDir, string outputDir, string fileName)
    {
        var (features, diagrams) = CreateTestData();

        // PlantUML for a component diagram with typed shapes
        const string componentPlantUml = """
            @startuml
            left to right direction
            skinparam defaultTextAlignment center
            skinparam wrapWidth 200
            skinparam shadowing false
            skinparam rectangle<<person>> {
              BackgroundColor #08427B
              FontColor #FFFFFF
              BorderColor #073B6F
              RoundCorner 25
            }
            skinparam rectangle<<system>> {
              BackgroundColor #438DD5
              FontColor #FFFFFF
              BorderColor #3C7FC0
              RoundCorner 25
            }
            skinparam database {
              BackgroundColor #E74C3C
              FontColor #FFFFFF
              BorderColor #C0392B
            }
            skinparam queue {
              BackgroundColor #9B59B6
              FontColor #FFFFFF
              BorderColor #7D3C98
            }
            skinparam arrow {
              Color #666666
              FontColor #666666
              FontSize 11
            }

            title Component Diagram

            rectangle "**Client**\n<size:10>[Person]</size>" as client <<person>>
            rectangle "**API**\n<size:10>[Software System]</size>" as api <<system>>
            database "CosmosDB" as cosmosDB
            queue "ServiceBus" as serviceBus

            client -[#438DD5]-> api : "HTTP: GET - 10 calls across 5 tests"
            api -[#E74C3C]-> cosmosDB : "CosmosDB: Query - 8 calls across 4 tests"
            api -[#9B59B6]-> serviceBus : "ServiceBus: Send - 3 calls across 2 tests"
            @enduml
            """;

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs,
            componentDiagramPlantUml: componentPlantUml);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// PlantUML source with one long note (more lines than the default truncation of 40)
    /// and one short note (2 lines). Used by tests that verify the 3-state note cycle
    /// for long notes vs the 2-state cycle for short notes.
    /// </summary>
    private const string LongNotePlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        participant "Database" as db

        caller -> svc : POST /api/orders
        note left
        Line 1
        Line 2
        Line 3
        Line 4
        Line 5
        Line 6
        Line 7
        Line 8
        Line 9
        Line 10
        Line 11
        Line 12
        Line 13
        Line 14
        Line 15
        Line 16
        Line 17
        Line 18
        Line 19
        Line 20
        Line 21
        Line 22
        Line 23
        Line 24
        Line 25
        Line 26
        Line 27
        Line 28
        Line 29
        Line 30
        Line 31
        Line 32
        Line 33
        Line 34
        Line 35
        Line 36
        Line 37
        Line 38
        Line 39
        Line 40
        Line 41
        Line 42
        Line 43
        Line 44
        Line 45
        end note
        svc -> db : INSERT INTO Orders
        note left
        Short note line 1
        Short note line 2
        Short note line 3
        Short note line 4
        end note
        db --> svc : OK
        svc --> caller : 201 Created
        @enduml
        """;

    public static string GenerateReportWithLongNotes(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        // Only one diagram to avoid ambiguity in Selenium selectors
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", LongNotePlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    private const string PartitionPlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "SetupService" as setup
        participant "OrderService" as svc
        participant "Database" as db

        partition #F6F6F6 Setup
          caller -> setup : POST /api/setup
          note left
          Content-Type: application/json
          {"env":"test"}
          end note
          setup --> caller : 200 OK
        end

        caller -> svc : POST /api/orders
        note left
        Content-Type: application/json
        {"item":"Widget","qty":2}
        end note
        svc -> db : INSERT INTO Orders
        note left
        INSERT INTO Orders (Item, Qty)
        VALUES ('Widget', 2)
        end note
        db --> svc : OK
        svc --> caller : 201 Created
        @enduml
        """;

    private static string PartitionLongNotePlantUmlSource
    {
        get
        {
            // Build PlantUML source with long notes (> 40 lines) to trigger truncation
            var longContent = string.Join("\n", Enumerable.Range(1, 50).Select(i => $"Line {i}: some content here"));
            return $"""
                @startuml
                actor "Caller" as caller
                participant "SetupService" as setup
                participant "OrderService" as svc
                participant "Database" as db

                partition #F6F6F6 Setup
                  caller -> setup : POST /api/setup
                  note left
                {longContent}
                  end note
                  setup --> caller : 200 OK
                end

                caller -> svc : POST /api/orders
                note left
                {longContent}
                end note
                svc -> db : INSERT INTO Orders
                note left
                {longContent}
                end note
                db --> svc : OK
                svc --> caller : 201 Created
                @enduml
                """;
        }
    }

    public static string GenerateReportWithPartitionLongNotes(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", PartitionLongNotePlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    public static string GenerateReportWithPartitionDiagram(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", PartitionPlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// PlantUML source with long notes (10+ lines) for testing truncation across
    /// multiple diagrams. Each scenario gets its own diagram with notes that exceed
    /// a truncation limit of 5 lines.
    /// </summary>
    private static string TwoScenarioLongNotePlantUmlSource(int scenarioIndex)
    {
        var longContent = string.Join("\n", Enumerable.Range(1, 15).Select(i => $"Scenario {scenarioIndex} - Line {i}"));
        return $"""
            @startuml
            actor "Caller" as caller
            participant "Service{scenarioIndex}" as svc
            participant "Database" as db

            caller -> svc : POST /api/items
            note left
            {longContent}
            end note
            svc -> db : INSERT INTO Items
            db --> svc : OK
            svc --> caller : 201 Created
            note right
            {longContent}
            end note
            @enduml
            """;
    }

    public static string GenerateReportWithTwoLongNoteDiagrams(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", TwoScenarioLongNotePlantUmlSource(1)),
            new DiagramAsCode("t2", "", TwoScenarioLongNotePlantUmlSource(2))
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// Generates a report where ONE scenario has TWO diagram containers (simulating a
    /// split diagram). Each diagram has long notes that exceed a truncation limit of 5.
    /// Used to test that hover buttons appear on ALL diagrams within a single scenario
    /// after a truncation dropdown change.
    /// </summary>
    public static string GenerateReportWithSplitDiagramLongNotes(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();

        // Generate long note content (50+ lines) to simulate real-world split diagrams
        var longContent1 = string.Join("\n",
            Enumerable.Range(1, 50).Select(i => $"  \"field{i}\": \"value {i}\","));
        var longContent2 = string.Join("\n",
            new[] { "..Continued From Previous Diagram.." }.Concat(
                Enumerable.Range(1, 50).Select(i => $"  \"continued_{i}\": \"data {i}\",")));

        var source1 = $$"""
            @startuml
            !pragma teoz true
            skinparam wrapWidth 800
            autonumber 1
            actor "Caller" as caller
            entity "Service" as svc
            caller -> svc : GET /api/spec
            note left
            <color:gray>[traceparent=00-abc-123-00]
            end note
            svc --> caller : OK
            note right
            <color:gray>[X-Correlation-Id=test-123]

            {
            {{longContent1}}
            ..Continued On Next Diagram..
            end note
            @enduml
            """;

        var source2 = $$"""
            @startuml
            !pragma teoz true
            skinparam wrapWidth 800
            autonumber 2
            actor "Caller" as caller
            entity "Service" as svc
            svc --> caller : OK
            note right
            {{longContent2}}
            }
            end note
            @enduml
            """;

        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", source1),
            new DiagramAsCode("t1", "", source2)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// Generates a report with THREE diagram containers for ONE scenario, matching a real-world
    /// split diagram with a very large response body (like an AsyncAPI spec).
    /// Structure: diagram 1 has no notes, diagram 2 has 2 notes (short header + long body),
    /// diagram 3 has 1 note (continuation with "..Continued From Previous Diagram..").
    /// </summary>
    public static string GenerateReportWithThreeDiagramSplit(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();

        // Diagram 1: simple request/response with NO notes (like puml-0 in the real report)
        const string source1 = """
            @startuml
            !pragma teoz true
            skinparam wrapWidth 800
            autonumber 1
            actor "Caller" as caller
            entity "Service" as svc
            caller -[#438DD5]> svc : GET /api/spec
            @enduml
            """;

        // Diagram 2: response with 2 notes — short header + VERY long JSON body (200+ lines)
        var longJsonContent = string.Join("\n",
            Enumerable.Range(1, 200).Select(i =>
                $"    \"field_{i}\": {{\"type\": \"string\", \"description\": \"Field {i} description\"}},"
            ));
        var source2 = $$"""
            @startuml
            !pragma teoz true
            skinparam wrapWidth 800
            autonumber 1
            actor "Caller" as caller
            entity "Service" as svc
            caller -[#438DD5]> svc : GET /api/spec
            note left
            <color:gray>[traceparent=00-abc-def-00]
            end note
            svc -[#438DD5]-> caller: OK
            note right
            <color:gray>[X-Correlation-Id=test-456]

            {
              "asyncapi": "3.0.0",
              "info": {
                "title": "Breakfast Provider",
                "version": "1.0.0"
              },
              "components": {
                "schemas": {
            {{longJsonContent}}
              ..Continued On Next Diagram..
            end note
            @enduml
            """;

        // Diagram 3: continuation note with "..Continued From Previous Diagram.."
        var continuedContent = string.Join("\n",
            Enumerable.Range(201, 100).Select(i =>
                $"    \"continued_{i}\": {{\"type\": \"integer\", \"description\": \"Continued field {i}\"}},"
            ));
        var source3 = $$"""
            @startuml
            !pragma teoz true
            skinparam wrapWidth 800
            autonumber 2
            actor "Caller" as caller
            entity "Service" as svc
            svc -[#438DD5]-> caller: OK
            note right
            ..Continued From Previous Diagram..
            {{continuedContent}}
                }
              }
            }
            end note
            @enduml
            """;

        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", source1),
            new DiagramAsCode("t1", "", source2),
            new DiagramAsCode("t1", "", source3)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// PlantUML source with one long note (45+ body lines, exceeds default truncation of 40)
    /// AND &lt;color:gray&gt; header lines, plus one short note (4 body lines + headers).
    /// Used by tests that verify note hover button behavior after hiding headers.
    /// </summary>
    private const string LongNoteWithHeadersPlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        participant "Database" as db

        caller -> svc : POST /api/orders
        note left
        <color:gray>Content-Type: application/json
        <color:gray>Authorization: Bearer token123

        Line 1
        Line 2
        Line 3
        Line 4
        Line 5
        Line 6
        Line 7
        Line 8
        Line 9
        Line 10
        Line 11
        Line 12
        Line 13
        Line 14
        Line 15
        Line 16
        Line 17
        Line 18
        Line 19
        Line 20
        Line 21
        Line 22
        Line 23
        Line 24
        Line 25
        Line 26
        Line 27
        Line 28
        Line 29
        Line 30
        Line 31
        Line 32
        Line 33
        Line 34
        Line 35
        Line 36
        Line 37
        Line 38
        Line 39
        Line 40
        Line 41
        Line 42
        Line 43
        Line 44
        Line 45
        end note
        svc -> db : INSERT INTO Orders
        note left
        <color:gray>Content-Type: text/plain
        <color:gray>X-Request-Id: abc-123

        Short note line 1
        Short note line 2
        Short note line 3
        Short note line 4
        end note
        db --> svc : OK
        svc --> caller : 201 Created
        @enduml
        """;

    public static string GenerateReportWithLongNotesAndHeaders(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        // Only one diagram to avoid ambiguity in Selenium selectors
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", LongNoteWithHeadersPlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    /// <summary>
    /// PlantUML source where note 1 is ALL gray headers (no body content) and note 2
    /// has actual body content. When headers are hidden, note 1 becomes empty, testing
    /// the index alignment between SVG groups and source note blocks.
    /// </summary>
    private const string HeaderOnlyNotePlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        participant "Database" as db

        caller -> svc : GET /api/orders
        note left
        <color:gray>Authorization: Bearer token123
        <color:gray>Accept: application/json
        <color:gray>X-Request-Id: req-001
        end note
        svc -> db : SELECT * FROM Orders
        note left
        <color:gray>Content-Type: text/plain

        SELECT Id, Name, Status
        FROM Orders
        WHERE Active = 1
        end note
        db --> svc : OK
        note right
        <color:gray>Content-Type: application/json

        [{"id":1,"name":"Order A"},{"id":2,"name":"Order B"}]
        end note
        svc --> caller : 200 OK
        @enduml
        """;

    /// <summary>
    /// PlantUML source with multiple header-only notes interspersed with content notes.
    /// Notes 1 and 3 are all-headers; notes 2 and 4 have body content.
    /// </summary>
    private const string MultipleHeaderOnlyNotesPlantUmlSource = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        participant "PaymentService" as pay
        participant "Database" as db

        caller -> svc : GET /api/orders
        note left
        <color:gray>Authorization: Bearer token123
        <color:gray>Accept: application/json
        end note
        svc -> db : SELECT * FROM Orders
        note left
        <color:gray>X-DB-Hint: readonly

        SELECT Id, Name FROM Orders
        end note
        db --> svc : OK
        svc -> pay : POST /api/charge
        note left
        <color:gray>Content-Type: application/json
        <color:gray>X-Idempotency-Key: abc-123
        end note
        pay --> svc : 200 OK
        note right
        <color:gray>Content-Type: application/json

        {"chargeId":"ch_001","status":"succeeded"}
        end note
        svc --> caller : 200 OK
        @enduml
        """;

    public static string GenerateReportWithHeaderOnlyNotes(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", HeaderOnlyNotePlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }

    public static string GenerateReportWithMultipleHeaderOnlyNotes(string tempDir, string outputDir, string fileName)
    {
        var (features, _) = CreateTestData();
        var diagrams = new[]
        {
            new DiagramAsCode("t1", "", MultipleHeaderOnlyNotesPlantUmlSource)
        };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, Path.Combine(tempDir, fileName), "Test Report", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);

        File.Copy(path, Path.Combine(outputDir, fileName), true);
        return new Uri(path).AbsoluteUri;
    }
}
