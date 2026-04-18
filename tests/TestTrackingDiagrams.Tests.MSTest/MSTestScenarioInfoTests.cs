using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class MSTestScenarioInfoTests
{
    [TestMethod]
    public void ShouldStoreAllProperties()
    {
        var info = new MSTestScenarioInfo
        {
            TestClassSimpleName = "My_Test_Class",
            TestMethodName = "My_Test_Method",
            TestId = "Namespace.My_Test_Class.My_Test_Method",
            Outcome = UnitTestOutcome.Passed,
            Endpoint = "/api/cake",
            IsHappyPath = true,
            ErrorMessage = null,
            ErrorStackTrace = null
        };

        Assert.AreEqual("My_Test_Class", info.TestClassSimpleName);
        Assert.AreEqual("My_Test_Method", info.TestMethodName);
        Assert.AreEqual("Namespace.My_Test_Class.My_Test_Method", info.TestId);
        Assert.AreEqual(UnitTestOutcome.Passed, info.Outcome);
        Assert.AreEqual("/api/cake", info.Endpoint);
        Assert.IsTrue(info.IsHappyPath);
        Assert.IsNull(info.ErrorMessage);
        Assert.IsNull(info.ErrorStackTrace);
    }

    [TestMethod]
    public void ShouldDefaultOptionalPropertiesToNull()
    {
        var info = new MSTestScenarioInfo
        {
            TestClassSimpleName = "TestClass",
            TestMethodName = "TestMethod",
            TestId = "TestClass.TestMethod",
            Outcome = UnitTestOutcome.Passed
        };

        Assert.IsNull(info.Endpoint);
        Assert.IsFalse(info.IsHappyPath);
        Assert.IsNull(info.ErrorMessage);
        Assert.IsNull(info.ErrorStackTrace);
    }

    [TestMethod]
    public void ShouldStoreErrorInformation()
    {
        var info = new MSTestScenarioInfo
        {
            TestClassSimpleName = "TestClass",
            TestMethodName = "TestMethod",
            TestId = "TestClass.TestMethod",
            Outcome = UnitTestOutcome.Failed,
            ErrorMessage = "Assert.AreEqual failed. Expected: 1 Actual: 2",
            ErrorStackTrace = "at TestClass.TestMethod() in TestClass.cs:line 42"
        };

        Assert.AreEqual("Assert.AreEqual failed. Expected: 1 Actual: 2", info.ErrorMessage);
        Assert.AreEqual("at TestClass.TestMethod() in TestClass.cs:line 42", info.ErrorStackTrace);
    }
}
