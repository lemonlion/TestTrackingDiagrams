namespace TestTrackingDiagrams;

public record DapperOperationInfo(
    DapperOperation Operation,
    string? TableName,
    string? CommandText = null);
