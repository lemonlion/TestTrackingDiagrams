using System.Collections.Concurrent;
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

    private static readonly ConcurrentQueue<string> DiagnosticEntries = new();

    /// <summary>
    /// When <c>true</c>, records diagnostic entries for assertion value resolution fallbacks.
    /// </summary>
    public static bool DiagnosticMode { get; set; }

    /// <summary>
    /// Diagnostic log entries recorded when <see cref="DiagnosticMode"/> is <c>true</c>.
    /// Contains details about closure value resolution fallbacks.
    /// </summary>
    public static IReadOnlyCollection<string> DiagnosticLog => DiagnosticEntries.ToArray();

    /// <summary>
    /// Clears all diagnostic log entries.
    /// </summary>
    public static void ClearDiagnosticLog() => DiagnosticEntries.Clear();

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
        [CallerArgumentExpression(nameof(assertion))] string? expression = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            assertion();
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: true, resolvedValues: resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
        }
        catch (Exception ex)
        {
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: false, ex.Message, resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
            throw;
        }
    }

    /// <summary>
    /// Executes an assertion function, tracks it in the sequence diagram, and returns the value.
    /// </summary>
    public static T That<T>(
        Func<T> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            var result = assertion();
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: true, resolvedValues: resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
            return result;
        }
        catch (Exception ex)
        {
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: false, ex.Message, resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
            throw;
        }
    }

    /// <summary>
    /// Executes an async assertion and tracks it in the sequence diagram.
    /// </summary>
    public static async Task ThatAsync(
        Func<Task> assertion,
        [CallerArgumentExpression(nameof(assertion))] string? expression = null,
        [CallerFilePath] string? callerFilePath = null,
        [CallerLineNumber] int callerLineNumber = 0)
    {
        try
        {
            await assertion();
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: true, resolvedValues: resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
        }
        catch (Exception ex)
        {
            var resolved = ResolveClosureValues(assertion, expression);
            LogAssertion(expression, passed: false, ex.Message, resolved,
                callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
            throw;
        }
    }

    /// <summary>
    /// Called by the IL weaver (AssertionWeaver) to log a passing assertion.
    /// Unlike <see cref="That(Action, string?, string?, int)"/>, this does NOT wrap
    /// the assertion in a lambda — the original code runs unchanged with full semantic
    /// fidelity (null propagation, ref params, etc.).
    /// </summary>
    public static void AssertionPassed(string expression, string? callerFilePath = null, int callerLineNumber = 0)
    {
        LogAssertion(expression, passed: true, callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
    }

    /// <summary>
    /// Called by the IL weaver (AssertionWeaver) to log a failing assertion.
    /// Unlike <see cref="That(Action, string?, string?, int)"/>, this does NOT wrap
    /// the assertion in a lambda — the original code runs unchanged with full semantic
    /// fidelity (null propagation, ref params, etc.).
    /// </summary>
    public static void AssertionFailed(string expression, string failureMessage, string? callerFilePath = null, int callerLineNumber = 0)
    {
        LogAssertion(expression, passed: false, failureMessage, callerFilePath: callerFilePath, callerLineNumber: callerLineNumber);
    }

    private static Dictionary<string, string>? ResolveClosureValues(Delegate assertion, string? expression)
    {
        try
        {
            var result = ClosureValueResolver.ResolveValues(assertion, expression);

            if (DiagnosticMode && result.Fallbacks.Count > 0)
            {
                foreach (var (fieldName, reason) in result.Fallbacks)
                    DiagnosticEntries.Enqueue(
                        $"[Track.That] Value resolution fallback: '{fieldName}' — {reason} (expression: {expression})");
            }

            return result.ResolvedValues.Count > 0 ? result.ResolvedValues : null;
        }
        catch
        {
            // Resolution must never break assertion tracking
            return null;
        }
    }

    private static void LogAssertion(string? expression, bool passed, string? failureMessage = null,
        Dictionary<string, string>? resolvedValues = null, string? callerFilePath = null, int callerLineNumber = 0)
    {
        var testId = ResolveTestId();
        if (testId is null)
            return;

        var formatted = AssertionExpressionFormatter.Format(expression, resolvedValues);
        if (string.IsNullOrEmpty(formatted))
            formatted = expression ?? "assertion";

        var color = passed ? PassColor : FailColor;
        var symbol = passed ? PassSymbol : FailSymbol;

        var noteContent = passed
            ? $"{symbol} {formatted}"
            : $"{symbol} {formatted}\n{failureMessage}";

        var plantUml = $"hnote across <<assertionNote>> {color}\n{noteContent}\nend note";

        // Append source location as a PlantUML comment (invisible in rendered output)
        if (!string.IsNullOrEmpty(callerFilePath))
        {
            // Use LastIndexOfAny to handle both / and \ separators cross-platform
            // (Path.GetFileName doesn't recognize \ on Linux)
            var separatorIndex = callerFilePath.LastIndexOfAny(['/', '\\']);
            var fileName = separatorIndex >= 0 ? callerFilePath[(separatorIndex + 1)..] : callerFilePath;
            plantUml += $"\n'__assertionLoc__:{fileName}:L{callerLineNumber}";
        }

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
