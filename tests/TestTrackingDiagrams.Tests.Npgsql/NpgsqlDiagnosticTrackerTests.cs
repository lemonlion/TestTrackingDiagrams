using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.Npgsql;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.Tests.Npgsql;

public class NpgsqlDiagnosticTrackerTests
{
    [Fact]
    public void ComponentName_contains_service_name()
    {
        var options = new NpgsqlTrackingOptions { ServiceName = "MyPostgres" };
        var tracker = new NpgsqlDiagnosticTracker(options);
        Assert.Contains("MyPostgres", tracker.ComponentName);
    }

    [Fact]
    public void Default_options_use_postgresql_values()
    {
        var options = new NpgsqlTrackingOptions();
        Assert.Equal("PostgreSQL", options.ServiceName);
        Assert.Equal("PostgreSQL", options.DependencyCategory);
        Assert.Equal("postgresql", options.UriScheme);
    }

    [Fact]
    public void WasInvoked_is_false_initially()
    {
        var tracker = new NpgsqlDiagnosticTracker(new NpgsqlTrackingOptions());
        Assert.False(tracker.WasInvoked);
        Assert.Equal(0, tracker.InvocationCount);
    }

    [Fact]
    public void Implements_ITrackingComponent()
    {
        var tracker = new NpgsqlDiagnosticTracker(new NpgsqlTrackingOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracker);
    }

    [Fact]
    public void Subscribe_and_unsubscribe_do_not_throw()
    {
        var tracker = new NpgsqlDiagnosticTracker(new NpgsqlTrackingOptions());
        tracker.Subscribe();
        tracker.Unsubscribe();
    }
}

public class NpgsqlTrackingOptionsTests
{
    [Fact]
    public void Options_inherit_from_SqlTrackingOptionsBase()
    {
        var options = new NpgsqlTrackingOptions();
        Assert.IsAssignableFrom<SqlTrackingOptionsBase>(options);
    }

    [Fact]
    public void Default_verbosity_is_Detailed()
    {
        var options = new NpgsqlTrackingOptions();
        Assert.Equal(SqlTrackingVerbosityLevel.Detailed, options.Verbosity);
    }

    [Fact]
    public void ExcludedOperations_defaults_to_empty()
    {
        var options = new NpgsqlTrackingOptions();
        Assert.Empty(options.ExcludedOperations);
    }
}

public class NpgsqlTestTrackingTests
{
    [Fact]
    public void EnsureTracking_returns_tracker()
    {
        try
        {
            var tracker = NpgsqlTestTracking.EnsureTracking();
            Assert.NotNull(tracker);
            Assert.IsType<NpgsqlDiagnosticTracker>(tracker);
        }
        finally
        {
            NpgsqlTestTracking.Reset();
        }
    }

    [Fact]
    public void EnsureTracking_is_idempotent()
    {
        try
        {
            var tracker1 = NpgsqlTestTracking.EnsureTracking();
            var tracker2 = NpgsqlTestTracking.EnsureTracking();
            Assert.Same(tracker1, tracker2);
        }
        finally
        {
            NpgsqlTestTracking.Reset();
        }
    }
}

public class NpgsqlServiceCollectionExtensionsTests
{
    [Fact]
    public void AddPostgreSqlTestTracking_registers_singleton()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddPostgreSqlTestTracking();

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<NpgsqlDiagnosticTracker>();

        Assert.NotNull(tracker);
    }

    [Fact]
    public void AddPostgreSqlTestTracking_applies_configuration()
    {
        var services = new Microsoft.Extensions.DependencyInjection.ServiceCollection();
        services.AddPostgreSqlTestTracking(opts =>
        {
            opts.ServiceName = "CustomPostgres";
            opts.Verbosity = SqlTrackingVerbosityLevel.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var tracker = provider.GetService<NpgsqlDiagnosticTracker>();

        Assert.NotNull(tracker);
        Assert.Contains("CustomPostgres", tracker!.ComponentName);
    }
}
