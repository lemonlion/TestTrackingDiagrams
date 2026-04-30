using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class TestIdentityScopeTests
{
    [Fact]
    public void Current_is_null_when_no_scope_active()
    {
        TestIdentityScope.Reset();

        Assert.Null(TestIdentityScope.Current);
    }

    [Fact]
    public void Begin_sets_current_identity()
    {
        using (TestIdentityScope.Begin("MyTest", "test-123"))
        {
            Assert.NotNull(TestIdentityScope.Current);
            Assert.Equal("MyTest", TestIdentityScope.Current.Value.Name);
            Assert.Equal("test-123", TestIdentityScope.Current.Value.Id);
        }
    }

    [Fact]
    public void Dispose_restores_previous_null()
    {
        TestIdentityScope.Reset();

        using (TestIdentityScope.Begin("MyTest", "test-123"))
        {
            Assert.NotNull(TestIdentityScope.Current);
        }

        Assert.Null(TestIdentityScope.Current);
    }

    [Fact]
    public void Dispose_restores_previous_non_null()
    {
        using (TestIdentityScope.Begin("Outer", "outer-id"))
        {
            using (TestIdentityScope.Begin("Inner", "inner-id"))
            {
                Assert.Equal("Inner", TestIdentityScope.Current!.Value.Name);
                Assert.Equal("inner-id", TestIdentityScope.Current.Value.Id);
            }

            Assert.Equal("Outer", TestIdentityScope.Current!.Value.Name);
            Assert.Equal("outer-id", TestIdentityScope.Current.Value.Id);
        }
    }

    [Fact]
    public void Reset_clears_identity()
    {
        using (TestIdentityScope.Begin("MyTest", "test-123"))
        {
            TestIdentityScope.Reset();
            Assert.Null(TestIdentityScope.Current);
        }
    }

    [Fact]
    public async Task Scope_flows_through_await()
    {
        using (TestIdentityScope.Begin("AsyncTest", "async-id"))
        {
            await Task.Yield();
            Assert.NotNull(TestIdentityScope.Current);
            Assert.Equal("AsyncTest", TestIdentityScope.Current.Value.Name);
        }
    }

    [Fact]
    public async Task Scope_flows_into_new_thread()
    {
        using (TestIdentityScope.Begin("ThreadTest", "thread-id"))
        {
            (string Name, string Id)? capturedInThread = null;
            var thread = new Thread(() => capturedInThread = TestIdentityScope.Current);
            thread.Start();
            thread.Join();

            // AsyncLocal propagates via ExecutionContext capture on Thread.Start()
            Assert.NotNull(capturedInThread);
            Assert.Equal("ThreadTest", capturedInThread.Value.Name);
        }
    }

    [Fact]
    public async Task Scope_flows_through_Task_Run()
    {
        using (TestIdentityScope.Begin("TaskRunTest", "taskrun-id"))
        {
            var result = await Task.Run(() => TestIdentityScope.Current);

            // AsyncLocal DOES flow into Task.Run (it's on the ExecutionContext)
            Assert.NotNull(result);
            Assert.Equal("TaskRunTest", result.Value.Name);
        }
    }

    [Fact]
    public void UnknownTestName_is_Unknown()
    {
        Assert.Equal("Unknown", TestIdentityScope.UnknownTestName);
    }

    [Fact]
    public void UnknownTestId_is_unknown()
    {
        Assert.Equal("unknown", TestIdentityScope.UnknownTestId);
    }

    [Fact]
    public void UnknownIdentity_tuple_matches_individual_constants()
    {
        var (name, id) = TestIdentityScope.UnknownIdentity;
        Assert.Equal(TestIdentityScope.UnknownTestName, name);
        Assert.Equal(TestIdentityScope.UnknownTestId, id);
    }
}
