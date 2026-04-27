namespace TestTrackingDiagrams.Extensions.Bigtable;

public static class BigtableOperationClassifier
{
    public static BigtableOperationInfo Classify(
        string methodName, string? tableName = null, string? rowKey = null, int? mutationCount = null)
    {
        var operation = methodName switch
        {
            "ReadRows" or "ReadRowsAsync" => BigtableOperation.ReadRows,
            "MutateRow" or "MutateRowAsync" => BigtableOperation.MutateRow,
            "MutateRows" or "MutateRowsAsync" => BigtableOperation.MutateRows,
            "CheckAndMutateRow" or "CheckAndMutateRowAsync" => BigtableOperation.CheckAndMutateRow,
            "ReadModifyWriteRow" or "ReadModifyWriteRowAsync" => BigtableOperation.ReadModifyWriteRow,
            "SampleRowKeys" or "SampleRowKeysAsync" => BigtableOperation.SampleRowKeys,
            _ => BigtableOperation.Other
        };

        return new BigtableOperationInfo(operation, tableName, rowKey, mutationCount);
    }

    public static string GetDiagramLabel(BigtableOperationInfo op, BigtableTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            BigtableTrackingVerbosity.Raw =>
                $"{op.Operation} table={op.TableName} row={op.RowKey}",
            BigtableTrackingVerbosity.Detailed => op.Operation switch
            {
                BigtableOperation.ReadRows => $"ReadRows ← {ShortTableName(op.TableName)}",
                BigtableOperation.MutateRow => $"MutateRow → {ShortTableName(op.TableName)}",
                BigtableOperation.MutateRows => op.MutationCount.HasValue
                    ? $"MutateRows (×{op.MutationCount}) → {ShortTableName(op.TableName)}"
                    : $"MutateRows → {ShortTableName(op.TableName)}",
                BigtableOperation.CheckAndMutateRow => $"CheckAndMutate → {ShortTableName(op.TableName)}",
                BigtableOperation.ReadModifyWriteRow => $"ReadModifyWrite → {ShortTableName(op.TableName)}",
                BigtableOperation.SampleRowKeys => $"SampleRowKeys ← {ShortTableName(op.TableName)}",
                _ => op.Operation.ToString()
            },
            BigtableTrackingVerbosity.Summarised => op.Operation switch
            {
                BigtableOperation.MutateRows => "MutateRow",
                BigtableOperation.CheckAndMutateRow => "CheckAndMutate",
                BigtableOperation.ReadModifyWriteRow => "ReadModifyWrite",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    private static string ShortTableName(string? fullName)
    {
        if (fullName is null) return "?";
        // Bigtable table names: projects/{project}/instances/{instance}/tables/{table}
        var lastSlash = fullName.LastIndexOf('/');
        return lastSlash >= 0 ? fullName[(lastSlash + 1)..] : fullName;
    }
}
