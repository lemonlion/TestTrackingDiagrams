namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

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
