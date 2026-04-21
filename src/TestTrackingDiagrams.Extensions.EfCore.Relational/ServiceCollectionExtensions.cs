using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="SqlTrackingInterceptor"/> singleton that can later be resolved
    /// by <see cref="DbContextOptionsBuilderExtensions.WithSqlTestTracking(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder, IServiceProvider)"/>.
    /// </summary>
    public static IServiceCollection AddSqlTestTracking(
        this IServiceCollection services,
        SqlTrackingInterceptorOptions options)
    {
        services.AddSingleton(new SqlTrackingInterceptor(options));
        return services;
    }
}
