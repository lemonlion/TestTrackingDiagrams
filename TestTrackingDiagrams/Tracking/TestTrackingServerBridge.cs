using Microsoft.AspNetCore.Http;
using TestTrackingDiagrams.Constants;

namespace TestTrackingDiagrams.Tracking;

/// <summary>
/// Reads test tracking context from HTTP request headers, enabling code running on the
/// WebApplicationFactory server thread to obtain the test name and ID that was propagated
/// by <see cref="TestTrackingMessageHandler"/>.
/// </summary>
public static class TestTrackingServerBridge
{
    /// <summary>
    /// Reads test name and ID from the current HTTP request headers.
    /// Returns null if no test context headers are present (e.g. outside a request).
    /// </summary>
    public static (string Name, string Id)? GetCurrentTestInfo(IHttpContextAccessor httpContextAccessor)
    {
        var context = httpContextAccessor.HttpContext;
        if (context is null) return null;

        var headers = context.Request.Headers;
        if (!headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestNameHeader, out var nameValues) ||
            !headers.TryGetValue(TestTrackingHttpHeaders.CurrentTestIdHeader, out var idValues))
            return null;

        var name = nameValues.FirstOrDefault();
        var id = idValues.FirstOrDefault();

        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(id))
            return null;

        return (name, id);
    }
}
