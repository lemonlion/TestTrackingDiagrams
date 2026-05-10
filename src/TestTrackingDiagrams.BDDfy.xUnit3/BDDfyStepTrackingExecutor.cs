using TestStack.BDDfy;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

internal class BDDfyStepTrackingExecutor(IStepExecutor inner) : IStepExecutor
{
    private static readonly AsyncLocal<string?> CurrentStepTypeLocal = new();

    public static string? CurrentStepType => CurrentStepTypeLocal.Value;

    public object Execute(Step step, object testObject)
    {
        var keyword = step.ExecutionOrder switch
        {
            ExecutionOrder.SetupState or ExecutionOrder.ConsecutiveSetupState => "Given",
            ExecutionOrder.Transition or ExecutionOrder.ConsecutiveTransition => "When",
            ExecutionOrder.Assertion or ExecutionOrder.ConsecutiveAssertion => "Then",
            _ => null
        };

        CurrentStepTypeLocal.Value = keyword?.ToUpperInvariant();
        TestPhaseContext.Current = PhaseConfiguration.ResolvePhaseFromStepType(keyword);

        // Resolve the xUnit test ID for StepCollector
        string? testId = null;
        try { testId = Xunit.TestContext.Current.Test?.UniqueID; } catch { /* not in xUnit context */ }

        var stepText = ExtractStepText(step.Title, keyword);
        if (testId is not null)
            StepCollector.StartStep(testId, keyword, stepText, null, null);

        try
        {
            return inner.Execute(step, testObject);
        }
        catch (Exception)
        {
            if (testId is not null)
                StepCollector.CompleteStep(testId, passed: false);
            throw;
        }
        finally
        {
            if (testId is not null && StepCollector.HasActiveStep(testId))
                StepCollector.CompleteStep(testId, passed: true);
            CurrentStepTypeLocal.Value = null;
            TestPhaseContext.Current = TestPhase.Unknown;
        }
    }

    /// <summary>
    /// Extracts the step text from BDDfy's step title, stripping the keyword prefix
    /// (e.g. "Given a user exists" → "a user exists"). Falls back to the full title
    /// if no keyword prefix is present.
    /// </summary>
    private static string ExtractStepText(string title, string? keyword)
    {
        if (keyword is null)
            return title.Trim();

        var trimmed = title.Trim();
        if (trimmed.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            var afterKeyword = trimmed[keyword.Length..];
            if (afterKeyword.Length > 0 && afterKeyword[0] == ' ')
                return afterKeyword[1..];
        }

        return trimmed;
    }
}
