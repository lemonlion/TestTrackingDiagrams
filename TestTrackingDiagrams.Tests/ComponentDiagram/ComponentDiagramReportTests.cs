using System.Net;
using TestTrackingDiagrams.ComponentDiagram;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.ComponentDiagram;

public class ComponentDiagramReportTests : IDisposable
{
    private readonly string _reportDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public void Dispose()
    {
        // Cleanup generated test files
        foreach (var file in Directory.GetFiles(_reportDir, "ComponentDiagram*"))
            File.Delete(file);
    }

    private static RequestResponseLog MakeRequest(
        string testId = "test-1",
        string callerName = "Caller",
        string serviceName = "OrderService",
        string method = "GET")
    {
        return new RequestResponseLog(
            TestName: "Test",
            TestId: testId,
            Method: HttpMethod.Parse(method),
            Content: null,
            Uri: new Uri("http://example.com/api"),
            Headers: [],
            ServiceName: serviceName,
            CallerName: callerName,
            Type: RequestResponseType.Request,
            TraceId: Guid.NewGuid(),
            RequestResponseId: Guid.NewGuid(),
            TrackingIgnore: false);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CreatesPumlFile()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, options);

        Assert.True(File.Exists(result.PumlFilePath));
        var content = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("@startuml", content);
        Assert.Contains("@enduml", content);
        Assert.Contains("OrderService", content);
    }

    [Fact]
    public void GenerateComponentDiagramReport_CreatesHtmlFile()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, options);

        Assert.True(File.Exists(result.HtmlFilePath));
        var content = File.ReadAllText(result.HtmlFilePath);
        Assert.Contains("<html>", content);
        Assert.Contains("Component Diagram", content);
        Assert.Contains("@startuml", content);
    }

    [Fact]
    public void GenerateComponentDiagramReport_EmptyLogs_StillGeneratesFiles()
    {
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport([], options);

        Assert.True(File.Exists(result.PumlFilePath));
        Assert.True(File.Exists(result.HtmlFilePath));
    }

    [Fact]
    public void GenerateComponentDiagramReport_CustomFileName_UsesIt()
    {
        var logs = new[] { MakeRequest() };
        var options = new ComponentDiagramOptions { FileName = "MyDiagram" };

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, options);

        Assert.Contains("MyDiagram.puml", result.PumlFilePath);
        Assert.Contains("MyDiagram.html", result.HtmlFilePath);
    }

    [Fact]
    public void GenerateComponentDiagramReport_PlantUmlContent_IsCorrect()
    {
        var logs = new[]
        {
            MakeRequest(callerName: "WebApp", serviceName: "OrderService", method: "POST"),
            MakeRequest(callerName: "OrderService", serviceName: "PaymentService", method: "POST")
        };
        var options = new ComponentDiagramOptions();

        var result = ComponentDiagramReportGenerator.GenerateComponentDiagramReport(logs, options);

        var puml = File.ReadAllText(result.PumlFilePath);
        Assert.Contains("Person(", puml);
        Assert.Contains("WebApp", puml);
        Assert.Contains("System(", puml);
        Assert.Contains("OrderService", puml);
        Assert.Contains("PaymentService", puml);
        Assert.Contains("Rel(", puml);
    }
}
