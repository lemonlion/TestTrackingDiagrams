using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.SNS;

public static partial class SnsOperationClassifier
{
    [GeneratedRegex(@"AmazonSimpleNotificationService\.(?<op>\w+)", RegexOptions.Compiled)]
    private static partial Regex TargetRegex();

    [GeneratedRegex(@"""(?:TopicArn|TargetArn)""\s*:\s*""arn:aws:sns:[^:]+:[^:]+:(?<topic>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex TopicArnBodyRegex();

    [GeneratedRegex(@"""(?:TopicArn|TargetArn)""\s*:\s*""(?<arn>[^""]+)""", RegexOptions.Compiled)]
    private static partial Regex FullArnBodyRegex();

    [GeneratedRegex(@"Action=(?<op>\w+)", RegexOptions.Compiled)]
    private static partial Regex ActionQueryRegex();

    public static SnsOperationInfo Classify(HttpRequestMessage request, string? requestBody = null)
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

        var operation = operationName is not null ? MapOperation(operationName) : SnsOperation.Other;
        var (topicName, topicArn) = ExtractTopicInfo(requestBody);

        return new SnsOperationInfo(operation, topicName, topicArn);
    }

    public static string? GetDiagramLabel(SnsOperationInfo op, SnsTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            SnsTrackingVerbosity.Summarised or SnsTrackingVerbosity.Detailed
                => op.Operation.ToString(),
            _ => null // Raw: caller uses HTTP method + full target header
        };
    }

    private static SnsOperation MapOperation(string operationName) => operationName switch
    {
        "Publish" => SnsOperation.Publish,
        "PublishBatch" => SnsOperation.PublishBatch,
        "Subscribe" => SnsOperation.Subscribe,
        "Unsubscribe" => SnsOperation.Unsubscribe,
        "CreateTopic" => SnsOperation.CreateTopic,
        "DeleteTopic" => SnsOperation.DeleteTopic,
        "ListTopics" => SnsOperation.ListTopics,
        "ListSubscriptions" => SnsOperation.ListSubscriptions,
        "ListSubscriptionsByTopic" => SnsOperation.ListSubscriptionsByTopic,
        "GetTopicAttributes" => SnsOperation.GetTopicAttributes,
        "SetTopicAttributes" => SnsOperation.SetTopicAttributes,
        "ConfirmSubscription" => SnsOperation.ConfirmSubscription,
        _ => SnsOperation.Other
    };

    private static (string? TopicName, string? TopicArn) ExtractTopicInfo(string? requestBody)
    {
        if (string.IsNullOrEmpty(requestBody)) return (null, null);

        // Try to extract topic name from ARN
        var arnMatch = TopicArnBodyRegex().Match(requestBody);
        if (arnMatch.Success)
        {
            var topicName = arnMatch.Groups["topic"].Value;
            var fullArnMatch = FullArnBodyRegex().Match(requestBody);
            var fullArn = fullArnMatch.Success ? fullArnMatch.Groups["arn"].Value : null;
            return (topicName, fullArn);
        }

        return (null, null);
    }
}
