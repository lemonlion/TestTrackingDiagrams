using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

/// <summary>
/// Tests for auto-starting <see cref="InternalFlowActivityListener"/>
/// from the <see cref="TestTrackingMessageHandler"/>.
/// The listener is started lazily on the first <c>SendAsync</c> call,
/// not in the constructor, to avoid interfering with diagnostic pipelines
/// that initialise during host startup.
/// All tests use custom sources + unique operation names for parallel safety.
/// No <c>ResetForTesting</c> — the static singleton is process-wide and shared.
/// </summary>
[Collection("InternalFlowSpanStore")]
public class AutoStartActivityListenerTests
{
    private static HttpMessageInvoker CreateInvoker(TestTrackingMessageHandlerOptions options)
    {
        var handler = new TestTrackingMessageHandler(options)
        {
            InnerHandler = new StubHandler()
        };
        return new HttpMessageInvoker(handler);
    }

    private class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
            => Task.FromResult(new HttpResponseMessage(System.Net.HttpStatusCode.OK) { Content = new StringContent("ok") });
    }

    private static TestTrackingMessageHandlerOptions DefaultOptions() => new()
    {
        CallingServiceName = "TestCaller",
        FixedNameForReceivingService = "Target",
        CurrentTestInfoFetcher = () => ("AutoStartTest", Guid.NewGuid().ToString()),
    };

    [Fact]
    public async Task First_SendAsync_starts_listener_for_custom_sources()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/api"), CancellationToken.None);

        var sourceName = $"CustomApp.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("custom-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "custom-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public async Task First_SendAsync_does_not_listen_to_System_Net_Http()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/api"), CancellationToken.None);

        var opName = $"http-{Guid.NewGuid():N}";
        using var source = new ActivitySource("System.Net.Http");
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.DoesNotContain(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == opName && s.Source.Name == "System.Net.Http");
    }

    [Theory]
    [InlineData("Microsoft.AspNetCore")]
    [InlineData("Microsoft.EntityFrameworkCore")]
    [InlineData("Npgsql")]
    [InlineData("StackExchange.Redis")]
    [InlineData("Azure.Cosmos")]
    public async Task First_SendAsync_does_listen_to_well_known_sources_other_than_System_Net_Http(string sourceName)
    {
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/api"), CancellationToken.None);

        var opName = $"wk-{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity(opName);
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == opName && s.Source.Name == sourceName);
    }

    [Fact]
    public async Task Multiple_SendAsync_calls_do_not_duplicate_spans()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/a"), CancellationToken.None);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/b"), CancellationToken.None);
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/c"), CancellationToken.None);

        var sourceName = $"MultiHandler.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("once-op");
        activity?.Stop();

        Assert.Single(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "once-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public async Task Listener_captures_any_source()
    {
        var unknownSource = $"Unknown.{Guid.NewGuid():N}";
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/api"), CancellationToken.None);

        using var source = new ActivitySource(unknownSource);
        using var activity = source.StartActivity("unknown-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.Source.Name == unknownSource);
    }

    [Fact]
    public async Task Listener_captures_spans_on_activity_stop()
    {
        using var invoker = CreateInvoker(DefaultOptions());
        await invoker.SendAsync(new HttpRequestMessage(HttpMethod.Get, "http://target:80/api"), CancellationToken.None);

        var sourceName = $"StopTest.{Guid.NewGuid():N}";
        using var source = new ActivitySource(sourceName);
        using var activity = source.StartActivity("stop-op");
        activity?.Stop();

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "stop-op" && s.Source.Name == sourceName);
    }

    [Fact]
    public void InternalFlowActivitySources_option_is_available_on_handler_options()
    {
        var customSources = new[] { "MyApp.Services", "MyApp.Database" };
        var options = new TestTrackingMessageHandlerOptions
        {
            InternalFlowActivitySources = customSources
        };

        Assert.Equal(customSources, options.InternalFlowActivitySources);
    }
}
