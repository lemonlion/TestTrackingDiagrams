using System.Diagnostics;
using OpenTelemetry;
using TestTrackingDiagrams.InternalFlow;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

internal sealed class TestTrackingSpanExporter : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            InternalFlowSpanStore.Add(activity);
        }

        return ExportResult.Success;
    }
}
