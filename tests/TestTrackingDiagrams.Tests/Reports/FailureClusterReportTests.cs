using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.Reports;

public class FailureClusterReportTests
{
    private static Feature[] MakeFeatures(params (string id, string name, ExecutionResult result, string? errorMessage)[] scenarios) =>
    [
        new Feature
        {
            DisplayName = "Test Feature",
            Scenarios = scenarios.Select(s => new Scenario
            {
                Id = s.id,
                DisplayName = s.name,
                IsHappyPath = false,
                Result = s.result,
                ErrorMessage = s.errorMessage
            }).ToArray()
        }
    ];

    private static Feature[] MakeMultiFeature(params Feature[] features) => features;

    private static string GenerateReport(Feature[] features, string fileName)
    {
        var path = ReportGenerator.GenerateHtmlReport(
            [], features,
            DateTime.UtcNow, DateTime.UtcNow,
            null, fileName, "Test", true,
            diagramFormat: DiagramFormat.PlantUml, plantUmlRendering: PlantUmlRendering.BrowserJs);
        return File.ReadAllText(path);
    }

    [Fact]
    public void Report_with_clustered_failures_shows_failure_clusters_section()
    {
        var features = MakeFeatures(
            ("t1", "Test 1", ExecutionResult.Failed, "Connection refused"),
            ("t2", "Test 2", ExecutionResult.Failed, "Connection refused"),
            ("t3", "Test 3", ExecutionResult.Passed, null));
        var content = GenerateReport(features, "FailureClusters.html");
        Assert.Contains("failure-clusters", content);
    }

    [Fact]
    public void Report_without_clusters_does_not_show_failure_clusters_section()
    {
        var features = MakeFeatures(
            ("t1", "Test 1", ExecutionResult.Passed, null),
            ("t2", "Test 2", ExecutionResult.Passed, null));
        var content = GenerateReport(features, "NoClusters.html");
        Assert.DoesNotContain("class=\"failure-clusters\"", content);
    }

    [Fact]
    public void Report_cluster_shows_count_in_summary()
    {
        var features = MakeFeatures(
            ("t1", "Test 1", ExecutionResult.Failed, "Connection refused"),
            ("t2", "Test 2", ExecutionResult.Failed, "Connection refused"),
            ("t3", "Test 3", ExecutionResult.Failed, "Connection refused"));
        var content = GenerateReport(features, "ClusterCount.html");
        Assert.Contains("3 scenarios", content);
    }

    [Fact]
    public void Report_cluster_contains_cluster_key_text()
    {
        var features = MakeFeatures(
            ("t1", "Test 1", ExecutionResult.Failed, "Connection refused"),
            ("t2", "Test 2", ExecutionResult.Failed, "Connection refused"));
        var content = GenerateReport(features, "ClusterKey.html");
        Assert.Contains("Connection refused", content);
    }

    [Fact]
    public void Report_cluster_lists_affected_scenario_names()
    {
        var features = MakeFeatures(
            ("t1", "Test 1", ExecutionResult.Failed, "Connection refused"),
            ("t2", "Test 2", ExecutionResult.Failed, "Connection refused"));
        var content = GenerateReport(features, "ClusterScenarios.html");
        Assert.Contains("Test 1", content);
        Assert.Contains("Test 2", content);
    }

    [Fact]
    public void Report_cluster_across_features_groups_together()
    {
        var features = MakeMultiFeature(
            new Feature
            {
                DisplayName = "Feature A",
                Scenarios = [new Scenario { Id = "t1", DisplayName = "Test 1", Result = ExecutionResult.Failed, ErrorMessage = "Timeout" }]
            },
            new Feature
            {
                DisplayName = "Feature B",
                Scenarios = [new Scenario { Id = "t2", DisplayName = "Test 2", Result = ExecutionResult.Failed, ErrorMessage = "Timeout" }]
            });
        var content = GenerateReport(features, "ClusterAcrossFeatures.html");
        Assert.Contains("failure-clusters", content);
        Assert.Contains("2 scenarios", content);
    }
}
