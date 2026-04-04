using System.Diagnostics;
using OpenTelemetry;

namespace TestTrackingDiagrams.Extensions.OpenTelemetry;

internal sealed class TestTrackingSpanExporter : BaseExporter<Activity>
{
    public override ExportResult Export(in Batch<Activity> batch)
    {
        foreach (var activity in batch)
        {
            TestTrackingSpanStore.Add(activity);
        }

        return ExportResult.Success;
    }
}
