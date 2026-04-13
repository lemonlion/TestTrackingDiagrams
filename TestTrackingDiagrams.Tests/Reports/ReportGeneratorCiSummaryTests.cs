using System.Reflection;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("DiagramsFetcher")]
public class ReportGeneratorCiSummaryTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly string _reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public ReportGeneratorCiSummaryTests()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();

        var ciSummaryPath = Path.Combine(_reportsDir, "CiSummary.md");
        if (File.Exists(ciSummaryPath)) File.Delete(ciSummaryPath);

        var interactivePath = Path.Combine(_reportsDir, "CiSummaryInteractive.html");
        if (File.Exists(interactivePath)) File.Delete(interactivePath);
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();

        var ciSummaryPath = Path.Combine(_reportsDir, "CiSummary.md");
        if (File.Exists(ciSummaryPath)) File.Delete(ciSummaryPath);

        var interactivePath = Path.Combine(_reportsDir, "CiSummaryInteractive.html");
        if (File.Exists(interactivePath)) File.Delete(interactivePath);
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_writes_ci_summary_when_enabled()
    {
        var options = new ReportConfigurationOptions { WriteCiSummary = true };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var ciSummaryPath = Path.Combine(_reportsDir, "CiSummary.md");
        Assert.True(File.Exists(ciSummaryPath));
        var content = File.ReadAllText(ciSummaryPath);
        Assert.Contains("# Diagrammed Test Run Summary", content);
        Assert.Contains("Passed", content);
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_skips_ci_summary_when_disabled()
    {
        var options = new ReportConfigurationOptions { WriteCiSummary = false };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var ciSummaryPath = Path.Combine(_reportsDir, "CiSummary.md");
        Assert.False(File.Exists(ciSummaryPath));
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_writes_interactive_html_when_enabled()
    {
        var options = new ReportConfigurationOptions { WriteCiSummary = true, WriteCiSummaryInteractiveHtml = true };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var interactivePath = Path.Combine(_reportsDir, "CiSummaryInteractive.html");
        Assert.True(File.Exists(interactivePath));
        var content = File.ReadAllText(interactivePath);
        Assert.Contains("<html>", content);
        Assert.Contains("CI Test Run Summary", content);
    }
}
