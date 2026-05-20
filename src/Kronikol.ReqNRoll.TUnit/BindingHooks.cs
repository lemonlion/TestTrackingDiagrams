using Reqnroll;

namespace Kronikol.ReqNRoll;

/// <summary>
/// Framework-assembly-resident binding hooks for Reqnroll discovery.
/// Reqnroll scans assemblies listed in bindingAssemblies for [Binding] classes
/// but does not follow TypeForwardedTo attributes. This subclass ensures hooks
/// are discoverable from this assembly.
/// </summary>
[Binding]
public class ReqNRollTrackingHooksTUnit : ReqNRollTrackingHooks
{
    public ReqNRollTrackingHooksTUnit(ScenarioContext scenarioContext, FeatureContext featureContext)
        : base(scenarioContext, featureContext) { }
}

[Binding]
public class ReqNRollTestRunHooksTUnit : ReqNRollTestRunHooks
{
    [BeforeTestRun(Order = int.MinValue)]
    public static new void BeforeTestRun() => ReqNRollTestRunHooks.BeforeTestRun();

    [AfterTestRun(Order = int.MinValue)]
    public static new void AfterTestRun() => ReqNRollTestRunHooks.AfterTestRun();
}
