using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using TestTrackingDiagrams.Extensions.Redis;

namespace TestTrackingDiagrams.Tests.Redis;

public class RedisServiceCollectionExtensionsTests
{
    [Fact]
    public void AddRedisTestTracking_decorates_registered_database()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());

        services.AddRedisTestTracking(options =>
        {
            options.ServiceName = "TestRedis";
        });

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<IDatabase>();

        Assert.IsAssignableFrom<RedisTrackingDatabase>((object)db);
    }

    [Fact]
    public void AddRedisTestTracking_preserves_service_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<IDatabase>(_ => StubDatabase.CreateProxy());

        services.AddRedisTestTracking();

        var descriptor = Assert.Single(services, d => d.ServiceType == typeof(IDatabase));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void AddRedisTestTracking_does_not_duplicate_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());

        services.AddRedisTestTracking();

        Assert.Single(services, d => d.ServiceType == typeof(IDatabase));
    }

    [Fact]
    public void AddRedisTestTracking_is_noop_when_no_database_registered()
    {
        var services = new ServiceCollection();

        services.AddRedisTestTracking();

        Assert.DoesNotContain(services, d => d.ServiceType == typeof(IDatabase));
    }

    [Fact]
    public void AddRedisTestTracking_applies_options_configuration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());

        services.AddRedisTestTracking(options =>
        {
            options.ServiceName = "CustomRedis";
            options.CallerName = "MySvc";
            options.Verbosity = RedisTrackingVerbosity.Summarised;
        });

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<IDatabase>();

        // Verify it resolves without error — options are applied internally
        Assert.IsAssignableFrom<RedisTrackingDatabase>((object)db);
    }

    [Fact]
    public void AddRedisTestTracking_resolves_IHttpContextAccessor_from_DI()
    {
        var services = new ServiceCollection();
        var accessor = new HttpContextAccessor();
        services.AddSingleton<IHttpContextAccessor>(accessor);
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());

        services.AddRedisTestTracking();

        var provider = services.BuildServiceProvider();
        var db = provider.GetRequiredService<IDatabase>();

        // Should resolve without error; accessor wired internally
        Assert.IsAssignableFrom<RedisTrackingDatabase>((object)db);
    }

    [Fact]
    public void AddRedisTestTracking_decorates_multiple_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());
        services.AddSingleton<IDatabase>(StubDatabase.CreateProxy());

        services.AddRedisTestTracking();

        var descriptors = services.Where(d => d.ServiceType == typeof(IDatabase)).ToList();
        Assert.Equal(2, descriptors.Count);

        var provider = services.BuildServiceProvider();
        var databases = provider.GetServices<IDatabase>().ToList();
        Assert.All(databases, db => Assert.IsAssignableFrom<RedisTrackingDatabase>((object)db));
    }
}
