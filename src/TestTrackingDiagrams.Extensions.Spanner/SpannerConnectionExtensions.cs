using System.Data.Common;
using Google.Cloud.Spanner.Data;
using Google.Cloud.Spanner.V1;
using Microsoft.AspNetCore.Http;

namespace TestTrackingDiagrams.Extensions.Spanner;

/// <summary>
/// Provides extension methods for configuring Google Cloud Spanner client options to enable test tracking.
/// </summary>
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
        return builder.WithTestTracking(options, httpContextAccessor: null);
    }

    /// <summary>
    /// Configures gRPC-level interception on a <see cref="SpannerConnectionStringBuilder"/>
    /// with access to the current HTTP context for test identity resolution.
    /// <para>
    /// When <paramref name="httpContextAccessor"/> is provided, the interceptor reads test
    /// identity from HTTP request headers (propagated by <c>TestTrackingMessageHandler</c>)
    /// before falling back to <see cref="SpannerTrackingOptions.CurrentTestInfoFetcher"/>.
    /// This is essential in WebApplicationFactory scenarios where <c>AsyncLocal</c> test
    /// identity does not propagate through the TestServer's request pipeline.
    /// </para>
    /// </summary>
    /// <returns>The same builder instance, for chaining.</returns>
    public static SpannerConnectionStringBuilder WithTestTracking(
        this SpannerConnectionStringBuilder builder,
        SpannerTrackingOptions options,
        IHttpContextAccessor? httpContextAccessor)
    {
        var interceptor = new SpannerTrackingInterceptor(options, httpContextAccessor);

        var settings = new SpannerSettings
        {
            Interceptor = interceptor
        };

        builder.SessionPoolManager = SessionPoolManager.CreateWithSettings(
            new SessionPoolOptions(), settings);

        return builder;
    }
}