using Microsoft.AspNetCore.Http;
using Kronikol.Constants;

namespace Kronikol.Tracking;

/// <summary>
/// ASP.NET Core middleware that reads test-tracking headers from incoming requests and
/// sets <see cref="TestIdentityScope.Current"/> for the duration of the request.
/// This ensures test identity flows into background tasks spawned by <c>Task.Run</c>
/// or <c>ThreadPool.QueueUserWorkItem</c> via <see cref="AsyncLocal{T}"/>.
/// <para>
/// Register via <c>app.UseTestTrackingContext()</c> or automatically using
/// <see cref="TestTrackingContextStartupFilter"/>.
/// </para>
/// </summary>
public class TestTrackingContextMiddleware
{
    private readonly RequestDelegate _next;

    public TestTrackingContextMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var name) &&
            context.Request.Headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var id) &&
            name.Count > 0 && id.Count > 0)
        {
            using (TestIdentityScope.Begin(name[0]!, id[0]!))
            {
                await _next(context);
            }
        }
        else
        {
            await _next(context);
        }
    }
}
