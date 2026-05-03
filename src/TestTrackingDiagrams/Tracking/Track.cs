using System.Runtime.CompilerServices;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Tracks assertions as inline notes in the sequence diagram.
/// Captures the assertion expression via <c>[CallerArgumentExpression]</c>,
/// formats it into a readable sentence, and injects it as a styled
/// <c>hnote across</c> entry in the PlantUML source.
/// </summary>
public static class Track
{
    private const string PassColor = "#d4edda";
    private const string FailColor = "#f8d7da";
    private const string PassSymbol = "\u2713"; // ✓
    private const string FailSymbol = "\u2717"; // ✗

    /// <summary>
    /// Optional delegate that resolves the current test ID from a framework-specific context
    /// (e.g. LightBDD's <c>ScenarioExecutionContext</c>, xUnit's <c>TestContext</c>).
    /// Checked before <see cref="TestIdentityScope.Current"/> and <see cref="TestIdentityScope.GlobalFallback"/>.
    /// </summary>
    public static Func<string?>? TestIdResolver { get; set; }

    /// <summary>
    /// Executes an assertion action and tracks it in the sequence diagram.
    /// On success, logs a green assertion note. On failure, logs a red note and re-throws.
    /// </summary>
    public static void That(
        Action assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        try
        {
            assertion();
            LogAssertion(expression, passed: true);
        }
        catch (Exception ex)
        {
            LogAssertion(expression, passed: false, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes an assertion function, tracks it in the sequence diagram, and returns the value.
    /// </summary>
    public static T That<T>(
        Func<T> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        try
        {
            var result = assertion();
            LogAssertion(expression, passed: true);
            return result;
        }
        catch (Exception ex)
        {
            LogAssertion(expression, passed: false, ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Executes an async assertion and tracks it in the sequence diagram.
    /// </summary>
    public static async Task ThatAsync(
        Func<Task> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null)
    {
        try
        {
            await assertion();
            LogAssertion(expression, passed: true);
        }
        catch (Exception ex)
        {
            LogAssertion(expression, passed: false, ex.Message);
            throw;
        }
    }

    private static void LogAssertion(string? expression, bool passed, string? failureMessage = null)
    {
        var testId = ResolveTestId();
        if (testId is null)
            return;

        var formatted = AssertionExpressionFormatter.Format(expression);
        if (string.IsNullOrEmpty(formatted))
            formatted = expression ?? "assertion";

        var color = passed ? PassColor : FailColor;
        var symbol = passed ? PassSymbol : FailSymbol;

        var noteContent = passed
            ? $"{symbol} {formatted}"
            : $"{symbol} {formatted}\n{failureMessage}";

        var plantUml = $"hnote across <<assertionNote>> {color}\n{noteContent}\nend note";

        DefaultTrackingDiagramOverride.InsertPlantUml(testId, plantUml);
    }

    private static string? ResolveTestId()
    {
        try
        {
            var resolved = TestIdResolver?.Invoke();
            if (resolved is not null)
                return resolved;
        }
        catch
        {
            // Resolver threw (e.g. no active scenario context) — fall through
        }

        var identity = TestIdentityScope.Current;
        if (identity is not null)
            return identity.Value.Id;

        var fallback = TestIdentityScope.GlobalFallback;
        if (fallback is not null)
            return fallback.Value.Id;

        return null;
    }
}
