using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.MongoDB;

/// <summary>
/// Provides extension methods for configuring MongoDB dependency tracking on <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection"/>.
/// </summary>
public static class MongoDbServiceCollectionExtensions
{
    public static IServiceCollection AddMongoDbTestTracking(
        this IServiceCollection services,
        Action<MongoDbTrackingOptions>? configure = null)
    {
        var options = new MongoDbTrackingOptions();
        configure?.Invoke(options);

        services.AddSingleton(sp => new MongoDbTrackingSubscriber(options, sp.GetService<IHttpContextAccessor>()));

        return services;
    }
}