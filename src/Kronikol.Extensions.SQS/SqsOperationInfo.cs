namespace Kronikol.Extensions.SQS;

/// <summary>
/// The result of classifying a SQS operation, containing the operation type and metadata.
/// </summary>
public record SqsOperationInfo(
    SqsOperation Operation,
    string? QueueName);
