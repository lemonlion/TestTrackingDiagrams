using System.Collections.Concurrent;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

public static class BDDfyScenarioCollector
{
    private static readonly ConcurrentQueue<BDDfyScenarioInfo> Scenarios = new();

    public static void Collect(BDDfyScenarioInfo info) => Scenarios.Enqueue(info);
    public static BDDfyScenarioInfo[] GetAll() => Scenarios.ToArray();

    public static DateTime StartRunTime { get; set; }
    public static DateTime EndRunTime { get; set; }
}
