using System.Reflection;
using Reqnroll;

namespace Kronikol.Tests;

public class ReqNRollBindingDiscoveryTests
{
    [Fact]
    public void XUnit2_assembly_contains_binding_annotated_types()
    {
        var assembly = typeof(Kronikol.ReqNRoll.ReqNRollTrackingHooksXUnit2).Assembly;
        var bindingTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<BindingAttribute>() is not null)
            .ToList();

        Assert.True(bindingTypes.Count >= 2,
            $"Expected at least 2 [Binding] types in {assembly.GetName().Name}, found: {string.Join(", ", bindingTypes.Select(t => t.Name))}");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTrackingHooksXUnit2");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTestRunHooksXUnit2");
    }

    [Fact]
    public void XUnit3_assembly_contains_binding_annotated_types()
    {
        var assembly = typeof(Kronikol.ReqNRoll.ReqNRollTrackingHooksXUnit3).Assembly;
        var bindingTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<BindingAttribute>() is not null)
            .ToList();

        Assert.True(bindingTypes.Count >= 2,
            $"Expected at least 2 [Binding] types in {assembly.GetName().Name}, found: {string.Join(", ", bindingTypes.Select(t => t.Name))}");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTrackingHooksXUnit3");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTestRunHooksXUnit3");
    }

    [Fact]
    public void TUnit_assembly_contains_binding_annotated_types()
    {
        var assembly = typeof(Kronikol.ReqNRoll.ReqNRollTrackingHooksTUnit).Assembly;
        var bindingTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<BindingAttribute>() is not null)
            .ToList();

        Assert.True(bindingTypes.Count >= 2,
            $"Expected at least 2 [Binding] types in {assembly.GetName().Name}, found: {string.Join(", ", bindingTypes.Select(t => t.Name))}");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTrackingHooksTUnit");
        Assert.Contains(bindingTypes, t => t.Name == "ReqNRollTestRunHooksTUnit");
    }

    [Fact]
    public void Derived_tracking_hooks_inherit_from_base()
    {
        Assert.True(typeof(Kronikol.ReqNRoll.ReqNRollTrackingHooksXUnit2).IsSubclassOf(typeof(Kronikol.ReqNRoll.ReqNRollTrackingHooks)));
        Assert.True(typeof(Kronikol.ReqNRoll.ReqNRollTestRunHooksXUnit2).IsSubclassOf(typeof(Kronikol.ReqNRoll.ReqNRollTestRunHooks)));
    }
}
