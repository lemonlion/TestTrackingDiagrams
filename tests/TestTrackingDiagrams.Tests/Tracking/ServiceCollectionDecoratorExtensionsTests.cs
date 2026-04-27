using Microsoft.Extensions.DependencyInjection;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class ServiceCollectionDecoratorExtensionsTests
{
    #region DecorateAll<TService>

    [Fact]
    public void DecorateAll_wraps_single_registration()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new PlainGreeter("Hello"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        Assert.Equal("HELLO", greeter.Greet());
    }

    [Fact]
    public void DecorateAll_wraps_all_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new PlainGreeter("Hi"));
        services.AddSingleton<IGreeter>(new PlainGreeter("Hey"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var provider = services.BuildServiceProvider();
        var greeters = provider.GetServices<IGreeter>().ToList();

        Assert.Equal(2, greeters.Count);
        Assert.Equal("HI", greeters[0].Greet());
        Assert.Equal("HEY", greeters[1].Greet());
    }

    [Fact]
    public void DecorateAll_preserves_singleton_lifetime()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new PlainGreeter("Test"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var descriptor = services.Single(d => d.ServiceType == typeof(IGreeter));
        Assert.Equal(ServiceLifetime.Singleton, descriptor.Lifetime);
    }

    [Fact]
    public void DecorateAll_preserves_scoped_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<IGreeter>(_ => new PlainGreeter("Scoped"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var descriptor = services.Single(d => d.ServiceType == typeof(IGreeter));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void DecorateAll_preserves_transient_lifetime()
    {
        var services = new ServiceCollection();
        services.AddTransient<IGreeter>(_ => new PlainGreeter("Transient"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var descriptor = services.Single(d => d.ServiceType == typeof(IGreeter));
        Assert.Equal(ServiceLifetime.Transient, descriptor.Lifetime);
    }

    [Fact]
    public void DecorateAll_handles_implementation_type_descriptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter, DefaultGreeter>();

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        Assert.Equal("DEFAULT", greeter.Greet());
    }

    [Fact]
    public void DecorateAll_handles_factory_descriptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(_ => new PlainGreeter("Factory"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        Assert.Equal("FACTORY", greeter.Greet());
    }

    [Fact]
    public void DecorateAll_does_not_duplicate_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new PlainGreeter("Single"));

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        Assert.Single(services, d => d.ServiceType == typeof(IGreeter));
    }

    [Fact]
    public void DecorateAll_is_noop_when_no_registrations()
    {
        var services = new ServiceCollection();

        services.DecorateAll<IGreeter>((_, inner) => new LoudGreeter(inner));

        Assert.Empty(services);
    }

    [Fact]
    public void DecorateAll_can_resolve_additional_services()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IGreeter>(new PlainGreeter("Hello"));
        services.AddSingleton("PREFIX");

        services.DecorateAll<IGreeter>((sp, inner) =>
        {
            var prefix = sp.GetRequiredService<string>();
            return new PlainGreeter($"{prefix}: {inner.Greet()}");
        });

        var provider = services.BuildServiceProvider();
        var greeter = provider.GetRequiredService<IGreeter>();

        Assert.Equal("PREFIX: Hello", greeter.Greet());
    }

    #endregion

    #region DecorateAllOpen

    [Fact]
    public void DecorateAllOpen_wraps_single_closed_generic()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("data"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IRepository<string>>();

        Assert.Equal("[LOG] data", repo.Get());
    }

    [Fact]
    public void DecorateAllOpen_wraps_multiple_closed_generics()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("str"));
        services.AddSingleton<IRepository<int>>(new InMemoryRepository<int>("42"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var stringRepo = provider.GetRequiredService<IRepository<string>>();
        var intRepo = provider.GetRequiredService<IRepository<int>>();

        Assert.Equal("[LOG] str", stringRepo.Get());
        Assert.Equal("[LOG] 42", intRepo.Get());
    }

    [Fact]
    public void DecorateAllOpen_preserves_lifetime()
    {
        var services = new ServiceCollection();
        services.AddScoped<IRepository<string>>(_ => new InMemoryRepository<string>("scoped"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var descriptor = services.Single(d => d.ServiceType == typeof(IRepository<string>));
        Assert.Equal(ServiceLifetime.Scoped, descriptor.Lifetime);
    }

    [Fact]
    public void DecorateAllOpen_does_not_duplicate_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("data"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        Assert.Single(services, d => d.ServiceType == typeof(IRepository<string>));
    }

    [Fact]
    public void DecorateAllOpen_is_noop_when_no_matching_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton("unrelated");

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        Assert.Single(services);
    }

    [Fact]
    public void DecorateAllOpen_handles_factory_descriptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(_ => new InMemoryRepository<string>("factory"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IRepository<string>>();

        Assert.Equal("[LOG] factory", repo.Get());
    }

    [Fact]
    public void DecorateAllOpen_handles_implementation_type_descriptor()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>, DefaultStringRepository>();

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IRepository<string>>();

        Assert.Equal("[LOG] default", repo.Get());
    }

    [Fact]
    public void DecorateAllOpen_resolves_additional_constructor_args_from_di()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("data"));
        services.AddSingleton(new LogPrefix("[CUSTOM]"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(PrefixedLoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IRepository<string>>();

        Assert.Equal("[CUSTOM] data", repo.Get());
    }

    [Fact]
    public void DecorateAllOpen_wraps_multiple_registrations_of_same_closed_type()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("first"));
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("second"));

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repos = provider.GetServices<IRepository<string>>().ToList();

        Assert.Equal(2, repos.Count);
        Assert.Equal("[LOG] first", repos[0].Get());
        Assert.Equal("[LOG] second", repos[1].Get());
    }

    [Fact]
    public void DecorateAllOpen_ignores_non_matching_generic_registrations()
    {
        var services = new ServiceCollection();
        services.AddSingleton<IRepository<string>>(new InMemoryRepository<string>("match"));
        services.AddSingleton<ILogger<string>>(new FakeLogger<string>());

        services.DecorateAllOpen(typeof(IRepository<>), typeof(LoggingRepository<>));

        var provider = services.BuildServiceProvider();
        var repo = provider.GetRequiredService<IRepository<string>>();
        var logger = provider.GetRequiredService<ILogger<string>>();

        Assert.Equal("[LOG] match", repo.Get());
        Assert.IsType<FakeLogger<string>>(logger);
    }

    #endregion

    #region Test Doubles

    private interface IGreeter
    {
        string Greet();
    }

    private class PlainGreeter(string message) : IGreeter
    {
        public string Greet() => message;
    }

    private class LoudGreeter(IGreeter inner) : IGreeter
    {
        public string Greet() => inner.Greet().ToUpperInvariant();
    }

    private class DefaultGreeter : IGreeter
    {
        public string Greet() => "default";
    }

    private interface IRepository<T>
    {
        string Get();
    }

    private class InMemoryRepository<T>(string data) : IRepository<T>
    {
        public string Get() => data;
    }

    private class LoggingRepository<T>(IRepository<T> inner) : IRepository<T>
    {
        public string Get() => $"[LOG] {inner.Get()}";
    }

    private class DefaultStringRepository : IRepository<string>
    {
        public string Get() => "default";
    }

    private record LogPrefix(string Value);

    private class PrefixedLoggingRepository<T>(IRepository<T> inner, LogPrefix prefix) : IRepository<T>
    {
        public string Get() => $"{prefix.Value} {inner.Get()}";
    }

    private interface ILogger<T>
    {
        void Log(string message);
    }

    private class FakeLogger<T> : ILogger<T>
    {
        public void Log(string message) { }
    }

    #endregion
}
