using System.Reflection;
using Xunit.v3;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// Captures TestMethodArguments before the LightBDD scenario method runs.
/// LightBDD receives raw parameter objects (records, lists, etc.) via [MemberData],
/// but its result API only exposes FormattedValue (ToString()). This attribute
/// captures the raw objects so the report generator can render them with full fidelity.
/// Apply to the LightBddScope class or at assembly level.
/// </summary>
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CaptureLightBddArgumentsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        object?[]? args = null;

        if (test is XunitTest xunitTest)
            args = xunitTest.TestMethodArguments;

        if (args is null or { Length: 0 } && test.TestCase is XunitTestCase testCase)
            args = testCase.TestMethodArguments;

        if (args is not { Length: > 0 })
            return;

        var parameters = methodUnderTest.GetParameters();
        if (parameters.Length == 0 || parameters.Length != args.Length)
            return;

        var paramNames = parameters.Select(p => p.Name ?? $"arg{p.Position}").ToArray();
        var formattedValues = args.Select(a => a?.ToString() ?? "").ToArray();

        CapturedScenarioArguments.Capture(paramNames, formattedValues, args);
    }
}
