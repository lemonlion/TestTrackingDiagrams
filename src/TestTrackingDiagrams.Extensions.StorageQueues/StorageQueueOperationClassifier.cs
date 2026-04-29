using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.StorageQueues;

/// <summary>
/// Classifies Azure Storage Queues HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class StorageQueueOperationClassifier
{
    [GeneratedRegex(@"^/(?<queue>[^/?]+)(?:/messages(?:/(?<msgId>[^/?]+))?)?$", RegexOptions.Compiled)]
    private static partial Regex QueuePathRegex();

    public static StorageQueueOperationInfo Classify(HttpRequestMessage request)
    {
        var path = request.RequestUri?.AbsolutePath ?? "";
        var method = request.Method;
        var query = request.RequestUri?.Query ?? "";

        // List queues: GET /?comp=list
        if (path == "/" && query.Contains("comp=list"))
            return new(StorageQueueOperation.ListQueues, null);

        var match = QueuePathRegex().Match(path);
        if (!match.Success)
            return new(StorageQueueOperation.Other, null);

        var queue = match.Groups["queue"].Value;
        var messageId = match.Groups["msgId"].Success && match.Groups["msgId"].Value.Length > 0
            ? match.Groups["msgId"].Value : null;
        var hasMessages = path.Contains("/messages");

        return (method.Method, hasMessages, messageId is not null) switch
        {
            ("POST", true, false) => new(StorageQueueOperation.SendMessage, queue),
            ("GET", true, false) when query.Contains("peekonly=true") =>
                new(StorageQueueOperation.PeekMessages, queue),
            ("GET", true, false) => new(StorageQueueOperation.ReceiveMessages, queue),
            ("DELETE", true, true) => new(StorageQueueOperation.DeleteMessage, queue, messageId),
            ("PUT", true, true) => new(StorageQueueOperation.UpdateMessage, queue, messageId),
            ("DELETE", true, false) => new(StorageQueueOperation.ClearMessages, queue),
            ("PUT", false, _) when query.Contains("comp=metadata") =>
                new(StorageQueueOperation.SetMetadata, queue),
            ("GET", false, _) when query.Contains("comp=metadata") =>
                new(StorageQueueOperation.GetProperties, queue),
            ("PUT", false, _) => new(StorageQueueOperation.CreateQueue, queue),
            ("DELETE", false, _) => new(StorageQueueOperation.DeleteQueue, queue),
            _ => new(StorageQueueOperation.Other, queue)
        };
    }

    public static string GetDiagramLabel(StorageQueueOperationInfo op, StorageQueueTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            StorageQueueTrackingVerbosity.Detailed => op.Operation switch
            {
                StorageQueueOperation.SendMessage => $"Send → {op.QueueName}",
                StorageQueueOperation.ReceiveMessages => $"Receive ← {op.QueueName}",
                StorageQueueOperation.PeekMessages => $"Peek ← {op.QueueName}",
                StorageQueueOperation.DeleteMessage => "Delete",
                StorageQueueOperation.UpdateMessage => "Update",
                StorageQueueOperation.ClearMessages => $"Clear → {op.QueueName}",
                _ => op.Operation.ToString()
            },
            StorageQueueTrackingVerbosity.Summarised => op.Operation.ToString(),
            _ => op.Operation.ToString() // Raw uses HTTP method
        };
    }
}