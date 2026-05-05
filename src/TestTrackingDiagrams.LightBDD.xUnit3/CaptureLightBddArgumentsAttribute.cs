using System.Reflection;
using TestTrackingDiagrams.xUnit3;
using Xunit.v3;

namespace TestTrackingDiagrams.LightBDD.xUnit3;

/// <summary>
/// Captures TestMethodArguments before the LightBDD scenario method runs.
/// LightBDD receives raw parameter objects (records, lists, etc.) via [MemberData],
/// but its result API only exposes FormattedValue (ToString()). This attribute
/// captures the raw objects so the report generator can render them with full fidelity.
/// Apply to the LightBddScope class or at assembly level.
/// </summary>
[Obsolete("Use configuration.CreateStandardReportsWithDiagrams(options) which registers automatic argument capture without requiring this attribute.")]
[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Class | AttributeTargets.Method)]
public sealed class CaptureLightBddArgumentsAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        var (args, paramNames) = XUnit3ArgumentExtractor.Extract(test);

        if (args is not { Length: > 0 } || paramNames is not { Length: > 0 })
            return;

        var formattedValues = args.Select(a => a?.ToString() ?? "").ToArray();

        CapturedScenarioArguments.Capture(paramNames, formattedValues, args);
    }
}
