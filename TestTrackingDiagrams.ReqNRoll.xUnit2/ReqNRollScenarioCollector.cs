using System.Collections.Concurrent;

namespace TestTrackingDiagrams.ReqNRoll.xUnit2;

public static class ReqNRollScenarioCollector
{
    private static readonly ConcurrentQueue<ReqNRollScenarioInfo> Scenarios = new();

    public static void Collect(ReqNRollScenarioInfo info) => Scenarios.Enqueue(info);
    public static ReqNRollScenarioInfo[] GetAll() => Scenarios.ToArray();

    public static DateTime StartRunTime { get; set; }
    public static DateTime EndRunTime { get; set; }
}
