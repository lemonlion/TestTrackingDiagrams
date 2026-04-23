using LightBDD.Core.ExecutionContext;
using LightBDD.Core.Metadata;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD;

public record LightBddTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public LightBddTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (ScenarioExecutionContext.CurrentScenario.Info.Name.ToString(), ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString());
        CurrentStepTypeFetcher = GetTopLevelStepType;
    }

    private static string? GetTopLevelStepType()
    {
        var step = ScenarioExecutionContext.CurrentStep;
        if (step is null)
            return null;

        var info = step.Info;
        while (info.Parent is IStepInfo parentStep)
            info = parentStep;

        var stepType = info.Name?.StepTypeName?.Name;
        TestPhaseContext.Current = PhaseConfiguration.ResolvePhaseFromStepType(stepType);
        return stepType;
    }
}
