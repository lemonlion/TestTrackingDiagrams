using System.Diagnostics;
using TestTrackingDiagrams.Tracking;

namespace TestTrackingDiagrams.Tests.Tracking;

public class TrackingTraceContextTests
{
    [Fact]
    public void CurrentTraceId_is_null_initially()
    {
        Assert.Null(TrackingTraceContext.CurrentTraceId);
    }

    [Fact]
    public void BeginTrace_sets_CurrentTraceId()
    {
        using var scope = TrackingTraceContext.BeginTrace();
        Assert.NotNull(TrackingTraceContext.CurrentTraceId);
    }

    [Fact]
    public void BeginTrace_outputs_traceId()
    {
        using var scope = TrackingTraceContext.BeginTrace(out var traceId);
        Assert.NotEqual(Guid.Empty, traceId);
        Assert.Equal(traceId, TrackingTraceContext.CurrentTraceId);
    }

    [Fact]
    public void Dispose_clears_CurrentTraceId()
    {
        var scope = TrackingTraceContext.BeginTrace();
        Assert.NotNull(TrackingTraceContext.CurrentTraceId);

        scope.Dispose();
        Assert.Null(TrackingTraceContext.CurrentTraceId);
    }

    [Fact]
    public void Nested_scopes_restore_previous_value()
    {
        using var outer = TrackingTraceContext.BeginTrace(out var outerId);
        using (var inner = TrackingTraceContext.BeginTrace(out var innerId))
        {
            Assert.NotEqual(outerId, innerId);
            Assert.Equal(innerId, TrackingTraceContext.CurrentTraceId);
        }
        Assert.Equal(outerId, TrackingTraceContext.CurrentTraceId);
    }

    [Fact]
    public void CreateParentContext_returns_default_when_no_trace()
    {
        var ctx = TrackingTraceContext.CreateParentContext();
        Assert.Equal(default, ctx);
    }

    [Fact]
    public void CreateParentContext_returns_valid_context_in_scope()
    {
        using var scope = TrackingTraceContext.BeginTrace(out var traceId);
        var ctx = TrackingTraceContext.CreateParentContext();

        Assert.NotEqual(default, ctx.TraceId);

        // The ActivityTraceId should be derived from our trace Guid
        var expectedBytes = traceId.ToByteArray();
        var actualTraceId = ctx.TraceId.ToString();
        Assert.False(string.IsNullOrEmpty(actualTraceId));
    }

    [Fact]
    public async Task TraceId_flows_across_async_boundaries()
    {
        using var scope = TrackingTraceContext.BeginTrace(out var traceId);

        var capturedId = await Task.Run(() => TrackingTraceContext.CurrentTraceId);

        Assert.Equal(traceId, capturedId);
    }
}
