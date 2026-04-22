using TestTrackingDiagrams.Reports;
using static TestTrackingDiagrams.DefaultDiagramsFetcher;

namespace TestTrackingDiagrams.Tests.Selenium;

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
}
