using System.Reflection;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("DiagramsFetcher")]
public class ReportToggleTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly string _reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");

    public ReportToggleTests()
    {
        DiagramsField.SetValue(null, null);
        CleanReports();
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
        CleanReports();
    }

    private void CleanReports()
    {
        if (Directory.Exists(_reportsDir))
            Directory.Delete(_reportsDir, true);
    }

    private static Feature[] SimpleFeatures =>
    [
        new()
        {
            DisplayName = "Toggle Test Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "1",
                    DisplayName = "Toggle scenario",
                    Result = ExecutionResult.Passed,
                    Duration = TimeSpan.FromMilliseconds(100)
                }
            ]
        }
    ];

    // --- Specifications HTML ---

    [Fact]
    public void SpecificationsReport_generated_by_default()
    {
        var options = new ReportConfigurationOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "Specifications.html")));
    }

    [Fact]
    public void SpecificationsReport_skipped_when_disabled()
    {
        var options = new ReportConfigurationOptions { GenerateSpecificationsReport = false };

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, "Specifications.html")));
    }

    // --- Test Run Report HTML ---

    [Fact]
    public void TestRunReport_generated_by_default()
    {
        var options = new ReportConfigurationOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "TestRunReport.html")));
    }

    [Fact]
    public void TestRunReport_skipped_when_disabled()
    {
        var options = new ReportConfigurationOptions { GenerateTestRunReport = false };

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, "TestRunReport.html")));
    }

    // --- Specifications Data ---

    [Fact]
    public void SpecificationsData_generated_by_default()
    {
        var options = new ReportConfigurationOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "Specifications.yml")));
    }

    [Fact]
    public void SpecificationsData_skipped_when_disabled()
    {
        var options = new ReportConfigurationOptions { GenerateSpecificationsData = false };

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, "Specifications.yml")));
    }

    // --- Test Run Report Data ---

    [Fact]
    public void TestRunReportData_generated_by_default()
    {
        var options = new ReportConfigurationOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "TestRunReport.json")));
    }

    [Fact]
    public void TestRunReportData_skipped_when_disabled()
    {
        var options = new ReportConfigurationOptions { GenerateTestRunReportData = false };

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, "TestRunReport.json")));
    }

    // --- Test Run Report Schema ---

    [Fact]
    public void TestRunReportSchema_generated_by_default()
    {
        var options = new ReportConfigurationOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, "TestRunReport.schema.json")));
    }

    [Fact]
    public void TestRunReportSchema_skipped_when_disabled()
    {
        var options = new ReportConfigurationOptions { GenerateTestRunReportSchema = false };

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, "TestRunReport.schema.json")));
    }
}
