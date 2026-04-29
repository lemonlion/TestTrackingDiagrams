namespace TestTrackingDiagrams.Extensions.BigQuery;

/// <summary>
/// Classified BigQuery operation types.
/// </summary>
public enum BigQueryOperation
{
    Query,
    Insert,
    Read,
    List,
    Create,
    Delete,
    Update,
    Cancel,
    Other
}
