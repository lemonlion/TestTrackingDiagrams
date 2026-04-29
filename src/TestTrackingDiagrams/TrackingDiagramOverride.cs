using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;
/// <summary>
/// Provides methods to inject custom PlantUML fragments into a test's sequence diagram,
/// and to mark the boundary between Setup and Action test phases.
/// </summary>
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

    public static void StartAction(string testId)
    {
        TestPhaseContext.Current = TestPhase.Action;
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
            IsActionStart = true
        };
        RequestResponseLogger.Log(log);
    }

    public static void StartSetup(string testId)
    {
        TestPhaseContext.Current = TestPhase.Setup;
    }

    // Delegate-based overloads for framework adapters
    public static void StartOverride(Func<string> getTestId, string? plantUml = null) => StartOverride(getTestId(), plantUml);
    public static void EndOverride(Func<string> getTestId, string? plantUml = null) => EndOverride(getTestId(), plantUml);
    public static void InsertPlantUml(Func<string> getTestId, string plantUml) => InsertPlantUml(getTestId(), plantUml);
    public static void InsertTestDelimiter(Func<string> getTestId, string testIdentifier) => InsertTestDelimiter(getTestId(), testIdentifier);
    public static void StartAction(Func<string> getTestId) => StartAction(getTestId());
    public static void StartSetup(Func<string> getTestId) => StartSetup(getTestId());
}