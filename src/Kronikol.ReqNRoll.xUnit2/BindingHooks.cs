using Reqnroll;

namespace Kronikol.ReqNRoll;

/// <summary>
/// Framework-assembly-resident binding hooks for Reqnroll discovery.
/// Reqnroll scans assemblies listed in bindingAssemblies for [Binding] classes
/// but does not follow TypeForwardedTo attributes. This subclass ensures hooks
/// are discoverable from this assembly.
/// </summary>
[Binding]
public class ReqNRollTrackingHooksXUnit2 : ReqNRollTrackingHooks
{
    public ReqNRollTrackingHooksXUnit2(ScenarioContext scenarioContext, FeatureContext featureContext)
        : base(scenarioContext, featureContext) { }
}

[Binding]
public class ReqNRollTestRunHooksXUnit2 : ReqNRollTestRunHooks
{
    [BeforeTestRun(Order = int.MinValue)]
    public static new void BeforeTestRun() => ReqNRollTestRunHooks.BeforeTestRun();

    [AfterTestRun(Order = int.MinValue)]
    public static new void AfterTestRun() => ReqNRollTestRunHooks.AfterTestRun();
}
