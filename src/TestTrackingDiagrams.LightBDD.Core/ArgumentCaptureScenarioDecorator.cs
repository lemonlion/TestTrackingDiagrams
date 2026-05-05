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
/// Registered automatically by CreateStandardReportsWithDiagrams() when no framework-specific
/// decorator is available. Framework-specific decorators (xUnit3, TUnit) should call
/// <see cref="TryCaptureFromDescriptor"/> as a fallback when their native extraction fails.
/// </summary>
internal sealed class ArgumentCaptureScenarioDecorator : IScenarioDecorator
{
    public Task ExecuteAsync(IScenario scenario, Func<Task> scenarioInvocation)
    {
        TryCaptureFromDescriptor(scenario);
        return scenarioInvocation();
    }

    /// <summary>
    /// Attempts to capture raw arguments from LightBDD's IScenario.Descriptor.Parameters.
    /// Called directly by this decorator and also available as a fallback for framework-specific decorators.
    /// </summary>
    internal static void TryCaptureFromDescriptor(IScenario scenario)
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
