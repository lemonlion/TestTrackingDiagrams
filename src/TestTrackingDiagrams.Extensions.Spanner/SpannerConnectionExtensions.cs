using System.Data.Common;
using Google.Cloud.Spanner.Data;
using Google.Cloud.Spanner.V1;

namespace TestTrackingDiagrams.Extensions.Spanner;

public static class SpannerConnectionExtensions
{
    /// <summary>
    /// Wraps a <see cref="DbConnection"/> with ADO.NET command interception.
    /// Only intercepts commands created via <see cref="DbConnection.CreateCommand()"/>.
    /// Spanner-specific methods (CreateInsertCommand, CreateSelectCommand, etc.) are NOT intercepted.
    /// For full coverage, use <see cref="WithTestTracking(SpannerConnectionStringBuilder, SpannerTrackingOptions)"/> instead.
    /// </summary>
    public static TrackingSpannerConnection WithTestTracking(
        this DbConnection connection,
        SpannerTrackingOptions options)
    {
        return new TrackingSpannerConnection(connection, options);
    }

    /// <summary>
    /// Configures gRPC-level interception on a <see cref="SpannerConnectionStringBuilder"/>.
    /// Intercepts ALL Spanner operations at the gRPC transport layer, including Spanner-specific
    /// methods (CreateInsertCommand, CreateSelectCommand, CreateInsertOrUpdateCommand, etc.).
    /// </summary>
    /// <returns>The same builder instance, for chaining.</returns>
    public static SpannerConnectionStringBuilder WithTestTracking(
        this SpannerConnectionStringBuilder builder,
        SpannerTrackingOptions options)
    {
        var interceptor = new SpannerTrackingInterceptor(options);

        var settings = new SpannerSettings
        {
            Interceptor = interceptor
        };

        builder.SessionPoolManager = SessionPoolManager.CreateWithSettings(
            new SessionPoolOptions(), settings);

        return builder;
    }
}
