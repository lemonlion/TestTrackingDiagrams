using LightBDD.Core.Execution;
using LightBDD.Core.ExecutionContext;
using LightBDD.Core.Extensibility.Execution;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// LightBDD step decorator that brackets each step with <see cref="StepCollector.StartStep"/>
/// and <see cref="StepCollector.CompleteStep"/>, enabling step delimiters in sequence diagrams
/// and assertion sub-step attachment during step execution.
/// Registered automatically by <c>CreateStandardReportsWithDiagrams()</c>.
/// </summary>
internal sealed class StepTrackingStepDecorator : IStepDecorator
{
    public async Task ExecuteAsync(IStep step, Func<Task> stepInvocation)
    {
        var keyword = step.Info.Name.StepTypeName?.OriginalName;
        var text = step.Info.Name.ToString();

        // Strip keyword prefix from the full text if present (LightBDD includes it)
        if (keyword is not null && text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
            text = text[keyword.Length..].TrimStart();

        string? testId = null;
        try { testId = ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString(); }
        catch { /* not in scenario context */ }

        if (testId is not null)
            StepCollector.StartStep(testId, keyword, text, null, null);

        try
        {
            await stepInvocation();
        }
        catch
        {
            if (testId is not null)
                StepCollector.CompleteStep(testId, passed: false);
            throw;
        }
        finally
        {
            if (testId is not null && StepCollector.HasActiveStep(testId))
                StepCollector.CompleteStep(testId, passed: true);
        }
    }
}
