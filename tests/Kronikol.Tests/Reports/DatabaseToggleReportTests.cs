using Kronikol.Reports;
using static Kronikol.DefaultDiagramsFetcher;

namespace Kronikol.Tests.Reports;

public class DatabaseToggleReportTests
{
    private const string PlantUmlSourceWithDatabase = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc
        database "CosmosDB" as cosmosdb #E74C3C

        caller -> svc : POST /api/orders
        note left
        {"item":"Widget","qty":2}
        end note
        svc -[#E74C3C]> cosmosdb: CreateItemAsync
        cosmosdb -[#E74C3C]-> svc: 201 Created
        svc --> caller : 201 Created
        @enduml
        """;

    private const string PlantUmlSourceWithoutDatabase = """
        @startuml
        actor "Caller" as caller
        participant "OrderService" as svc

        caller -> svc : POST /api/orders
        svc --> caller : 200 OK
        @enduml
        """;

    private static string GenerateReport(string fileName, string plantUmlSource)
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
                        Id = "t1", DisplayName = "Create order", IsHappyPath = true,
                        Result = ExecutionResult.Passed,
                        Steps =
                        [
                            new ScenarioStep { Keyword = "When", Text = "I create an order", Status = ExecutionResult.Passed },
                        ]
                    }
                ]
            }
        };

        var diagrams = new[] { new DiagramAsCode("t1", "", plantUmlSource) };

        var path = ReportGenerator.GenerateHtmlReport(
            diagrams, features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml,
            plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Databases_toggle_button_rendered_when_database_participant_present()
    {
        var content = GenerateReport("DbToggle_Present.html", PlantUmlSourceWithDatabase);
        Assert.Contains("data-toggle=\"databases\"", content);
        Assert.Contains("Databases Shown", content);
        Assert.Contains("_toggleDatabases", content);
    }

    [Fact]
    public void Databases_toggle_button_not_rendered_when_no_database_participant()
    {
        var content = GenerateReport("DbToggle_Absent.html", PlantUmlSourceWithoutDatabase);
        Assert.DoesNotContain("data-toggle=\"databases\"", content);
        Assert.DoesNotContain("Databases Shown", content);
    }

    [Fact]
    public void StripDatabaseCalls_function_is_present_in_report_script()
    {
        var content = GenerateReport("DbToggle_StripFn.html", PlantUmlSourceWithDatabase);
        Assert.Contains("function stripDatabaseCalls", content);
        Assert.Contains("function applyDatabasesFilter", content);
        Assert.Contains("function buildDatabasesQueue", content);
    }

    [Fact]
    public void Databases_toggle_defaults_to_shown()
    {
        var content = GenerateReport("DbToggle_Default.html", PlantUmlSourceWithDatabase);
        // The button should have details-active class and data-shown="true"
        var btnIdx = content.IndexOf("data-toggle=\"databases\"");
        Assert.True(btnIdx > 0);
        // Find the opening <button tag and closing >
        var btnStart = content.LastIndexOf("<button", btnIdx);
        var btnEnd = content.IndexOf(">", btnIdx);
        var btnTag = content[btnStart..(btnEnd + 1)];
        Assert.Contains("details-active", btnTag);
        Assert.Contains("data-shown=\"true\"", btnTag);
    }

    [Fact]
    public void Global_databasesVisible_defaults_to_true()
    {
        var content = GenerateReport("DbToggle_Global.html", PlantUmlSourceWithDatabase);
        Assert.Contains("window._databasesVisible = true", content);
    }
}
