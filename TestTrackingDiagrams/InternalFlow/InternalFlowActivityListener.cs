using System.Diagnostics;

namespace TestTrackingDiagrams.InternalFlow;

/// <summary>
/// A non-invasive <see cref="ActivityListener"/> that captures completed
/// <see cref="Activity"/> spans into <see cref="InternalFlowSpanStore"/>
/// without interfering with the SUT's existing OpenTelemetry configuration.
/// <para>
/// Uses <see cref="ActivitySamplingResult.AllData"/> (not <c>AllDataAndRecorded</c>)
/// so that the <see cref="Activity.Recorded"/> flag remains <c>false</c> —
/// existing OTel exporters that check <c>Recorded</c> will skip these activities.
/// </para>
/// </summary>
public sealed class InternalFlowActivityListener : IDisposable
{
    private readonly ActivityListener _listener;

    public InternalFlowActivityListener(params string[] additionalActivitySources)
    {
        var subscribedSources = new HashSet<string>(
            InternalFlowSpanCollector.WellKnownAutoInstrumentationSources,
            StringComparer.OrdinalIgnoreCase);

        foreach (var source in additionalActivitySources)
            subscribedSources.Add(source);

        _listener = new ActivityListener
        {
            ShouldListenTo = activitySource => subscribedSources.Contains(activitySource.Name),
            Sample = (ref ActivityCreationOptions<ActivityContext> _) => ActivitySamplingResult.AllData,
            ActivityStopped = InternalFlowSpanStore.Add
        };

        ActivitySource.AddActivityListener(_listener);
    }

    public void Dispose() => _listener.Dispose();
}
