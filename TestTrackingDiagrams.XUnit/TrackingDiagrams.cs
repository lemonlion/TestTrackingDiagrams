using Xunit;

namespace TestTrackingDiagrams.XUnit;

public static class TrackingDiagrams
{
    public static void StartOverride(string? plantUml = null)
    {
        DefaultTrackingDiagrams.StartOverride(GetTestId(), plantUml);
    }

    public static void EndOverride(string? plantUml = null)
    {
        DefaultTrackingDiagrams.EndOverride(GetTestId(), plantUml);
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        DefaultTrackingDiagrams.InsertTestDelimiter(GetTestId(), testIdentifier);
    }

    public static void InsertPlantUml(string plantUml)
    {
        DefaultTrackingDiagrams.InsertPlantUml(GetTestId(), plantUml);
    }

    private static string GetTestId() => TestContext.Current.Test!.UniqueID;
}