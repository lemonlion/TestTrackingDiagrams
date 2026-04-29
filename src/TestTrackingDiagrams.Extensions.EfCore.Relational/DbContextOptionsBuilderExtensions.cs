using Microsoft.EntityFrameworkCore;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

/// <summary>
/// Provides extension methods for configuring Entity Framework Core client options to enable test tracking.
/// </summary>
public static class DbContextOptionsBuilderExtensions
{
    /// <summary>
    /// Adds a new <see cref="SqlTrackingInterceptor"/> to the DbContext pipeline.
    /// </summary>
    public static DbContextOptionsBuilder WithSqlTestTracking(
        this DbContextOptionsBuilder builder,
        SqlTrackingInterceptorOptions options)
    {
        builder.AddInterceptors(new SqlTrackingInterceptor(options));
        return builder;
    }

    /// <summary>
    /// Resolves the <see cref="SqlTrackingInterceptor"/> registered by
    /// <see cref="ServiceCollectionExtensions.AddSqlTestTracking"/> and adds it to the DbContext pipeline.
    /// </summary>
    public static DbContextOptionsBuilder WithSqlTestTracking(
        this DbContextOptionsBuilder builder,
        IServiceProvider serviceProvider)
    {
        var interceptor = serviceProvider.GetRequiredService<SqlTrackingInterceptor>();
        builder.AddInterceptors(interceptor);
        return builder;
    }

    private static T GetRequiredService<T>(this IServiceProvider provider) where T : class
    {
        return (T)(provider.GetService(typeof(T))
            ?? throw new InvalidOperationException(
                $"No service for type '{typeof(T)}' has been registered. " +
                $"Call services.AddSqlTestTracking(options) first."));
    }
}