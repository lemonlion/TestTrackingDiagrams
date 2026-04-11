using System.Diagnostics;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Tests.InternalFlow;

/// <summary>
/// Tests for <see cref="InternalFlowSpanStore"/>.
/// All tests use unique source names and filter assertions by source
/// to avoid contamination from parallel test execution.
/// Serialized via collection because <see cref="Clear_removes_all_spans"/> wipes the global store.
/// </summary>
[Collection("InternalFlowSpanStore")]
public class InternalFlowSpanStoreTests : IDisposable
{
    private readonly string _sourceName = $"StoreTest.{Guid.NewGuid():N}";
    private readonly ActivitySource _source;
    private readonly ActivityListener _listener;

    public InternalFlowSpanStoreTests()
    {
        _source = new ActivitySource(_sourceName);
        _listener = new ActivityListener
        {
            ShouldListenTo = s => s.Name == _sourceName,
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllDataAndRecorded
        };
        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose()
    {
        _listener.Dispose();
        _source.Dispose();
    }

    [Fact]
    public void Add_and_GetSpans_round_trip()
    {
        using var activity = _source.StartActivity("test-op")!;
        activity.Stop();
        InternalFlowSpanStore.Add(activity);

        Assert.Contains(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "test-op" && s.Source.Name == _sourceName);
    }

    [Fact]
    public void GetSpans_returns_snapshot_not_live_reference()
    {
        using var a1 = _source.StartActivity("snapshot-1")!;
        a1.Stop();
        InternalFlowSpanStore.Add(a1);

        var snapshot = InternalFlowSpanStore.GetSpans();
        var countBefore = snapshot.Count(s => s.Source.Name == _sourceName);

        using var a2 = _source.StartActivity("snapshot-2")!;
        a2.Stop();
        InternalFlowSpanStore.Add(a2);

        Assert.Equal(countBefore, snapshot.Count(s => s.Source.Name == _sourceName));
    }

    [Fact]
    public void Clear_removes_all_spans()
    {
        using var activity = _source.StartActivity("clear-op")!;
        activity.Stop();
        InternalFlowSpanStore.Add(activity);

        InternalFlowSpanStore.Clear();

        Assert.DoesNotContain(InternalFlowSpanStore.GetSpans(),
            s => s.DisplayName == "clear-op" && s.Source.Name == _sourceName);
    }

    [Fact]
    public async Task Thread_safe_concurrent_writes()
    {
        const int threadCount = 10;
        const int spansPerThread = 100;

        var barrier = new Barrier(threadCount);
        var tasks = Enumerable.Range(0, threadCount).Select(t => Task.Run(() =>
        {
            barrier.SignalAndWait();
            for (var i = 0; i < spansPerThread; i++)
            {
                using var a = _source.StartActivity($"t{t}-op-{i}")!;
                a.Stop();
                InternalFlowSpanStore.Add(a);
            }
        })).ToArray();

        await Task.WhenAll(tasks);

        Assert.Equal(threadCount * spansPerThread,
            InternalFlowSpanStore.GetSpans().Count(s => s.Source.Name == _sourceName));
    }
}
