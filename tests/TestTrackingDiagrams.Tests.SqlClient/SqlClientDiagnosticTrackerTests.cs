using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.SqlClient;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.Tests.SqlClient;

public class SqlClientDiagnosticTrackerTests
{
    [Fact]
    public void ComponentName_contains_service_name()
    {
        var options = new SqlClientTrackingOptions { ServiceName = "MySqlServer" };
        var tracker = new SqlClientDiagnosticTracker(options);
        Assert.Contains("MySqlServer", tracker.ComponentName);
    }

    [Fact]
    public void Default_options_use_sqlserver_values()
    {
        var options = new SqlClientTrackingOptions();
        Assert.Equal("SQL Server", options.ServiceName);
        Assert.Equal("SqlServer", options.DependencyCategory);
        Assert.Equal("sqlserver", options.UriScheme);
    }

    [Fact]
    public void WasInvoked_is_false_initially()
    {
        var tracker = new SqlClientDiagnosticTracker(new SqlClientTrackingOptions());
        Assert.False(tracker.WasInvoked);
        Assert.Equal(0, tracker.InvocationCount);
    }

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var tracker = new SqlClientDiagnosticTracker(new SqlClientTrackingOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracker);
    }

    [Fact]
    public void Subscribe_and_unsubscribe_do_not_throw()
    {
        var tracker = new SqlClientDiagnosticTracker(new SqlClientTrackingOptions());
        tracker.Subscribe();
        tracker.Unsubscribe();
    }
}

public class SqlClientServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqlServerTestTracking_registers_singleton()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSqlServerTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<SqlClientDiagnosticTracker>();

        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddSqlServerTestTracking_applies_configuration()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddSqlServerTestTracking(opts =>
        {
            opts.ServiceName = "CustomSqlServer";
            opts.Verbosity = SqlTrackingVerbosityLevel.Raw;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<SqlClientDiagnosticTracker>();

        Assert.NotNull(tracker);
        Assert.Contains("CustomSqlServer", tracker!.ComponentName);
    }
}

public class SqlClientTestTrackingTests
{
    [Fact]
    public void EnsureTracking_returns_tracker()
    {
        try
        {
            var tracker = SqlClientTestTracking.EnsureTracking();
            Assert.NotNull(tracker);
        }
        finally
        {
            SqlClientTestTracking.Reset();
        }
    }

    [Fact]
    public void EnsureTracking_is_idempotent()
    {
        try
        {
            var t1 = SqlClientTestTracking.EnsureTracking();
            var t2 = SqlClientTestTracking.EnsureTracking();
            Assert.Same(t1, t2);
        }
        finally
        {
            SqlClientTestTracking.Reset();
        }
    }
}
