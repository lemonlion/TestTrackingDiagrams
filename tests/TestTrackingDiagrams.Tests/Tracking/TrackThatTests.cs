using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class TrackThatTests : IDisposable
{
    private const string TestId = "TrackThatTests.TestMethod";

    public TrackThatTests()
    {
        RequestResponseLogger.Clear();
    }

    public void Dispose()
    {
        TestIdentityScope.Reset();
        TestIdentityScope.ClearGlobalFallback();
        RequestResponseLogger.Clear();
    }

    private static List<RequestResponseLog> GetAssertionLogs() =>
        RequestResponseLogger.RequestAndResponseLogs
            .Where(l => l.PlantUml is not null && l.PlantUml.Contains("<<assertionNote>>"))
            .ToList();

    [Fact]
    public void That_passed_assertion_logs_green_note_with_assertionNote_stereotype()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#d4edda", logs[0].PlantUml!);
        Assert.Contains("\u2713", logs[0].PlantUml!); // ✓
    }

    [Fact]
    public void That_failed_assertion_logs_red_note_and_rethrows()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var ex = Assert.Throws<Exception>(() =>
            Track.That(() => throw new Exception("Test failure")));

        Assert.Equal("Test failure", ex.Message);

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#f8d7da", logs[0].PlantUml!);
        Assert.Contains("\u2717", logs[0].PlantUml!); // ✗
        Assert.Contains("Test failure", logs[0].PlantUml!);
    }

    [Fact]
    public async Task ThatAsync_passed_assertion_logs_green_note()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        await Track.ThatAsync(async () =>
        {
            await Task.Yield();
            Assert.True(true);
        });

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#d4edda", logs[0].PlantUml!);
    }

    [Fact]
    public async Task ThatAsync_failed_assertion_logs_red_note_and_rethrows()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(async () =>
            await Track.ThatAsync(async () =>
            {
                await Task.Yield();
                throw new InvalidOperationException("Async failure");
            }));

        Assert.Equal("Async failure", ex.Message);

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#f8d7da", logs[0].PlantUml!);
    }

    [Fact]
    public void That_with_return_value_returns_value_and_logs()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var result = Track.That(() => 42);

        Assert.Equal(42, result);
        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#d4edda", logs[0].PlantUml!);
    }

    [Fact]
    public void That_without_test_context_still_executes_assertion()
    {
        // No TestIdentityScope.Begin — no test context
        TestIdentityScope.Reset();

        var executed = false;
        Track.That(() => executed = true);

        Assert.True(executed);
    }

    [Fact]
    public void That_without_test_context_rethrows_on_failure()
    {
        TestIdentityScope.Reset();

        Assert.Throws<Exception>(() =>
            Track.That(() => throw new Exception("No context failure")));
    }

    [Fact]
    public void That_formats_expression_in_plantuml_note()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        // The expression should be formatted — at minimum it should contain some text
        Assert.Contains("Assert.True(true)", logs[0].PlantUml!);
    }
}
