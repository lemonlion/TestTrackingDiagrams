using System.Reflection;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

[Collection("DiagramsFetcher")]
public class ReportToggleTests : IDisposable
{
    private static readonly FieldInfo DiagramsField =
        typeof(DefaultDiagramsFetcher).GetField("_diagrams", BindingFlags.Static | BindingFlags.NonPublic)!;

    private readonly string _reportsDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Reports");
    private readonly string _suffix = Guid.NewGuid().ToString("N")[..8];

    public ReportToggleTests()
    {
        DiagramsField.SetValue(null, null);
    }

    public void Dispose()
    {
        DiagramsField.SetValue(null, null);
        // Clean up files created by this test instance
        foreach (var file in Directory.GetFiles(_reportsDir, $"*{_suffix}*"))
            try { File.Delete(file); } catch { /* best-effort */ }
    }

    private ReportConfigurationOptions MakeOptions(Action<ReportConfigurationOptions>? configure = null)
    {
        var options = new ReportConfigurationOptions
        {
            HtmlSpecificationsFileName = $"Specifications_{_suffix}",
            HtmlTestRunReportFileName = $"TestRunReport_{_suffix}",
            YamlSpecificationsFileName = $"Specifications_{_suffix}"
        };
        configure?.Invoke(options);
        return options;
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
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, $"Specifications_{_suffix}.html")));
    }

    [Fact]
    public void SpecificationsReport_skipped_when_disabled()
    {
        var options = MakeOptions(o => o.GenerateSpecificationsReport = false);

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, $"Specifications_{_suffix}.html")));
    }

    // --- Test Run Report HTML ---

    [Fact]
    public void TestRunReport_generated_by_default()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.html")));
    }

    [Fact]
    public void TestRunReport_skipped_when_disabled()
    {
        var options = MakeOptions(o => o.GenerateTestRunReport = false);

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.html")));
    }

    // --- Specifications Data ---

    [Fact]
    public void SpecificationsData_generated_by_default()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, $"Specifications_{_suffix}.yml")));
    }

    [Fact]
    public void SpecificationsData_skipped_when_disabled()
    {
        var options = MakeOptions(o => o.GenerateSpecificationsData = false);

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, $"Specifications_{_suffix}.yml")));
    }

    // --- Test Run Report Data ---

    [Fact]
    public void TestRunReportData_generated_by_default()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.json")));
    }

    [Fact]
    public void TestRunReportData_skipped_when_disabled()
    {
        var options = MakeOptions(o => o.GenerateTestRunReportData = false);

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.json")));
    }

    // --- Test Run Report Schema ---

    [Fact]
    public void TestRunReportSchema_generated_by_default()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.True(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.schema.json")));
    }

    [Fact]
    public void TestRunReportSchema_skipped_when_disabled()
    {
        var options = MakeOptions(o => o.GenerateTestRunReportSchema = false);

        ReportGenerator.CreateStandardReportsWithDiagrams(
            SimpleFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        Assert.False(File.Exists(Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.schema.json")));
    }

    // --- Specifications blank on failed tests ---

    private static Feature[] FailingFeatures =>
    [
        new()
        {
            DisplayName = "Failing Feature",
            Scenarios =
            [
                new Scenario
                {
                    Id = "f1",
                    DisplayName = "Failing scenario",
                    Result = ExecutionResult.Failed,
                    ErrorMessage = "assertion failed",
                    Duration = TimeSpan.FromMilliseconds(50)
                }
            ]
        }
    ];

    [Fact]
    public void SpecificationsReport_blank_when_tests_fail()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            FailingFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var path = Path.Combine(_reportsDir, $"Specifications_{_suffix}.html");
        Assert.True(File.Exists(path));
        Assert.Equal(string.Empty, File.ReadAllText(path));
    }

    [Fact]
    public void SpecificationsData_blank_when_tests_fail()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            FailingFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var path = Path.Combine(_reportsDir, $"Specifications_{_suffix}.yml");
        Assert.True(File.Exists(path));
        Assert.Equal(string.Empty, File.ReadAllText(path));
    }

    [Fact]
    public void TestRunReport_not_blank_when_tests_fail()
    {
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            FailingFeatures, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var path = Path.Combine(_reportsDir, $"TestRunReport_{_suffix}.html");
        Assert.True(File.Exists(path));
        Assert.NotEqual(string.Empty, File.ReadAllText(path));
    }

    [Fact]
    public void SpecificationsReport_not_blank_when_only_skipped_and_bypassed()
    {
        var features = new Feature[]
        {
            new()
            {
                DisplayName = "Non-failure Feature",
                Scenarios =
                [
                    new Scenario { Id = "s1", DisplayName = "Skipped", Result = ExecutionResult.Skipped, Duration = TimeSpan.FromMilliseconds(10) },
                    new Scenario { Id = "b1", DisplayName = "Bypassed", Result = ExecutionResult.Bypassed, Duration = TimeSpan.FromMilliseconds(10) },
                    new Scenario { Id = "p1", DisplayName = "Passed", Result = ExecutionResult.Passed, Duration = TimeSpan.FromMilliseconds(10) }
                ]
            }
        };
        var options = MakeOptions();

        ReportGenerator.CreateStandardReportsWithDiagrams(
            features, DateTime.UtcNow.AddMinutes(-1), DateTime.UtcNow, options);

        var path = Path.Combine(_reportsDir, $"Specifications_{_suffix}.html");
        Assert.True(File.Exists(path));
        Assert.NotEqual(string.Empty, File.ReadAllText(path));
    }
}
