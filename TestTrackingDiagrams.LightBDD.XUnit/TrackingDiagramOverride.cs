using LightBDD.Core.ExecutionContext;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.LightBDD.XUnit;
public static class TrackingDiagramOverride
{
    public static void StartOverrideSummary(string plantUml)
    {
        var log = new RequestResponseLog(
            ScenarioExecutionContext.CurrentScenario.Info.Name.ToString(),
            ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString(),
            "",
            "",
            new Uri("http://override.com"),
            [],
            "",
            "",
            RequestResponseType.Request,
            Guid.NewGuid(), 
            Guid.NewGuid(), 
            false)
        {
            IsOverrideSummary = true,
            PlantUml = $"""
                        
                        {plantUml}
                        
                        
                        """
        };
        RequestResponseLogger.Log(log);
    }

    public static void EndOverrideSummary()
    {
        var log = new RequestResponseLog(
            ScenarioExecutionContext.CurrentScenario.Info.Name.ToString(),
            ScenarioExecutionContext.CurrentScenario.Info.RuntimeId.ToString(),
            "",
            "",
            new Uri("http://override.com"),
            [],
            "",
            "",
            RequestResponseType.Request,
            Guid.NewGuid(),
            Guid.NewGuid(),
            false)
        {
            IsOverrideEnding = true,
        };
        RequestResponseLogger.Log(log);
    }
}