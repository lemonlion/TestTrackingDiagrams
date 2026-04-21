using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.SQS;

public static partial class SqsOperationClassifier
{
    [GeneratedRegex(@"AmazonSQS\.(?<op>\w+)", RegexOptions.Compiled)]
    private static partial Regex TargetRegex();

    [GeneratedRegex(@"/\d+/(?<queue>[^/?]+)", RegexOptions.Compiled)]
    private static partial Regex QueueUrlPathRegex();

    [GeneratedRegex(@"""QueueName""\s*:\s*""(?<queue>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex QueueNameBodyRegex();

    [GeneratedRegex(@"""QueueUrl""\s*:\s*""[^""]*?/\d+/(?<queue>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex QueueUrlBodyRegex();

    [GeneratedRegex(@"Action=(?<op>\w+)", RegexOptions.Compiled)]
    private static partial Regex ActionQueryRegex();

    public static SqsOperationInfo Classify(HttpRequestMessage request, string? requestBody = null)
    {
        string? operationName = null;

        // 1. Try X-Amz-Target header (JSON protocol)
        if (request.Headers.TryGetValues("X-Amz-Target", out var targetValues))
        {
            var targetHeader = targetValues.FirstOrDefault();
            if (targetHeader is not null)
            {
                var match = TargetRegex().Match(targetHeader);
                if (match.Success)
                    operationName = match.Groups["op"].Value;
            }
        }

        // 2. Fallback: Action from query string (query protocol)
        if (operationName is null && request.RequestUri?.Query is { Length: > 0 } query)
        {
            var match = ActionQueryRegex().Match(query);
            if (match.Success)
                operationName = match.Groups["op"].Value;
        }

        // 3. Fallback: Action from form body (query protocol POST)
        if (operationName is null && requestBody is not null)
        {
            var match = ActionQueryRegex().Match(requestBody);
            if (match.Success)
                operationName = match.Groups["op"].Value;
        }

        var operation = operationName is not null ? MapOperation(operationName) : SqsOperation.Other;
        var queueName = ExtractQueueName(request, requestBody);

        return new SqsOperationInfo(operation, queueName);
    }

    public static string? GetDiagramLabel(SqsOperationInfo op, SqsTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            SqsTrackingVerbosity.Summarised or SqsTrackingVerbosity.Detailed
                => op.Operation.ToString(),
            _ => null // Raw: caller uses HTTP method + full target header
        };
    }

    private static SqsOperation MapOperation(string operationName) => operationName switch
    {
        "SendMessage" => SqsOperation.SendMessage,
        "SendMessageBatch" => SqsOperation.SendMessageBatch,
        "ReceiveMessage" => SqsOperation.ReceiveMessage,
        "DeleteMessage" => SqsOperation.DeleteMessage,
        "DeleteMessageBatch" => SqsOperation.DeleteMessageBatch,
        "ChangeMessageVisibility" => SqsOperation.ChangeMessageVisibility,
        "ChangeMessageVisibilityBatch" => SqsOperation.ChangeMessageVisibilityBatch,
        "CreateQueue" => SqsOperation.CreateQueue,
        "DeleteQueue" => SqsOperation.DeleteQueue,
        "GetQueueUrl" => SqsOperation.GetQueueUrl,
        "GetQueueAttributes" => SqsOperation.GetQueueAttributes,
        "SetQueueAttributes" => SqsOperation.SetQueueAttributes,
        "PurgeQueue" => SqsOperation.PurgeQueue,
        "ListQueues" => SqsOperation.ListQueues,
        _ => SqsOperation.Other
    };

    private static string? ExtractQueueName(HttpRequestMessage request, string? requestBody)
    {
        // 1. Try URL path: /account-id/queue-name
        if (request.RequestUri is not null)
        {
            var match = QueueUrlPathRegex().Match(request.RequestUri.AbsolutePath);
            if (match.Success)
                return match.Groups["queue"].Value;
        }

        if (string.IsNullOrEmpty(requestBody)) return null;

        // 2. Try QueueUrl from body
        var urlMatch = QueueUrlBodyRegex().Match(requestBody);
        if (urlMatch.Success)
            return urlMatch.Groups["queue"].Value;

        // 3. Try QueueName from body
        var nameMatch = QueueNameBodyRegex().Match(requestBody);
        if (nameMatch.Success)
            return nameMatch.Groups["queue"].Value;

        return null;
    }
}
