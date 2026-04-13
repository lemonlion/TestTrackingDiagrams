using Microsoft.VisualStudio.TestTools.UnitTesting;
using TestTrackingDiagrams.MSTest;
using TestTrackingDiagrams.Reports;

namespace TestTrackingDiagrams.Tests.MSTest;

[TestClass]
public class TestOutcomeExtensionsTests
{
    [TestMethod]
    public void Passed_ShouldMapToExecutionResultPassed()
    {
        Assert.AreEqual(ExecutionResult.Passed, UnitTestOutcome.Passed.ToExecutionResult());
    }

    [TestMethod]
    public void Failed_ShouldMapToExecutionResultFailed()
    {
        Assert.AreEqual(ExecutionResult.Failed, UnitTestOutcome.Failed.ToExecutionResult());
    }

    [TestMethod]
    public void Error_ShouldMapToExecutionResultFailed()
    {
        Assert.AreEqual(ExecutionResult.Failed, UnitTestOutcome.Error.ToExecutionResult());
    }

    [TestMethod]
    public void Timeout_ShouldMapToExecutionResultFailed()
    {
        Assert.AreEqual(ExecutionResult.Failed, UnitTestOutcome.Timeout.ToExecutionResult());
    }

    [TestMethod]
    public void Aborted_ShouldMapToExecutionResultFailed()
    {
        Assert.AreEqual(ExecutionResult.Failed, UnitTestOutcome.Aborted.ToExecutionResult());
    }

    [TestMethod]
    public void Inconclusive_ShouldMapToExecutionResultSkipped()
    {
        Assert.AreEqual(ExecutionResult.Skipped, UnitTestOutcome.Inconclusive.ToExecutionResult());
    }

    [TestMethod]
    public void InProgress_ShouldMapToExecutionResultSkipped()
    {
        Assert.AreEqual(ExecutionResult.Skipped, UnitTestOutcome.InProgress.ToExecutionResult());
    }

    [TestMethod]
    public void NotRunnable_ShouldMapToExecutionResultSkipped()
    {
        Assert.AreEqual(ExecutionResult.Skipped, UnitTestOutcome.NotRunnable.ToExecutionResult());
    }

    [TestMethod]
    public void Unknown_ShouldMapToExecutionResultSkipped()
    {
        Assert.AreEqual(ExecutionResult.Skipped, UnitTestOutcome.Unknown.ToExecutionResult());
    }
}
