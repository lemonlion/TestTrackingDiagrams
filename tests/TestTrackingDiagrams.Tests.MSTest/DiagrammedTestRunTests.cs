using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class DiagrammedTestRunTests
{
    [TestMethod]
    public void TestContexts_ShouldBeInitialized()
    {
        Assert.IsNotNull(DiagrammedTestRun.TestContexts);
    }

    [TestMethod]
    public void TestContexts_ShouldAcceptMSTestScenarioInfo()
    {
        var info = new MSTestScenarioInfo
        {
            TestClassSimpleName = "TestClass",
            TestMethodName = "TestMethod",
            TestId = "TestClass.TestMethod",
            Outcome = UnitTestOutcome.Passed
        };

        DiagrammedTestRun.TestContexts.Enqueue(info);

        Assert.IsTrue(DiagrammedTestRun.TestContexts.TryDequeue(out var dequeued));
        Assert.AreEqual("TestClass", dequeued.TestClassSimpleName);
    }
}
