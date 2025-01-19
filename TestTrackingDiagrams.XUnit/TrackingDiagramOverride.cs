using Xunit;

namespace TestTrackingDiagrams.XUnit;

public static class TrackingDiagramOverride
{
    public static void StartOverrideSummary(string plantUml)
    {
        DefaultTrackingDiagramOverride.StartOverrideSummary(GetTestId(), plantUml);
    }

    public static void EndOverrideSummary()
    {
        DefaultTrackingDiagramOverride.EndOverrideSummary(GetTestId());
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagramOverride.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagramOverride.InsertPlantUml(GetTestId(), plantUml);
    }

    private static string GetTestId() => TestContext.Current.Test!.UniqueID;
}