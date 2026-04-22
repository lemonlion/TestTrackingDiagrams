using System.Text.Json;

namespace TestTrackingDiagrams.Extensions.EventBridge;

public static class EventBridgeOperationClassifier
{
    private static readonly Dictionary<string, EventBridgeOperation> TargetMapping = new(StringComparer.OrdinalIgnoreCase)
    {
        ["AWSEvents.PutEvents"] = EventBridgeOperation.PutEvents,
        ["AWSEvents.PutPartnerEvents"] = EventBridgeOperation.PutPartnerEvents,
        ["AWSEvents.TestEventPattern"] = EventBridgeOperation.TestEventPattern,
        ["AWSEvents.PutRule"] = EventBridgeOperation.PutRule,
        ["AWSEvents.DeleteRule"] = EventBridgeOperation.DeleteRule,
        ["AWSEvents.DescribeRule"] = EventBridgeOperation.DescribeRule,
        ["AWSEvents.EnableRule"] = EventBridgeOperation.EnableRule,
        ["AWSEvents.DisableRule"] = EventBridgeOperation.DisableRule,
        ["AWSEvents.ListRules"] = EventBridgeOperation.ListRules,
        ["AWSEvents.PutTargets"] = EventBridgeOperation.PutTargets,
        ["AWSEvents.RemoveTargets"] = EventBridgeOperation.RemoveTargets,
        ["AWSEvents.ListTargetsByRule"] = EventBridgeOperation.ListTargetsByRule,
        ["AWSEvents.CreateEventBus"] = EventBridgeOperation.CreateEventBus,
        ["AWSEvents.DeleteEventBus"] = EventBridgeOperation.DeleteEventBus,
        ["AWSEvents.DescribeEventBus"] = EventBridgeOperation.DescribeEventBus,
        ["AWSEvents.ListEventBuses"] = EventBridgeOperation.ListEventBuses,
        ["AWSEvents.CreateArchive"] = EventBridgeOperation.CreateArchive,
        ["AWSEvents.DeleteArchive"] = EventBridgeOperation.DeleteArchive,
        ["AWSEvents.DescribeArchive"] = EventBridgeOperation.DescribeArchive,
        ["AWSEvents.ListArchives"] = EventBridgeOperation.ListArchives,
        ["AWSEvents.StartReplay"] = EventBridgeOperation.StartReplay,
        ["AWSEvents.DescribeReplay"] = EventBridgeOperation.DescribeReplay,
        ["AWSEvents.ListReplays"] = EventBridgeOperation.ListReplays,
        ["AWSEvents.CreateApiDestination"] = EventBridgeOperation.CreateApiDestination,
        ["AWSEvents.CreateConnection"] = EventBridgeOperation.CreateConnection,
        ["AWSEvents.TagResource"] = EventBridgeOperation.TagResource,
        ["AWSEvents.UntagResource"] = EventBridgeOperation.UntagResource,
        ["AWSEvents.ListTagsForResource"] = EventBridgeOperation.ListTagsForResource,
    };

    public static EventBridgeOperationInfo Classify(HttpRequestMessage request, string? bodyContent = null)
    {
        var target = request.Headers.TryGetValues("X-Amz-Target", out var values)
            ? values.FirstOrDefault()
            : null;

        var operation = target is not null && TargetMapping.TryGetValue(target, out var op)
            ? op
            : EventBridgeOperation.Other;

        string? eventBusName = null;
        string? ruleName = null;
        string? detailType = null;
        string? source = null;
        int? entryCount = null;

        if (bodyContent is not null && operation is EventBridgeOperation.PutEvents)
        {
            try
            {
                using var doc = JsonDocument.Parse(bodyContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("EventBusName", out var busEl))
                    eventBusName = busEl.GetString();

                if (root.TryGetProperty("Entries", out var entries) && entries.ValueKind == JsonValueKind.Array)
                {
                    entryCount = entries.GetArrayLength();
                    if (entryCount > 0)
                    {
                        var first = entries[0];
                        if (first.TryGetProperty("DetailType", out var dtEl))
                            detailType = dtEl.GetString();
                        if (first.TryGetProperty("Source", out var srcEl))
                            source = srcEl.GetString();
                        if (first.TryGetProperty("EventBusName", out var ebEl))
                            eventBusName ??= ebEl.GetString();
                    }
                }
            }
            catch (JsonException)
            {
                // Body parsing is best-effort
            }
        }
        else if (bodyContent is not null && operation is EventBridgeOperation.PutRule
            or EventBridgeOperation.DeleteRule
            or EventBridgeOperation.DescribeRule)
        {
            try
            {
                using var doc = JsonDocument.Parse(bodyContent);
                var root = doc.RootElement;

                if (root.TryGetProperty("Name", out var nameEl))
                    ruleName = nameEl.GetString();
                if (root.TryGetProperty("EventBusName", out var busEl))
                    eventBusName = busEl.GetString();
            }
            catch (JsonException) { }
        }

        return new(operation, eventBusName, ruleName, detailType, source, entryCount);
    }

    public static string GetDiagramLabel(EventBridgeOperationInfo op, EventBridgeTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            EventBridgeTrackingVerbosity.Raw =>
                $"{op.Operation}" +
                (op.EventBusName is not null ? $" bus={op.EventBusName}" : "") +
                (op.DetailType is not null ? $" type={op.DetailType}" : "") +
                (op.EntryCount.HasValue ? $" count={op.EntryCount}" : ""),
            EventBridgeTrackingVerbosity.Detailed => op.Operation switch
            {
                EventBridgeOperation.PutEvents when op.DetailType is not null =>
                    $"PutEvents [{op.DetailType}]" + (op.EntryCount > 1 ? $" x{op.EntryCount}" : ""),
                EventBridgeOperation.PutEvents =>
                    "PutEvents" + (op.EntryCount.HasValue ? $" x{op.EntryCount}" : ""),
                EventBridgeOperation.PutRule => $"PutRule {op.RuleName ?? ""}".TrimEnd(),
                EventBridgeOperation.DeleteRule => $"DeleteRule {op.RuleName ?? ""}".TrimEnd(),
                EventBridgeOperation.PutTargets => "PutTargets",
                EventBridgeOperation.RemoveTargets => "RemoveTargets",
                EventBridgeOperation.CreateEventBus => $"CreateEventBus {op.EventBusName ?? ""}".TrimEnd(),
                EventBridgeOperation.DeleteEventBus => $"DeleteEventBus {op.EventBusName ?? ""}".TrimEnd(),
                _ => op.Operation.ToString()
            },
            EventBridgeTrackingVerbosity.Summarised => op.Operation switch
            {
                EventBridgeOperation.PutEvents => "PutEvents",
                EventBridgeOperation.PutRule or EventBridgeOperation.DeleteRule => "ManageRule",
                EventBridgeOperation.PutTargets or EventBridgeOperation.RemoveTargets => "ManageTargets",
                EventBridgeOperation.CreateEventBus or EventBridgeOperation.DeleteEventBus => "ManageBus",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }
}
