namespace TestTrackingDiagrams.Sql;

public record UnifiedSqlOperationInfo(
    UnifiedSqlOperation Operation,
    string? TableName,
    string? CommandText = null);
