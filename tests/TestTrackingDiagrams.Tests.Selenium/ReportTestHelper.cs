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
}
