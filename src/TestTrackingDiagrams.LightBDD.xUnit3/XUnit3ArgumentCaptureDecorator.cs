using LightBDD.Core.Execution;
using LightBDD.Core.Extensibility.Execution;
using TestTrackingDiagrams.LightBDD;
using TestTrackingDiagrams.xUnit3;
using Xunit;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// xUnit v3-specific LightBDD scenario decorator that captures raw test method arguments
/// using <see cref="XUnit3ArgumentExtractor"/> from the xUnit3 test context.
/// Falls back to the generic LightBDD parameter descriptor extraction if the framework
/// extraction fails (e.g. when TestContext is unavailable).
/// </summary>
internal sealed class XUnit3ArgumentCaptureDecorator : IScenarioDecorator
{
    public Task ExecuteAsync(IScenario scenario, Func<Task> scenarioInvocation)
    {
        TryCaptureArguments(scenario);
        return scenarioInvocation();
    }

    private static void TryCaptureArguments(IScenario scenario)
    {
        try
        {
            // Try framework-specific extraction from xUnit3's TestContext first
            var (args, paramNames) = XUnit3ArgumentExtractor.Extract(TestContext.Current);

            if (args is { Length: > 0 } && paramNames is { Length: > 0 })
            {
                var formattedValues = new string[args.Length];
                for (var i = 0; i < args.Length; i++)
                    formattedValues[i] = args[i]?.ToString() ?? "";

                CapturedScenarioArguments.Capture(paramNames, formattedValues, args);
                return;
            }
        }
        catch
        {
            // Framework extraction failed — fall through to generic extraction
        }

        // Fallback: use LightBDD's parameter descriptor extraction
        ArgumentCaptureScenarioDecorator.TryCaptureFromDescriptor(scenario);
    }
}
