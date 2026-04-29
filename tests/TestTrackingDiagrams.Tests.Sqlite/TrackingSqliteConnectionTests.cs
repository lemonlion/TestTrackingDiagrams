using Microsoft.Data.Sqlite;
using TestTrackingDiagrams.Extensions.Sqlite;
using TestTrackingDiagrams.Sql;
using TestTrackingDiagrams.Tracking;
using Xunit;

namespace TestTrackingDiagrams.Tests.Sqlite;

public class TrackingSqliteConnectionTests
{
    [Fact]
    public void ComponentName_contains_service_name()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        var options = new SqliteTrackingOptions { ServiceName = "MyDB" };
        using var tracking = new TrackingSqliteConnection(inner, options);
        Assert.Contains("MyDB", tracking.ComponentName);
    }

    [Fact]
    public void Default_options_use_sqlite_values()
    {
        var options = new SqliteTrackingOptions();
        Assert.Equal("SQLite", options.ServiceName);
        Assert.Equal("SQLite", options.DependencyCategory);
        Assert.Equal("sqlite", options.UriScheme);
    }

    [Fact]
    public void WasInvoked_is_false_initially()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions());
        Assert.False(tracking.WasInvoked);
        Assert.Equal(0, tracking.InvocationCount);
    }

    [Fact]
    public void Implements_ITrackingComponent()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions());
        Assert.IsAssignableFrom<ITrackingComponent>(tracking);
    }

    [Fact]
    public void InnerConnection_returns_original()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions());
        Assert.Same(inner, tracking.InnerConnection);
    }

    [Fact]
    public void Open_and_close_delegate_to_inner()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions());

        tracking.Open();
        Assert.Equal(System.Data.ConnectionState.Open, tracking.State);

        tracking.Close();
        Assert.Equal(System.Data.ConnectionState.Closed, tracking.State);
    }

    [Fact]
    public void CreateCommand_returns_tracking_command()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions());
        tracking.Open();

        using var cmd = tracking.CreateCommand();
        Assert.IsType<TrackingSqliteCommand>(cmd);
    }

    [Fact]
    public void ExecuteNonQuery_increments_invocation_count()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = new TrackingSqliteConnection(inner, new SqliteTrackingOptions
        {
            CurrentTestInfoFetcher = () => ("TestName", "TestId")
        });
        tracking.Open();

        using var cmd = tracking.CreateCommand();
        cmd.CommandText = "CREATE TABLE Test (Id INTEGER PRIMARY KEY)";
        cmd.ExecuteNonQuery();

        Assert.True(tracking.InvocationCount > 0);
    }
}

public class SqliteConnectionExtensionsTests
{
    [Fact]
    public void WithTestTracking_returns_tracking_connection()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        using var tracking = inner.WithTestTracking();
        Assert.IsType<TrackingSqliteConnection>(tracking);
        Assert.Same(inner, tracking.InnerConnection);
    }

    [Fact]
    public void WithTestTracking_accepts_custom_options()
    {
        using var inner = new SqliteConnection("Data Source=:memory:");
        var options = new SqliteTrackingOptions { ServiceName = "CustomSQLite" };
        using var tracking = inner.WithTestTracking(options);
        Assert.Contains("CustomSQLite", tracking.ComponentName);
    }
}
