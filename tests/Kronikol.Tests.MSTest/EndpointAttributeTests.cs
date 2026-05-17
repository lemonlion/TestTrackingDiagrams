using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kronikol.MSTest;

namespace Kronikol.Tests.MSTest;

[TestClass]
public class EndpointAttributeTests
{
    [TestMethod]
    public void ShouldStoreEndpointValue()
    {
        var attribute = new EndpointAttribute("/api/cake");

        Assert.AreEqual("/api/cake", attribute.Endpoint);
    }

    [TestMethod]
    public void ShouldHaveEndpointPropertyKeyConstant()
    {
        Assert.AreEqual("Endpoint", EndpointAttribute.EndpointPropertyKey);
    }

    [TestMethod]
    public void ShouldOnlyTargetClasses()
    {
        var usageAttribute = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(EndpointAttribute), typeof(AttributeUsageAttribute))!;

        Assert.AreEqual(AttributeTargets.Class, usageAttribute.ValidOn);
    }
}
