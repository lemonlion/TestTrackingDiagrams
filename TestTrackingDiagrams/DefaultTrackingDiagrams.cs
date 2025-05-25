using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;
public static class DefaultTrackingDiagrams
{
    public static void StartOverride(string testId, string? plantUml = null)
        => Override(testId, RequestResponseType.OverrideStart, plantUml);

    public static void EndOverride(string testId, string? plantUml = null)
        => Override(testId, RequestResponseType.OverrideEnd, plantUml);

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
        => Override(testId, RequestResponseType.ActionStart);

    public static void EndAction(string testId)
        => Override(testId, RequestResponseType.ActionEnd);

    private static void Override(string testId, RequestResponseType type, string? plantUml = null)
    {
        var log = new RequestResponseLog(
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
        RequestResponseLogger.Log(log);
    }

    private static string? ToBufferedPlantUml(string? plantUml) => plantUml is null
        ? null
        : $"""

           {plantUml}

           """;
}