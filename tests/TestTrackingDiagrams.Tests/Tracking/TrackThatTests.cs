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
        Track.TestIdResolver = null;
        Track.DiagnosticMode = false;
        Track.ClearDiagnosticLog();
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

    [Fact]
    public void That_uses_TestIdResolver_when_no_identity_scope()
    {
        TestIdentityScope.Reset();
        Track.TestIdResolver = () => TestId;

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("#d4edda", logs[0].PlantUml!);
    }

    [Fact]
    public void That_TestIdResolver_is_checked_before_identity_scope()
    {
        const string resolverId = "resolver-id";
        const string scopeId = "scope-id";
        using var scope = TestIdentityScope.Begin("scope-test", scopeId);
        Track.TestIdResolver = () => resolverId;

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Equal(resolverId, logs[0].TestId);
    }

    [Fact]
    public void That_falls_back_to_identity_scope_when_resolver_returns_null()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);
        Track.TestIdResolver = () => null;

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Equal(TestId, logs[0].TestId);
    }

    [Fact]
    public void That_falls_back_to_identity_scope_when_resolver_throws()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);
        Track.TestIdResolver = () => throw new InvalidOperationException("No scenario context");

        Track.That(() => Assert.True(true));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Equal(TestId, logs[0].TestId);
    }

    [Fact]
    public void That_resolves_captured_variable_value_in_assertion_note()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var expected = "hello-world";
        // CallerArgumentExpression will be: () => "x".Should().Be(expected)
        Track.That(() => "x".Should().Be(expected));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        // The resolved value should appear in the PlantUML note
        Assert.Contains("'hello-world'", logs[0].PlantUml!);
    }

    [Fact]
    public void That_falls_back_to_source_text_for_computed_expression()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var maxOrders = 5;
        // CallerArgumentExpression: () => "x".Should().Be(maxOrders - 1)
        Track.That(() => "x".Should().Be(maxOrders - 1));

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        // Should NOT contain a resolved value with quotes for maxOrders — fallback to source text
        Assert.DoesNotContain("'5'", logs[0].PlantUml!);
    }

    [Fact]
    public void That_DiagnosticMode_logs_fallback_reason()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);
        Track.DiagnosticMode = true;

        try
        {
            var maxOrders = 5;
            Track.That(() => "x".Should().Be(maxOrders - 1));

            Assert.NotEmpty(Track.DiagnosticLog);
            Assert.Contains(Track.DiagnosticLog, l => l.Contains("computed expression"));
        }
        finally
        {
            Track.DiagnosticMode = false;
            Track.ClearDiagnosticLog();
        }
    }

    [Fact]
    public void That_DiagnosticMode_off_does_not_log_fallback()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);
        Track.DiagnosticMode = false;

        var maxOrders = 5;
        Track.That(() => "x".Should().Be(maxOrders - 1));

        Assert.Empty(Track.DiagnosticLog);
    }

    [Fact]
    public async Task ThatAsync_resolves_captured_variable_value()
    {
        using var scope = TestIdentityScope.Begin(TestId, TestId);

        var expected = "async-value";
        await Track.ThatAsync(async () =>
        {
            await Task.Yield();
            "x".Should().Be(expected);
        });

        var logs = GetAssertionLogs();
        Assert.NotEmpty(logs);
        Assert.Contains("'async-value'", logs[0].PlantUml!);
    }
}

/// <summary>
/// Minimal FluentAssertions-like helpers for testing Track.That value resolution.
/// These produce the .Should().Be(args) pattern in CallerArgumentExpression text.
/// </summary>
internal static class FakeFluentExtensions
{
    public static FakeShouldResult Should(this object _) => new();
}

internal class FakeShouldResult
{
    public void Be(object? _) { }
}
