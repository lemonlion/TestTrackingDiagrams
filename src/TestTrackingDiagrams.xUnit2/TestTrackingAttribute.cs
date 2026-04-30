using System.Reflection;
using TestTrackingDiagrams.Tracking;
using Xunit.Sdk;

namespace TestTrackingDiagrams.xUnit2;

/// <summary>
/// xUnit v2 <see cref="BeforeAfterTestAttribute"/> that sets the current test identity
/// in <see cref="AsyncLocal{T}"/> before each test runs, and collects test metadata for reports.
/// <para>
/// Apply this attribute to individual test classes, or apply it at the assembly level
/// to cover all tests: <c>[assembly: TestTracking]</c>.
/// </para>
/// <para>
/// When using <see cref="DiagrammedComponentTest"/> as a base class, this attribute
/// is already applied automatically.
/// </para>
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method | AttributeTargets.Assembly, AllowMultiple = false)]
public sealed class TestTrackingAttribute : BeforeAfterTestAttribute
{
    public override void Before(MethodInfo methodUnderTest)
    {
        var testId = Guid.NewGuid().ToString();
        var className = (methodUnderTest.ReflectedType ?? methodUnderTest.DeclaringType)?.Name ?? TestIdentityScope.UnknownTestName;
        var methodName = methodUnderTest.Name;

        var featureName = ScenarioTitleResolver.FormatFeatureName(className);
        var scenarioName = ScenarioTitleResolver.FormatScenarioDisplayName(methodName);

        XUnit2TestTrackingContext.SetCurrentTest($"{className}.{methodName}", testId);

        var endpointAttr = (methodUnderTest.ReflectedType ?? methodUnderTest.DeclaringType)?
            .GetCustomAttributes(inherit: true)
            .OfType<EndpointAttribute>()
            .FirstOrDefault();

        var isHappyPath = methodUnderTest
            .GetCustomAttributes(inherit: true)
            .OfType<HappyPathAttribute>()
            .Any();

        var methodMatchKey = $"{(methodUnderTest.ReflectedType ?? methodUnderTest.DeclaringType)!.FullName}.{methodUnderTest.Name}";

        XUnit2TestTrackingContext.CollectedScenarios[testId] = new ScenarioInfo
        {
            Id = testId,
            FeatureName = featureName,
            ScenarioName = scenarioName,
            MethodMatchKey = methodMatchKey,
            Endpoint = endpointAttr?.Endpoint,
            IsHappyPath = isHappyPath,
        };
    }

    public override void After(MethodInfo methodUnderTest)
    {
        XUnit2TestTrackingContext.ClearCurrentTest();
    }
}
