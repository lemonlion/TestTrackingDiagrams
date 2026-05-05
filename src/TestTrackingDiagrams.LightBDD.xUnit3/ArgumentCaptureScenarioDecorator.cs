using System.Reflection;
using LightBDD.Core.Execution;
using LightBDD.Core.Extensibility.Execution;
using Xunit;
using Xunit.v3;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// LightBDD scenario decorator that automatically captures raw test method arguments
/// from xUnit3's TestContext during scenario execution. This eliminates the need for
/// the explicit [assembly: CaptureLightBddArguments] attribute.
/// Registered automatically by CreateStandardReportsWithDiagrams().
/// </summary>
internal sealed class ArgumentCaptureScenarioDecorator : IScenarioDecorator
{
    public Task ExecuteAsync(IScenario scenario, Func<Task> scenarioInvocation)
    {
        TryCaptureArguments();
        return scenarioInvocation();
    }

    private static void TryCaptureArguments()
    {
        try
        {
            var testContext = TestContext.Current;
            if (testContext?.Test is not XunitTest xunitTest)
                return;

            var args = xunitTest.TestMethodArguments;
            if (args is not { Length: > 0 })
            {
                if (testContext.Test.TestCase is XunitTestCase testCase)
                    args = testCase.TestMethodArguments;
            }

            if (args is not { Length: > 0 })
                return;

            // Get parameter names from the test method
            var method = xunitTest.TestMethod?.Method as MethodInfo;
            if (method is null && testContext.Test.TestCase is XunitTestCase tc)
                method = tc.TestMethod?.Method as MethodInfo;

            if (method is null)
                return;

            var parameters = method.GetParameters();
            if (parameters.Length == 0 || parameters.Length != args.Length)
                return;

            var paramNames = new string[parameters.Length];
            for (var i = 0; i < parameters.Length; i++)
                paramNames[i] = parameters[i].Name ?? $"arg{i}";

            var formattedValues = new string[args.Length];
            for (var i = 0; i < args.Length; i++)
                formattedValues[i] = args[i]?.ToString() ?? "";

            CapturedScenarioArguments.Capture(paramNames, formattedValues, args!);
        }
        catch
        {
            // Silently fail — capture is best-effort
        }
    }
}
