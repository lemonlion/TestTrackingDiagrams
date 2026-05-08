using TestTrackingDiagrams.Tracking;
using TestTrackingDiagrams.xUnit3;

namespace TestTrackingDiagrams.Tests.xUnit3.xUnit3;

public class TrackingDiagramOverrideTests
{
    [Fact]
    public void GetTestId_Falls_Back_To_GlobalFallback_When_TestContext_Unavailable()
    {
        // Simulates a pre-existing background thread (hosted service, change-feed processor)
        // that has NO xUnit TestContext and NO AsyncLocal scope — only GlobalFallback.
        var testId = $"global-{Guid.NewGuid()}";

        TestIdentityScope.SetGlobalFallback("GlobalTest", testId);
        try
        {
            // SuppressFlow prevents ALL AsyncLocal propagation (both xUnit's TestContext
            // and our TestIdentityScope.Current), leaving only GlobalFallback.
            using var suppressFlow = ExecutionContext.SuppressFlow();
            var thread = new Thread(() =>
            {
                TrackingDiagramOverride.InsertPlantUml("note over B: global");
            });
            thread.Start();
            thread.Join();
        }
        finally
        {
            TestIdentityScope.ClearGlobalFallback();
        }

        var logged = RequestResponseLogger.RequestAndResponseLogs
            .Any(l => l.TestId == testId && l.IsOverrideStart == true);
        Assert.True(logged, "InsertPlantUml should have logged with the GlobalFallback test ID");
    }

    [Fact]
    public void GetTestId_Falls_Back_To_TestIdentityScope_Current()
    {
        // Simulates a thread started from within a TestIdentityScope where xUnit's
        // TestContext.Test is null (e.g., a nested thread pool in a hosted service).
        var testId = $"scope-{Guid.NewGuid()}";

        // SuppressFlow prevents xUnit's TestContext from propagating.
        // We then manually set TestIdentityScope on the new thread.
        using var suppressFlow = ExecutionContext.SuppressFlow();
        var thread = new Thread(() =>
        {
            // On this thread, no xUnit TestContext and no inherited AsyncLocal.
            // Set TestIdentityScope.Begin here to simulate the test wrapping background
            // processing (or a hosted service that sets the scope from GlobalFallback info).
            using (TestIdentityScope.Begin("ScopeTest", testId))
            {
                TrackingDiagramOverride.InsertPlantUml("note over A: scoped");
            }
        });
        thread.Start();
        thread.Join();

        var logged = RequestResponseLogger.RequestAndResponseLogs
            .Any(l => l.TestId == testId && l.IsOverrideStart == true);
        Assert.True(logged, "InsertPlantUml should have logged with the TestIdentityScope.Current test ID");
    }
}
