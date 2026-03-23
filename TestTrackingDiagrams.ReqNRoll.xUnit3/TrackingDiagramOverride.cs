namespace TestTrackingDiagrams.ReqNRoll.xUnit3;

public static class TrackingDiagramOverride
{
    public static void StartOverride(string? plantUml = null)
    {
        DefaultTrackingDiagramOverride.StartOverride(GetTestId(), plantUml);
    }

    public static void EndOverride(string? plantUml = null)
    {
        DefaultTrackingDiagramOverride.EndOverride(GetTestId(), plantUml);
    }

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagramOverride.InsertPlantUml(GetTestId(), plantUml);
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagramOverride.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    public static void StartAction()
    {
        DefaultTrackingDiagramOverride.StartAction(GetTestId());
    }

    private static string GetTestId() =>
        ReqNRollTestContext.CurrentTestInfo?.Id
        ?? throw new InvalidOperationException("No ReqNRoll scenario is currently executing.");
}
