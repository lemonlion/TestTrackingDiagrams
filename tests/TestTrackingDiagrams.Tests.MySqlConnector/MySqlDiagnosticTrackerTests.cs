using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.MySqlConnector;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.Tests.MySqlConnector;

public class MySqlDiagnosticTrackerTests
{
    [Fact]
    public void ComponentName_contains_service_name()
    {
        var options = new MySqlTrackingOptions { ServiceName = "MyMySQL" };
        var tracker = new MySqlDiagnosticTracker(options);
        Assert.Contains("MyMySQL", tracker.ComponentName);
    }

    [Fact]
    public void Default_options_use_mysql_values()
    {
        var options = new MySqlTrackingOptions();
        Assert.Equal("MySQL", options.ServiceName);
        Assert.Equal("MySQL", options.DependencyCategory);
        Assert.Equal("mysql", options.UriScheme);
    }

    [Fact]
    public void WasInvoked_is_false_initially()
    {
        var tracker = new MySqlDiagnosticTracker(new MySqlTrackingOptions());
        Assert.False(tracker.WasInvoked);
    }

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var tracker = new MySqlDiagnosticTracker(new MySqlTrackingOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracker);
    }

    [Fact]
    public void Subscribe_and_unsubscribe_do_not_throw()
    {
        var tracker = new MySqlDiagnosticTracker(new MySqlTrackingOptions());
        tracker.Subscribe();
        tracker.Unsubscribe();
    }
}

public class MySqlServiceCollectionExtensionsTests
{
    [Fact]
    public void AddMySqlTestTracking_registers_singleton()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddMySqlTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<MySqlDiagnosticTracker>();

        Assert.NotNull(tracker);
    }
}

public class MySqlTestTrackingTests
{
    [Fact]
    public void EnsureTracking_returns_tracker()
    {
        try
        {
            var tracker = MySqlTestTracking.EnsureTracking();
            Assert.NotNull(tracker);
        }
        finally
        {
            MySqlTestTracking.Reset();
        }
    }

    [Fact]
    public void EnsureTracking_is_idempotent()
    {
        try
        {
            var t1 = MySqlTestTracking.EnsureTracking();
            var t2 = MySqlTestTracking.EnsureTracking();
            Assert.Same(t1, t2);
        }
        finally
        {
            MySqlTestTracking.Reset();
        }
    }
}
