using Microsoft.VisualStudio.TestTools.UnitTesting;
using Kronikol.MSTest;

namespace Kronikol.Tests.MSTest;

[TestClass]
public class HappyPathAttributeTests
{
    [TestMethod]
    public void ShouldHaveHappyPathPropertyKeyConstant()
    {
        Assert.AreEqual("Happy Path", HappyPathAttribute.HappyPathPropertyKey);
    }

    [TestMethod]
    public void ShouldOnlyTargetMethods()
    {
        var usageAttribute = (AttributeUsageAttribute)Attribute.GetCustomAttribute(
            typeof(HappyPathAttribute), typeof(AttributeUsageAttribute))!;

        Assert.AreEqual(AttributeTargets.Method, usageAttribute.ValidOn);
    }

    [TestMethod]
    public void ShouldBeInstantiable()
    {
        var attribute = new HappyPathAttribute();

        Assert.IsNotNull(attribute);
    }
}
