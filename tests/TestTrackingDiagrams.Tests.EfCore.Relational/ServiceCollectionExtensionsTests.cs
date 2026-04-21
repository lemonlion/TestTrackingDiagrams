using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.EfCore.Relational;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class ServiceCollectionExtensionsTests
{
    [Fact]
    public void AddSqlTestTracking_RegistersSqlTrackingInterceptorAsSingleton()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions { ServiceName = "TestDB" };

        services.AddSqlTestTracking(options);

        var provider = services.BuildServiceProvider();
        var interceptor = provider.GetService<SqlTrackingInterceptor>();
        Assert.NotNull(interceptor);
    }

    [Fact]
    public void AddSqlTestTracking_ReturnsSameServiceCollection()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions();

        var result = services.AddSqlTestTracking(options);

        Assert.Same(services, result);
    }

    [Fact]
    public void AddSqlTestTracking_RegistersAsSingleton()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions();

        services.AddSqlTestTracking(options);

        var provider = services.BuildServiceProvider();
        var first = provider.GetService<SqlTrackingInterceptor>();
        var second = provider.GetService<SqlTrackingInterceptor>();
        Assert.Same(first, second);
    }

    [Fact]
    public void WithSqlTestTracking_ServiceProvider_ResolvesFromDI()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions();
        services.AddSqlTestTracking(options);
        var provider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder();
        builder.WithSqlTestTracking(provider);

        Assert.NotNull(builder.Options);
    }

    [Fact]
    public void WithSqlTestTracking_ServiceProvider_ReturnsSameBuilder()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions();
        services.AddSqlTestTracking(options);
        var provider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder();
        var result = builder.WithSqlTestTracking(provider);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithSqlTestTracking_ServiceProvider_ThrowsWhenInterceptorNotRegistered()
    {
        var services = new ServiceCollection();
        var provider = services.BuildServiceProvider();

        var builder = new DbContextOptionsBuilder();

        Assert.Throws<InvalidOperationException>(() => builder.WithSqlTestTracking(provider));
    }

    [Fact]
    public void AddSqlTestTracking_AutoResolvesHttpContextAccessor_WhenRegistered()
    {
        var services = new ServiceCollection();
        services.AddHttpContextAccessor();
        var options = new SqlTrackingInterceptorOptions { ServiceName = "AutoResolveDB" };

        services.AddSqlTestTracking(options);

        var provider = services.BuildServiceProvider();
        var interceptor = provider.GetService<SqlTrackingInterceptor>();
        Assert.NotNull(interceptor);
    }

    [Fact]
    public void AddSqlTestTracking_WorksWithout_HttpContextAccessor()
    {
        var services = new ServiceCollection();
        var options = new SqlTrackingInterceptorOptions { ServiceName = "NoAccessorDB" };

        services.AddSqlTestTracking(options);

        var provider = services.BuildServiceProvider();
        var interceptor = provider.GetService<SqlTrackingInterceptor>();
        Assert.NotNull(interceptor);
    }
}
