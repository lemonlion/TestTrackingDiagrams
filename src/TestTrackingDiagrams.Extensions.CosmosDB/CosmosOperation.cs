namespace TestTrackingDiagrams.Extensions.CosmosDB;

/// <summary>
/// Classified CosmosDB operation types.
/// </summary>
public enum CosmosOperation
{
    Create,
    Read,
    Replace,
    Patch,
    Delete,
    Upsert,
    Query,
    List,
    ExecStoredProc,
    Batch,
    Other
}
