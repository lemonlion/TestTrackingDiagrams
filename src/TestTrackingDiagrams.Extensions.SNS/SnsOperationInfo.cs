namespace TestTrackingDiagrams.Extensions.SNS;

public record SnsOperationInfo(
    SnsOperation Operation,
    string? TopicName,
    string? TopicArn = null);
