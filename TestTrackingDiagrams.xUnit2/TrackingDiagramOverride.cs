namespace TestTrackingDiagrams.xUnit2;

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

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagramOverride.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagramOverride.InsertPlantUml(GetTestId(), plantUml);
    }

    public static void StartAction()
    {
        DefaultTrackingDiagramOverride.StartAction(GetTestId());
    }

    private static string GetTestId() => XUnit2TestTrackingContext.GetCurrentTestInfo().Id;
}
