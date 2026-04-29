namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

/// <summary>
/// Classified EfCore.Relational operation types.
/// </summary>
public enum SqlOperation
{
    Select,
    Insert,
    Update,
    Delete,
    Merge,
    Upsert,
    StoredProc,
    Other
}
