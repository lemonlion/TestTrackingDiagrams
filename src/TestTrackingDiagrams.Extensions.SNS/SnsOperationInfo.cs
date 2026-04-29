namespace TestTrackingDiagrams.Extensions.SNS;

/// <summary>
/// The result of classifying a SNS operation, containing the operation type and metadata.
/// </summary>
public record SnsOperationInfo(
    SnsOperation Operation,
    string? TopicName,
    string? TopicArn = null);
