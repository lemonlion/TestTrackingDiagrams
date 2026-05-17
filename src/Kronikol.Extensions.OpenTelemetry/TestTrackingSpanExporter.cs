using System.Diagnostics;
using OpenTelemetry;
using Kronikol.InternalFlow;

namespace Kronikol.Extensions.OpenTelemetry;

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
