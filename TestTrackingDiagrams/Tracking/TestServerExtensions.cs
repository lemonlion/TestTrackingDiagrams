using System.Net;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using TestTrackingDiagrams.Extensions;

namespace TestTrackingDiagrams.Tracking;

public static class TestServerExtensions
{
    public static async Task<HttpContext> SendAsync(this TestServer server, Action<HttpContext> configureContext, HttpContent? body, string serviceName, Func<string> currentTestInfoFetcher,
        CancellationToken cancellationToken = default)
    {
        var context = new DefaultHttpContext();
        configureContext.Invoke(context);
        var requestContentString = body is null ? string.Empty : await body.ReadAsStringAsync(cancellationToken);
        var requestHeaders = context.Request.Headers.Select(x => (x.Key, x.Value.First())).ToArray();

        var result = await server.SendAsync(configureContext, cancellationToken);

        var responseContentString = await new StreamContent(result.Response!.Body).ReadAsStringAsync(cancellationToken);
        var responseHeaders = result.Response.Headers.Select(x => (x.Key, x.Value.First())).ToArray();
        result.Response.Body = new MemoryStream(Encoding.UTF8.GetBytes(responseContentString));

        RequestResponseLogger.Log(new RequestResponseLog(
            currentTestInfoFetcher(),
            new RequestLog(new HttpMethod(context.Request.Method), requestContentString, context.Request.GetUri()!, requestHeaders, serviceName),
            new ResponseLog((HttpStatusCode)result.Response.StatusCode, responseContentString, responseHeaders)));

        return result;
    }
}