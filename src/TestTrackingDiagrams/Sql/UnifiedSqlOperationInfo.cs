namespace TestTrackingDiagrams.Sql;

/// <summary>
/// The result of classifying a SQL command text via <see cref="UnifiedSqlClassifier.Classify"/>.
/// Contains the classified operation type, the target table name (if detected), and the original command text.
/// </summary>
public record UnifiedSqlOperationInfo(
    UnifiedSqlOperation Operation,
    string? TableName,
    string? CommandText = null);
