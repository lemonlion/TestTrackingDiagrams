using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Extensions.EfCore.Relational;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class RemoveDbContextTests
{
    private class TestDbContext(DbContextOptions<TestDbContext> options) : DbContext(options);

    [Fact]
    public void RemoveDbContext_Removes_DbContextOptions_Registration()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test1"));

        services.RemoveDbContext<TestDbContext>();

        var remaining = services.Where(d => d.ServiceType == typeof(DbContextOptions<TestDbContext>));
        Assert.Empty(remaining);
    }

    [Fact]
    public void RemoveDbContext_Removes_DbContext_Registration()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test2"));

        services.RemoveDbContext<TestDbContext>();

        var remaining = services.Where(d => d.ServiceType == typeof(TestDbContext));
        Assert.Empty(remaining);
    }

    [Fact]
    public void RemoveDbContext_Removes_IDbContextOptionsConfiguration()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test3"));

        services.RemoveDbContext<TestDbContext>();

        // IDbContextOptionsConfiguration<T> is an internal EF Core type.
        // We verify by checking any generic type whose type argument contains TestDbContext.
        var remaining = services.Where(d =>
            d.ServiceType.IsGenericType &&
            d.ServiceType.GetGenericArguments().Contains(typeof(TestDbContext)));
        Assert.Empty(remaining);
    }

    [Fact]
    public void RemoveDbContext_Removes_NonGeneric_DbContextOptions()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test4"));

        services.RemoveDbContext<TestDbContext>();

        var remaining = services.Where(d => d.ServiceType == typeof(DbContextOptions));
        Assert.Empty(remaining);
    }

    [Fact]
    public void RemoveDbContext_Returns_Same_ServiceCollection()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test5"));

        var result = services.RemoveDbContext<TestDbContext>();

        Assert.Same(services, result);
    }

    [Fact]
    public void RemoveDbContext_Is_Safe_When_Context_Not_Registered()
    {
        var services = new ServiceCollection();

        var result = services.RemoveDbContext<TestDbContext>();

        Assert.Same(services, result);
    }

    [Fact]
    public void RemoveDbContext_Allows_Clean_Reregistration_With_Interceptor()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("original"));

        // Remove and re-register
        services.RemoveDbContext<TestDbContext>();
        services.AddSqlTestTracking(new SqlTrackingInterceptorOptions());
        services.AddDbContext<TestDbContext>((sp, o) =>
        {
            o.UseInMemoryDatabase("replacement")
             .WithSqlTestTracking(sp);
        });

        var provider = services.BuildServiceProvider();
        var context = provider.GetRequiredService<TestDbContext>();
        Assert.NotNull(context);
    }

    [Fact]
    public void RemoveDbContext_Does_Not_Remove_Unrelated_Services()
    {
        var services = new ServiceCollection();
        services.AddDbContext<TestDbContext>(o => o.UseInMemoryDatabase("test6"));
        services.AddSingleton<string>("keep-me");

        services.RemoveDbContext<TestDbContext>();

        var remaining = services.Where(d => d.ServiceType == typeof(string));
        Assert.Single(remaining);
    }
}
