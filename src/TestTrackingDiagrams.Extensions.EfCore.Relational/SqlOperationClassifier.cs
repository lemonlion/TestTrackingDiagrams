using System.Data;
using TestTrackingDiagrams.Sql;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static class SqlOperationClassifier
{
    private static readonly Dictionary<UnifiedSqlOperation, SqlOperation> OperationMap = new()
    {
        [UnifiedSqlOperation.Select] = SqlOperation.Select,
        [UnifiedSqlOperation.Insert] = SqlOperation.Insert,
        [UnifiedSqlOperation.Update] = SqlOperation.Update,
        [UnifiedSqlOperation.Delete] = SqlOperation.Delete,
        [UnifiedSqlOperation.Merge] = SqlOperation.Merge,
        [UnifiedSqlOperation.Upsert] = SqlOperation.Upsert,
        [UnifiedSqlOperation.StoredProcedure] = SqlOperation.StoredProc,
    };

    public static SqlOperationInfo Classify(string? commandText, CommandType commandType = CommandType.Text)
    {
        var unified = UnifiedSqlClassifier.Classify(commandText, commandType);
        var op = OperationMap.GetValueOrDefault(unified.Operation, SqlOperation.Other);
        return new SqlOperationInfo(op, unified.TableName, unified.CommandText);
    }

    public static string? GetDiagramLabel(SqlOperationInfo op, SqlTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            SqlTrackingVerbosity.Summarised or SqlTrackingVerbosity.Detailed => op.Operation.ToString(),
            _ => null
        };
    }

    public static string? GetRawKeyword(string? commandText) =>
        UnifiedSqlClassifier.GetRawKeyword(commandText);
}
