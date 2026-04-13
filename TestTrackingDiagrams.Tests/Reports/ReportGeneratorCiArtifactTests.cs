using System.Reflection;
using TestTrackingDiagrams.Reports;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("DiagramsFetcher")]
public class ReportGeneratorCiArtifactTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly string _reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public ReportGeneratorCiArtifactTests()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
        RequestResponseLogger.Clear();
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_publishes_artifacts_when_enabled()
    {
        var options = new ReportConfigurationOptions { PublishCiArtifacts = true };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        // Reports should exist in the directory (standard reports are always generated)
        var reportFiles = Directory.GetFiles(_reportsDir);
        Assert.True(reportFiles.Length >= 3, $"Expected at least 3 report files, found {reportFiles.Length}");
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_skips_artifacts_when_disabled()
    {
        var options = new ReportConfigurationOptions { PublishCiArtifacts = false };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        // This should not throw — artifact publishing is skipped
        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);
    }

    [Fact]
    public void CreateStandardReportsWithDiagrams_includes_ci_summary_in_artifacts_when_both_enabled()
    {
        var options = new ReportConfigurationOptions
        {
            PublishCiArtifacts = true,
            WriteCiSummary = true,
            WriteCiSummaryInteractiveHtml = true
        };
        var features = new[] { new Feature { DisplayName = "Orders", Scenarios = [new Scenario { Id = "1", DisplayName = "Create order", Result = ExecutionResult.Passed }] } };

        ReportGenerator.CreateStandardReportsWithDiagrams(features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "CiSummary.md")));
        Assert.True(File.Exists(Path.Combine(_reportsDir, "CiSummaryInteractive.html")));
    }
}
