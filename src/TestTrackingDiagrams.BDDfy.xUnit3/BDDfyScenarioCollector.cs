using System.Collections.Concurrent;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

/// <summary>
/// Thread-safe collector that accumulates BDDfy scenario execution information for report generation.
/// </summary>
public static class BDDfyScenarioCollector
{
    private static readonly ConcurrentQueue<BDDfyScenarioInfo> Scenarios = new();

    public static void Collect(BDDfyScenarioInfo info) => Scenarios.Enqueue(info);
    public static BDDfyScenarioInfo[] GetAll() => Scenarios.ToArray();

    public static DateTime StartRunTime { get; set; }
    public static DateTime EndRunTime { get; set; }
}