using System.Data.Common;

namespace TestTrackingDiagrams.Extensions.Spanner;

public static class SpannerConnectionExtensions
{
    public static TrackingSpannerConnection WithTestTracking(
        this DbConnection connection,
        SpannerTrackingOptions options)
    {
        return new TrackingSpannerConnection(connection, options);
    }
}
