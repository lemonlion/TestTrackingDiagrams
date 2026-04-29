using System.Data;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams;

/// <summary>
/// Classifies Dapper HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static class DapperOperationClassifier
{
    private static readonly Dictionary<UnifiedSqlOperation, DapperOperation> OperationMap = new()
    {
        [UnifiedSqlOperation.Select] = DapperOperation.Query,
        [UnifiedSqlOperation.Insert] = DapperOperation.Insert,
        [UnifiedSqlOperation.Update] = DapperOperation.Update,
        [UnifiedSqlOperation.Delete] = DapperOperation.Delete,
        [UnifiedSqlOperation.Merge] = DapperOperation.Merge,
        [UnifiedSqlOperation.Upsert] = DapperOperation.Insert, // Dapper has no Upsert — map to Insert
        [UnifiedSqlOperation.StoredProcedure] = DapperOperation.StoredProcedure,
        [UnifiedSqlOperation.CreateTable] = DapperOperation.CreateTable,
        [UnifiedSqlOperation.AlterTable] = DapperOperation.AlterTable,
        [UnifiedSqlOperation.DropTable] = DapperOperation.DropTable,
        [UnifiedSqlOperation.CreateIndex] = DapperOperation.CreateIndex,
        [UnifiedSqlOperation.Truncate] = DapperOperation.Truncate,
        [UnifiedSqlOperation.BeginTransaction] = DapperOperation.BeginTransaction,
        [UnifiedSqlOperation.Commit] = DapperOperation.Commit,
        [UnifiedSqlOperation.Rollback] = DapperOperation.Rollback,
    };

    public static DapperOperationInfo Classify(string? commandText, CommandType commandType = CommandType.Text)
    {
        var unified = UnifiedSqlClassifier.Classify(commandText, commandType);
        var op = OperationMap.GetValueOrDefault(unified.Operation, DapperOperation.Other);
        // Dapper convention: StoredProcedure has null table name (proc name goes in CommandText)
        var tableName = op == DapperOperation.StoredProcedure && commandType == CommandType.StoredProcedure
            ? null
            : unified.TableName;
        return new(op, tableName, unified.CommandText);
    }

    public static string GetDiagramLabel(DapperOperationInfo op, DapperTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            DapperTrackingVerbosity.Raw => op.CommandText ?? op.Operation.ToString(),
            DapperTrackingVerbosity.Detailed => op.Operation switch
            {
                DapperOperation.Query => $"SELECT FROM {op.TableName ?? "?"}",
                DapperOperation.Insert => $"INSERT INTO {op.TableName ?? "?"}",
                DapperOperation.Update => $"UPDATE {op.TableName ?? "?"}",
                DapperOperation.Delete => $"DELETE FROM {op.TableName ?? "?"}",
                DapperOperation.StoredProcedure => $"EXEC {UnifiedSqlClassifier.ExtractProcName(op.CommandText)}",
                _ => op.Operation.ToString()
            },
            DapperTrackingVerbosity.Summarised => op.Operation switch
            {
                DapperOperation.Query => "SELECT",
                DapperOperation.Insert => "INSERT",
                DapperOperation.Update => "UPDATE",
                DapperOperation.Delete => "DELETE",
                DapperOperation.StoredProcedure => "EXEC",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    internal static string ExtractProcName(string? commandText) =>
        UnifiedSqlClassifier.ExtractProcName(commandText);
}