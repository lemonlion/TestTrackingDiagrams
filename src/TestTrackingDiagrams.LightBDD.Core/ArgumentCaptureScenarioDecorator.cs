using LightBDD.Core.Execution;
using LightBDD.Core.Extensibility;
using LightBDD.Core.Extensibility.Execution;

namespace TestTrackingDiagrams.LightBDD;

/// <summary>
/// Framework-agnostic LightBDD scenario decorator that captures raw test method argument values
/// from LightBDD's IScenario.Descriptor.Parameters during scenario execution.
/// This enables rich sub-table/expandable rendering for complex objects passed via framework-level
/// attributes (e.g. MemberData, ClassData, TestCase, TestCaseSource) — using the same processing
/// pipeline as the non-LightBDD adapters.
/// Registered automatically by CreateStandardReportsWithDiagrams().
/// </summary>
internal sealed class ArgumentCaptureScenarioDecorator : IScenarioDecorator
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
            var descriptor = scenario.Descriptor;
            if (descriptor?.Parameters is not { Length: > 0 } parameters)
                return;

            var fixture = scenario.Fixture;
            var paramNames = new string[parameters.Length];
            var rawValues = new object?[parameters.Length];
            var formattedValues = new string[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                paramNames[i] = parameters[i].RawName ?? $"arg{i}";

                // Evaluate the parameter value using the fixture context
                try
                {
                    rawValues[i] = parameters[i].ValueEvaluator(fixture);
                }
                catch
                {
                    rawValues[i] = null;
                }

                formattedValues[i] = rawValues[i]?.ToString() ?? "";
            }

            CapturedScenarioArguments.Capture(paramNames, formattedValues, rawValues);
        }
        catch
        {
            // Silently fail — capture is best-effort
        }
    }
}
