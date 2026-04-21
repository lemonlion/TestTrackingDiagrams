namespace TestTrackingDiagrams.Extensions.SQS;

public record SqsOperationInfo(
    SqsOperation Operation,
    string? QueueName);
