using LightBDD.Core.ExecutionContext;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Provides a standard way to obtain the current test's name and ID for LightBDD.
/// </summary>
public static class CurrentTestInfo
{
    /// <summary>
    /// A delegate that returns the current LightBDD scenario's name and runtime ID.
    /// Assign to <c>CurrentTestInfoFetcher</c> on any tracking options class.
    /// </summary>
    public static Func<(string Name, string Id)> Fetcher { get; } =
        () =>
        {
            try
            {
                var scenario = ScenarioExecutionContext.CurrentScenario;
                return (scenario.Info.Name.ToString(), scenario.Info.RuntimeId.ToString());
            }
            catch
            {
                return ("Unknown", "unknown");
            }
        };
}
