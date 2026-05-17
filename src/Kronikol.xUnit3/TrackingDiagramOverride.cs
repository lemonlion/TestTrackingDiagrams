using Kronikol.Tracking;
using Xunit;

namespace Kronikol.xUnit3;

/// <summary>
/// Allows manual override of diagram generation settings for specific xUnit v3 tests.
/// </summary>
public static class TrackingDiagramOverride
{
    public static void StartOverride(string? plantUml = null)
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.StartOverride(testId, plantUml);
    }

    public static void EndOverride(string? plantUml = null)
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.EndOverride(testId, plantUml);
    }

    public static void InsertTestDelimiter(string testIdentifier)
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.InsertTestDelimiter(testId, testIdentifier);
    }

    public static void InsertPlantUml(string plantUml)
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.InsertPlantUml(testId, plantUml);
    }

    public static void StartAction()
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.StartAction(testId);
    }

    public static void StartSetup()
    {
        var testId = GetTestId();
        if (testId is null) return;
        DefaultTrackingDiagramOverride.StartSetup(testId);
    }

    private static string? GetTestId()
    {
        try
        {
            var id = TestContext.Current.Test?.UniqueID;
            if (id is not null) return id;
        }
        catch
        {
            // TestContext.Current throws on non-test threads — fall through to ambient scope
        }

        return (TestIdentityScope.Current ?? TestIdentityScope.GlobalFallback)?.Id;
    }
}