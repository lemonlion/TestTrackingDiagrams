using LightBDD.Core.ExecutionContext;
using LightBDD.Core.Metadata;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD;

public record LightBddTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    /// <summary>
    /// A reusable delegate that returns the current LightBDD scenario's name and runtime ID.
    /// Use this when configuring extension options (e.g. <c>SqlTrackingInterceptorOptions</c>)
    /// instead of writing the fetcher lambda inline.
    /// </summary>
    public static readonly Func<(string Name, string Id)> TestInfoFetcher = CurrentTestInfo.Fetcher;

    public LightBddTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = CurrentTestInfo.Fetcher;
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
