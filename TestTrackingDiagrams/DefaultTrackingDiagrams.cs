using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;
public static class DefaultTrackingDiagrams
{
    public static void StartOverride(string testId, string? plantUml = null)
        => Override(testId, TestTrackingLogType.OverrideStart, plantUml);

    public static void EndOverride(string testId, string? plantUml = null)
        => Override(testId, TestTrackingLogType.OverrideEnd, plantUml);

    public static void InsertPlantUml(string testId, string plantUml)
    {
        StartOverride(testId, plantUml);
        EndOverride(testId);
    }

    public static void InsertTestDelimiter(string testRuntimeId, string testIdentifier)
    {
        InsertPlantUml(testRuntimeId, $"hnote across #black:<color:white>Test {testIdentifier}");
    }

    public static void StartAction(string testId)
        => Override(testId, TestTrackingLogType.ActionStart);

    public static void EndAction(string testId)
        => Override(testId, TestTrackingLogType.ActionEnd);

    private static void Override(string testId, TestTrackingLogType type, string? plantUml = null)
    {
        var log = new TestTrackingLog(
            testId,
            testId,
            "",
            "",
            new Uri("http://override.com"),
            [],
            "",
            "",
            type,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false,
            PlantUml: ToBufferedPlantUml(plantUml));
        TestTracker.Log(log);
    }

    private static string? ToBufferedPlantUml(string? plantUml) => plantUml is null
        ? null
        : $"""

           {plantUml}

           """;
}