using System.Collections.Concurrent;

namespace TestTrackingDiagrams.ReqNRoll;

/// <summary>
/// Thread-safe collector that accumulates Reqnroll scenario execution information for report generation.
/// </summary>
public static class ReqNRollScenarioCollector
{
    private static readonly ConcurrentQueue<ReqNRollScenarioInfo> Scenarios = new();

    public static void Collect(ReqNRollScenarioInfo info) => Scenarios.Enqueue(info);
    public static ReqNRollScenarioInfo[] GetAll() => Scenarios.ToArray();

    public static DateTime StartRunTime { get; set; }
    public static DateTime EndRunTime { get; set; }
}