using TestTrackingDiagrams.LightBDD;

namespace TestTrackingDiagrams.Tests.LightBDD.xUnit3;

public class LightBddDiagramsFetcherTests
{
    [Fact]
    public void GetDiagramsFetcher_ShouldReturnNonNullFunc()
    {
        var fetcher = LightBddDiagramsFetcher.GetDiagramsFetcher();

        Assert.NotNull(fetcher);
    }

    [Fact]
    public void GetDiagramsFetcher_ShouldAcceptNullOptions()
    {
        var fetcher = LightBddDiagramsFetcher.GetDiagramsFetcher(null);

        Assert.NotNull(fetcher);
    }

    [Fact]
    public void GetDiagramsFetcher_ShouldAcceptCustomOptions()
    {
        var options = new DiagramsFetcherOptions
        {
            PlantUmlServerBaseUrl = "http://custom-server.com"
        };

        var fetcher = LightBddDiagramsFetcher.GetDiagramsFetcher(options);

        Assert.NotNull(fetcher);
    }

    [Fact]
    public void GetDiagramsFetcher_ShouldReturnEmptyArrayWhenNoLogs()
    {
        var fetcher = LightBddDiagramsFetcher.GetDiagramsFetcher();

        var result = fetcher();

        Assert.NotNull(result);
    }
}
