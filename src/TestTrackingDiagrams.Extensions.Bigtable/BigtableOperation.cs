namespace TestTrackingDiagrams.Extensions.Bigtable;

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
