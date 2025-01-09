using LightBDD.Core.ExecutionContext;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.XUnit;

public record LightBddTestTrackingMessageHandlerOptions : TestTrackingMessageHandlerOptions
{
    public LightBddTestTrackingMessageHandlerOptions()
    {
        CurrentTestInfoFetcher = () => (ScenarioExecutionContext.CurrentScenario.Info.Name.ToString(), ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString());
    }
}