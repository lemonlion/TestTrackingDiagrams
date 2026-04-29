using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestTrackingDiagrams.MSTest;

/// <summary>
/// Allows manual override of diagram generation settings for specific MSTest tests.
/// </summary>
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

    public static void StartSetup()
    {
        DefaultTrackingDiagramOverride.StartSetup(GetTestId());
    }

    private static string GetTestId()
    {
        var ctx = DiagrammedComponentTest.GetCurrentTestContext();
        return $"{ctx!.FullyQualifiedTestClassName}.{ctx.TestName}";
    }
}