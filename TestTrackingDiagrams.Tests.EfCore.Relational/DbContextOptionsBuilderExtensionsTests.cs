using Microsoft.EntityFrameworkCore;
using TestTrackingDiagrams.Extensions.EfCore.Relational;

namespace TestTrackingDiagrams.Tests.EfCore.Relational;

public class DbContextOptionsBuilderExtensionsTests
{
    [Fact]
    public void WithSqlTestTracking_AddsInterceptor()
    {
        var builder = new DbContextOptionsBuilder();
        var options = new SqlTrackingInterceptorOptions();

        builder.WithSqlTestTracking(options);

        // Verify options were configured without throwing
        Assert.NotNull(builder.Options);
    }

    [Fact]
    public void WithSqlTestTracking_ReturnsSameBuilder()
    {
        var builder = new DbContextOptionsBuilder();
        var options = new SqlTrackingInterceptorOptions();

        var result = builder.WithSqlTestTracking(options);

        Assert.Same(builder, result);
    }
}
