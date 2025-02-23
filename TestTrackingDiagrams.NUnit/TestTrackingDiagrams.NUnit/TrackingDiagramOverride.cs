using NUnit.Framework;

namespace TestTrackingDiagrams.NUnit;

public static class TrackingDiagrams
{
    public static void StartSetup()
    {
        DefaultTrackingDiagrams.StartAction(GetTestId());
    }

    public static void EndSetup()
    {
        DefaultTrackingDiagrams.EndAction(GetTestId());
    }

    public static void StartOverride(string? plantUml = null)
    {
        DefaultTrackingDiagrams.StartOverride(GetTestId(), plantUml);
    }

    public static void EndOverride(string? plantUml = null)
    {
        DefaultTrackingDiagrams.EndOverride(GetTestId(), plantUml);
    }

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagrams.InsertPlantUml(GetTestId(), plantUml);
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagrams.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    private static string GetTestId() => TestContext.CurrentContext.Test.ID;
}