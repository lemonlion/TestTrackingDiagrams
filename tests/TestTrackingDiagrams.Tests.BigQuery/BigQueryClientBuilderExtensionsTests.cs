using Google.Cloud.BigQuery.V2;
using TestTrackingDiagrams.Extensions.BigQuery;

namespace TestTrackingDiagrams.Tests.BigQuery;

public class BigQueryClientBuilderExtensionsTests
{
    [Fact]
    public void WithTestTracking_Returns_Same_Builder_Instance()
    {
        var builder = new BigQueryClientBuilder { ProjectId = "test-project" };
        var options = new BigQueryTrackingMessageHandlerOptions();

        var result = builder.WithTestTracking(options);

        Assert.Same(builder, result);
    }

    [Fact]
    public void WithTestTracking_Sets_HttpClientFactory()
    {
        var builder = new BigQueryClientBuilder { ProjectId = "test-project" };
        Assert.Null(builder.HttpClientFactory);

        builder.WithTestTracking(new BigQueryTrackingMessageHandlerOptions());

        Assert.NotNull(builder.HttpClientFactory);
    }
}
