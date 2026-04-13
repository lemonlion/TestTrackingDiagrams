using TestTrackingDiagrams.TUnit;
using TUnit.Core;

namespace TestTrackingDiagrams.Tests.TUnit;

public class TestStateExtensionsTests
{
    [Fact]
    public void PassedShouldMapToExecutionResultPassed()
    {
        var result = TestState.Passed.ToExecutionResult();

        Assert.Equal(Reports.ExecutionResult.Passed, result);
    }

    [Fact]
    public void FailedShouldMapToExecutionResultFailed()
    {
        var result = TestState.Failed.ToExecutionResult();

        Assert.Equal(Reports.ExecutionResult.Failed, result);
    }

    [Fact]
    public void SkippedShouldMapToExecutionResultSkipped()
    {
        var result = TestState.Skipped.ToExecutionResult();

        Assert.Equal(Reports.ExecutionResult.Skipped, result);
    }

    [Fact]
    public void TimeoutShouldMapToExecutionResultFailed()
    {
        var result = TestState.Timeout.ToExecutionResult();

        Assert.Equal(Reports.ExecutionResult.Failed, result);
    }

    [Fact]
    public void CancelledShouldMapToExecutionResultSkipped()
    {
        var result = TestState.Cancelled.ToExecutionResult();

        Assert.Equal(Reports.ExecutionResult.Skipped, result);
    }

    [Theory]
    [InlineData(TestState.NotStarted)]
    [InlineData(TestState.WaitingForDependencies)]
    [InlineData(TestState.Queued)]
    [InlineData(TestState.Running)]
    public void UnexpectedStateShouldThrow(TestState state)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => state.ToExecutionResult());
    }
}
