namespace TestTrackingDiagrams.Extensions.Bigtable;

/// <summary>
/// Classified Bigtable operation types.
/// </summary>
public enum BigtableOperation
{
    ReadRows,
    MutateRow,
    MutateRows,
    CheckAndMutateRow,
    ReadModifyWriteRow,
    SampleRowKeys,
    Other
}
