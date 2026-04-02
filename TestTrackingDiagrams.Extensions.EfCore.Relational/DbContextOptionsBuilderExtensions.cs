using Microsoft.EntityFrameworkCore;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static class DbContextOptionsBuilderExtensions
{
    public static DbContextOptionsBuilder WithSqlTestTracking(
        this DbContextOptionsBuilder builder,
        SqlTrackingInterceptorOptions options)
    {
        builder.AddInterceptors(new SqlTrackingInterceptor(options));
        return builder;
    }
}
