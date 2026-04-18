using TestStack.BDDfy;

namespace TestTrackingDiagrams.BDDfy.xUnit3;

internal class BDDfyStepTrackingExecutor(IStepExecutor inner) : IStepExecutor
{
    private static readonly AsyncLocal<string?> CurrentStepTypeLocal = new();

    public static string? CurrentStepType => CurrentStepTypeLocal.Value;

    public object Execute(Step step, object testObject)
    {
        CurrentStepTypeLocal.Value = step.ExecutionOrder switch
        {
            ExecutionOrder.SetupState or ExecutionOrder.ConsecutiveSetupState => "GIVEN",
            ExecutionOrder.Transition or ExecutionOrder.ConsecutiveTransition => "WHEN",
            ExecutionOrder.Assertion or ExecutionOrder.ConsecutiveAssertion => "THEN",
            _ => null
        };

        try
        {
            return inner.Execute(step, testObject);
        }
        finally
        {
            CurrentStepTypeLocal.Value = null;
        }
    }
}
