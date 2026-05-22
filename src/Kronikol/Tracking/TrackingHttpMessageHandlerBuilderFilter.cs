using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Http;

namespace Kronikol.Tracking;

/// <summary>
/// An <see cref="IHttpMessageHandlerBuilderFilter"/> that adds <see cref="TestTrackingMessageHandler"/>
/// to every HttpClient pipeline. Unlike the legacy <see cref="TestTrackingHttpClientFactory"/> approach,
/// this filter coexists with other registered filters (Polly, logging, user filters) and passes
/// the <c>builder.Name</c> as the <c>clientName</c> for <c>ClientNamesToServiceNames</c> resolution.
/// </summary>
internal class TrackingHttpMessageHandlerBuilderFilter(
    TestTrackingMessageHandlerOptions options,
    IHttpContextAccessor httpContextAccessor) : IHttpMessageHandlerBuilderFilter
{
    public Action<HttpMessageHandlerBuilder> Configure(Action<HttpMessageHandlerBuilder> next)
    {
        return builder =>
        {
            next(builder);
            builder.AdditionalHandlers.Add(
                new TestTrackingMessageHandler(options, httpContextAccessor, clientName: builder.Name));
        };
    }
}
