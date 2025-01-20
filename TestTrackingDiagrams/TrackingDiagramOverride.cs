using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;
public static class DefaultTrackingDiagramOverride
{
    public static void StartOverride(string testId, string? plantUml = null)
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
            RequestResponseType.Request,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false)
        {
            IsOverrideStart = true,
            PlantUml = ToBufferedPlantUml(plantUml)
        };
        RequestResponseLogger.Log(log);
    }

    public static void EndOverride(string testId, string? plantUml = null)
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
            RequestResponseType.Request,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false)
        {
            IsOverrideEnd = true,
            PlantUml = ToBufferedPlantUml(plantUml)
        };
        RequestResponseLogger.Log(log);
    }

    private static string? ToBufferedPlantUml(string? plantUml) => plantUml is null
        ? null
        : $"""

           {plantUml}


           """;

    public static void InsertPlantUml(string testId, string plantUml)
    {
        StartOverride(testId, plantUml);
        EndOverride(testId);
    }

    public static void InsertTestDelimiter(string testRuntimeId, string testIdentifier)
    {
        StartOverride(testRuntimeId, $"hnote across #black:<color:white>Test {testIdentifier}");
        EndOverride(testRuntimeId);
    }
}