using System.Data.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tests.Dapper.Fakes;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Dapper;

[Collection("TrackingComponentRegistry")]
public class DapperServiceCollectionExtensionsTests : IDisposable
{
    public DapperServiceCollectionExtensionsTests()
    {
        TrackingComponentRegistry.Clear();
    }

    public void Dispose()
    {
        TrackingComponentRegistry.Clear();
    }

    [Fact]
    public void AddDapperTestTracking_decorates_registered_connection()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(new FakeDbConnection());

        services.AddDapperTestTracking(options =>
        {
            options.ServiceName = "TestDb";
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<DbConnection>();

        Assert.IsType<TrackingDbConnection>(connection);
    }

    [Fact]
    public void AddDapperTestTracking_preserves_service_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<DbConnection>(_ => new FakeDbConnection());

        services.AddDapperTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(DbConnection));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddDapperTestTracking_does_not_duplicate_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(new FakeDbConnection());

        services.AddDapperTestTracking();

        Assert.Single(services, d => d.ServiceType == typeof(DbConnection));
    }

    [Fact]
    public void AddDapperTestTracking_is_noop_when_no_connection_registered()
    {
        var services = new ServiceCollection();

        services.AddDapperTestTracking();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(DbConnection));
    }

    [Fact]
    public void AddDapperTestTracking_applies_options_configuration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(new FakeDbConnection());

        services.AddDapperTestTracking(options =>
        {
            options.ServiceName = "CustomDb";
            options.CallingServiceName = "MySvc";
            options.Verbosity = DapperTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<DbConnection>();

        Assert.IsType<TrackingDbConnection>(connection);
    }

    [Fact]
    public void AddDapperTestTracking_preserves_inner_connection()
    {
        var services = new ServiceCollection();
        var fakeConn = new FakeDbConnection();
        services.AddSingleton<DbConnection>(fakeConn);

        services.AddDapperTestTracking();

        var provider = services.BuildServiceProvider();
        var tracked = Assert.IsType<TrackingDbConnection>(provider.GetRequiredService<DbConnection>());

        Assert.Same(fakeConn, tracked.InnerConnection);
    }

    [Fact]
    public void AddDapperTestTracking_resolves_IHttpContextAccessor_from_DI()
    {
        var services = new ServiceCollection();
        var accessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<DbConnection>(new FakeDbConnection());

        services.AddDapperTestTracking();

        var provider = services.BuildServiceProvider();
        var connection = provider.GetRequiredService<DbConnection>();

        // Should resolve without error; accessor wired internally
        Assert.IsType<TrackingDbConnection>(connection);
    }

    [Fact]
    public void AddDapperTestTracking_decorates_multiple_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<DbConnection>(new FakeDbConnection());
        services.AddSingleton<DbConnection>(new FakeDbConnection());

        services.AddDapperTestTracking();

        var descriptors = services.Where(d => d.ServiceType == typeof(DbConnection)).ToList();
        Assert.Equal(2, descriptors.Count);

        var provider = services.BuildServiceProvider();
        var connections = provider.GetServices<DbConnection>().ToList();
        Assert.All(connections, c => Assert.IsType<TrackingDbConnection>(c));
    }
}
