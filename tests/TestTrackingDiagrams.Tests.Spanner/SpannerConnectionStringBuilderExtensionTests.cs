using Google.Cloud.Spanner.Data;
using TestTrackingDiagrams.Extensions.Spanner;

namespace TestTrackingDiagrams.Tests.Spanner;

public class SpannerConnectionStringBuilderExtensionTests
{
    private SpannerTrackingOptions MakeOptions() => new()
    {
        ServiceName = "Spanner",
        CallingServiceName = "TestCaller",
        CurrentTestInfoFetcher = () => ("Test", Guid.NewGuid().ToString()),
    };

    [Fact]
    public void WithTestTracking_Returns_same_builder_instance()
    {
        var builder = new SpannerConnectionStringBuilder
        {
            DataSource = "projects/p/instances/i/databases/d"
        };

        var result = builder.WithTestTracking(MakeOptions());

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithTestTracking_Sets_SessionPoolManager()
    {
        var builder = new SpannerConnectionStringBuilder
        {
            DataSource = "projects/p/instances/i/databases/d"
        };
        var defaultManager = builder.SessionPoolManager;

        builder.WithTestTracking(MakeOptions());

        Assert.NotSame(defaultManager, builder.SessionPoolManager);
    }

    [Fact]
    public void WithTestTracking_Preserves_DataSource()
    {
        var builder = new SpannerConnectionStringBuilder
        {
            DataSource = "projects/p/instances/i/databases/d"
        };

        builder.WithTestTracking(MakeOptions());

        Assert.Equal("projects/p/instances/i/databases/d", builder.DataSource);
    }
}
