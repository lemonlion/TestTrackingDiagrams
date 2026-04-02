using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class TestOutcomeExtensionsTests
{
    [TestMethod]
    public void Passed_ShouldMapToScenarioResultPassed()
    {
        Assert.AreEqual(ScenarioResult.Passed, UnitTestOutcome.Passed.ToScenarioResult());
    }

    [TestMethod]
    public void Failed_ShouldMapToScenarioResultFailed()
    {
        Assert.AreEqual(ScenarioResult.Failed, UnitTestOutcome.Failed.ToScenarioResult());
    }

    [TestMethod]
    public void Error_ShouldMapToScenarioResultFailed()
    {
        Assert.AreEqual(ScenarioResult.Failed, UnitTestOutcome.Error.ToScenarioResult());
    }

    [TestMethod]
    public void Timeout_ShouldMapToScenarioResultFailed()
    {
        Assert.AreEqual(ScenarioResult.Failed, UnitTestOutcome.Timeout.ToScenarioResult());
    }

    [TestMethod]
    public void Aborted_ShouldMapToScenarioResultFailed()
    {
        Assert.AreEqual(ScenarioResult.Failed, UnitTestOutcome.Aborted.ToScenarioResult());
    }

    [TestMethod]
    public void Inconclusive_ShouldMapToScenarioResultSkipped()
    {
        Assert.AreEqual(ScenarioResult.Skipped, UnitTestOutcome.Inconclusive.ToScenarioResult());
    }

    [TestMethod]
    public void InProgress_ShouldMapToScenarioResultSkipped()
    {
        Assert.AreEqual(ScenarioResult.Skipped, UnitTestOutcome.InProgress.ToScenarioResult());
    }

    [TestMethod]
    public void NotRunnable_ShouldMapToScenarioResultSkipped()
    {
        Assert.AreEqual(ScenarioResult.Skipped, UnitTestOutcome.NotRunnable.ToScenarioResult());
    }

    [TestMethod]
    public void Unknown_ShouldMapToScenarioResultSkipped()
    {
        Assert.AreEqual(ScenarioResult.Skipped, UnitTestOutcome.Unknown.ToScenarioResult());
    }
}
