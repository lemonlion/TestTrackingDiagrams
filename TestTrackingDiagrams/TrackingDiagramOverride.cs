using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams;
public static class DefaultTrackingDiagramOverride
{
    public static void StartOverrideSummary(string testId, string plantUml)
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
            IsOverrideSummary = true,
            PlantUml = $"""
                        
                        {plantUml}
                        
                        
                        """
        };
        RequestResponseLogger.Log(log);
    }

    public static void EndOverrideSummary(string testId)
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
            IsOverrideEnding = true,
        };
        RequestResponseLogger.Log(log);
    }

    public static void InsertPlantUml(string testId, string plantUml)
    {
        StartOverrideSummary(testId, plantUml);
        EndOverrideSummary(testId);
    }

    public static void InsertTestDelimiter(string testRuntimeId, string testIdentifier)
    {
        StartOverrideSummary(testRuntimeId, $"hnote across #black:<color:white>Test {testIdentifier}");
        EndOverrideSummary(testRuntimeId);
    }
}