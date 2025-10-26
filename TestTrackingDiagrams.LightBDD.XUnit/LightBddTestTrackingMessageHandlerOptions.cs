using LightBDD.Core.ExecutionContext;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.XUnit;

public record LightBddTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public LightBddTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () =>
        {
            var x = ScenarioExecutionContext.CurrentStep;
            return (ScenarioExecutionContext.CurrentScenario.Info.Name.ToString(),
                ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString(),
                ScenarioExecutionContext.CurrentStep.Info.Name.ToString(),
                ScenarioExecutionContext.CurrentStep.Info.Parent.Name.ToString());
        };
    }
}