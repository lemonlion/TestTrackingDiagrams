using TestTrackingDiagrams.Tests.Dapper.Fakes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Dapper;

public class DbConnectionExtensionsTests : IDisposable
{
    public DbConnectionExtensionsTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void WithTestTracking_returns_TrackingDbConnection()
    {
        var fakeConn = new FakeDbConnection();
        var options = new DapperTrackingOptions();

        using var tracked = fakeConn.WithTestTracking(options);

        Assert.IsType<TrackingDbConnection>(tracked);
    }

    [Fact]
    public void WithTestTracking_preserves_inner_connection()
    {
        var fakeConn = new FakeDbConnection();
        var options = new DapperTrackingOptions();

        using var tracked = fakeConn.WithTestTracking(options);

        Assert.Same(fakeConn, tracked.InnerConnection);
    }

    [Fact]
    public void WithTestTracking_uses_provided_options()
    {
        var fakeConn = new FakeDbConnection();
        var options = new DapperTrackingOptions { ServiceName = "CustomDb" };

        using var tracked = fakeConn.WithTestTracking(options);

        Assert.Equal("TrackingDbConnection (CustomDb)", tracked.ComponentName);
    }
}
