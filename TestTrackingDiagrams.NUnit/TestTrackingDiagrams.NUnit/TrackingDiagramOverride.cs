using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

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

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagramOverride.InsertPlantUml(GetTestId(), plantUml);
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagramOverride.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    private static string GetTestId() => TestContext.CurrentContext.Test.ID;
}