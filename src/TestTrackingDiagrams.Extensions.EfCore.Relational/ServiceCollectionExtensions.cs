using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers a <see cref="SqlTrackingInterceptor"/> singleton that can later be resolved
    /// by <see cref="DbContextOptionsBuilderExtensions.WithSqlTestTracking(Microsoft.EntityFrameworkCore.DbContextOptionsBuilder, IServiceProvider)"/>.
    /// When <see cref="IHttpContextAccessor"/> is registered in the container, the interceptor
    /// will automatically read test identity from HTTP request headers (set by TTD's tracking
    /// client), enabling SQL tracking inside server-side HTTP request pipelines.
    /// </summary>
    public static IServiceCollection AddSqlTestTracking(
        this IServiceCollection services,
        SqlTrackingInterceptorOptions options)
    {
        services.AddSingleton(sp =>
        {
            var httpContextAccessor = sp.GetService<IHttpContextAccessor>();
            return new SqlTrackingInterceptor(options, httpContextAccessor);
        });
        return services;
    }
}
