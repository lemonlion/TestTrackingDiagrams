using TestTrackingDiagrams.xUnit2;

namespace TestTrackingDiagrams.Tests.xUnit2;

// --- Test hierarchy: abstract base with a method, two concrete subclasses ---
public abstract class AbstractBaseTests
{
    public void SomeTestMethod() { }
}

public class ConcreteAlphaTests : AbstractBaseTests;
public class ConcreteBetaTests : AbstractBaseTests;

public class TestTrackingAttributeInheritanceTests : IDisposable
{
    public TestTrackingAttributeInheritanceTests()
    {
        XUnit2TestTrackingContext.CollectedScenarios.Clear();
    }

    public void Dispose()
    {
        XUnit2TestTrackingContext.CollectedScenarios.Clear();
    }

    [Fact]
    public void Inherited_test_should_use_concrete_class_name_as_feature()
    {
        // Arrange: get the MethodInfo as xUnit would discover it — via the concrete subclass
        var concreteMethod = typeof(ConcreteAlphaTests).GetMethod(nameof(AbstractBaseTests.SomeTestMethod))!;

        // Sanity check: DeclaringType (the bug) returns the abstract base
        Assert.Equal(nameof(AbstractBaseTests), concreteMethod.DeclaringType!.Name);
        // ReflectedType returns the concrete class
        Assert.Equal(nameof(ConcreteAlphaTests), concreteMethod.ReflectedType!.Name);

        // Act: simulate what TestTrackingAttribute.Before() does
        var attribute = new TestTrackingAttribute();
        attribute.Before(concreteMethod);

        // Assert: the feature name should come from the concrete class, not the abstract base
        var scenario = XUnit2TestTrackingContext.CollectedScenarios.Values.Single();
        Assert.Equal("Concrete Alpha Tests", scenario.FeatureName);
    }

    [Fact]
    public void Two_concrete_subclasses_should_produce_different_features()
    {
        var alphaMethod = typeof(ConcreteAlphaTests).GetMethod(nameof(AbstractBaseTests.SomeTestMethod))!;
        var betaMethod = typeof(ConcreteBetaTests).GetMethod(nameof(AbstractBaseTests.SomeTestMethod))!;

        var attribute = new TestTrackingAttribute();
        attribute.Before(alphaMethod);
        attribute.Before(betaMethod);

        var features = XUnit2TestTrackingContext.CollectedScenarios.Values
            .Select(s => s.FeatureName)
            .Distinct()
            .OrderBy(f => f)
            .ToArray();

        Assert.Equal(2, features.Length);
        Assert.Equal("Concrete Alpha Tests", features[0]);
        Assert.Equal("Concrete Beta Tests", features[1]);
    }

    [Fact]
    public void Endpoint_attribute_on_concrete_class_should_be_found()
    {
        var method = typeof(ConcreteWithEndpointTests).GetMethod(nameof(AbstractBaseTests.SomeTestMethod))!;

        var attribute = new TestTrackingAttribute();
        attribute.Before(method);

        var scenario = XUnit2TestTrackingContext.CollectedScenarios.Values.Single();
        Assert.Equal("/api/concrete", scenario.Endpoint);
    }
}

[Endpoint("/api/concrete")]
public class ConcreteWithEndpointTests : AbstractBaseTests;
