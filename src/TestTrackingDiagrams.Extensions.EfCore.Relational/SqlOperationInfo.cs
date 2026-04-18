namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public record SqlOperationInfo(
    SqlOperation Operation,
    string? TableName,
    string? CommandText);
