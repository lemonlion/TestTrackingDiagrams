using System.Data;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams;

public static partial class DapperOperationClassifier
{
    [GeneratedRegex(@"\bFROM\s+(?:\[?(\w+)\]?\.)?(\[?\w+\]?)", RegexOptions.IgnoreCase)]
    private static partial Regex FromTableRegex();

    [GeneratedRegex(@"\bINSERT\s+INTO\s+(?:\[?(\w+)\]?\.)?(\[?\w+\]?)", RegexOptions.IgnoreCase)]
    private static partial Regex InsertTableRegex();

    [GeneratedRegex(@"\bUPDATE\s+(?:\[?(\w+)\]?\.)?(\[?\w+\]?)", RegexOptions.IgnoreCase)]
    private static partial Regex UpdateTableRegex();

    [GeneratedRegex(@"\bDELETE\s+FROM\s+(?:\[?(\w+)\]?\.)?(\[?\w+\]?)", RegexOptions.IgnoreCase)]
    private static partial Regex DeleteTableRegex();

    public static DapperOperationInfo Classify(string? commandText, CommandType commandType = CommandType.Text)
    {
        if (commandType == CommandType.StoredProcedure)
            return new(DapperOperation.StoredProcedure, null, commandText);

        if (string.IsNullOrWhiteSpace(commandText))
            return new(DapperOperation.Other, null, commandText);

        var trimmed = commandText.TrimStart();

        var (operation, tableRegex) = trimmed switch
        {
            _ when trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Query, FromTableRegex()),
            _ when trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Insert, InsertTableRegex()),
            _ when trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Update, UpdateTableRegex()),
            _ when trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Delete, DeleteTableRegex()),
            _ when trimmed.StartsWith("MERGE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Merge, (Regex?)null),
            _ when trimmed.StartsWith("EXEC", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.StoredProcedure, (Regex?)null),
            _ when trimmed.StartsWith("CREATE TABLE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.CreateTable, (Regex?)null),
            _ when trimmed.StartsWith("ALTER TABLE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.AlterTable, (Regex?)null),
            _ when trimmed.StartsWith("DROP TABLE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.DropTable, (Regex?)null),
            _ when trimmed.StartsWith("CREATE INDEX", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.CreateIndex, (Regex?)null),
            _ when trimmed.StartsWith("TRUNCATE", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Truncate, (Regex?)null),
            _ when trimmed.StartsWith("BEGIN TRAN", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.BeginTransaction, (Regex?)null),
            _ when trimmed.StartsWith("COMMIT", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Commit, (Regex?)null),
            _ when trimmed.StartsWith("ROLLBACK", StringComparison.OrdinalIgnoreCase) =>
                (DapperOperation.Rollback, (Regex?)null),
            _ => (DapperOperation.Other, (Regex?)null)
        };

        string? tableName = null;
        if (tableRegex is not null)
        {
            var match = tableRegex.Match(commandText);
            if (match.Success)
            {
                tableName = match.Groups[2].Success ? match.Groups[2].Value.Trim('[', ']')
                    : match.Groups[1].Success ? match.Groups[1].Value.Trim('[', ']')
                    : null;
            }
        }

        return new(operation, tableName, commandText);
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
                DapperOperation.StoredProcedure => $"EXEC {ExtractProcName(op.CommandText)}",
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

    internal static string ExtractProcName(string? commandText)
    {
        if (commandText is null) return "?";
        var trimmed = commandText.TrimStart();
        if (trimmed.StartsWith("EXEC ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[5..].TrimStart();
        else if (trimmed.StartsWith("EXECUTE ", StringComparison.OrdinalIgnoreCase))
            trimmed = trimmed[8..].TrimStart();

        var spaceIdx = trimmed.IndexOf(' ');
        return spaceIdx > 0 ? trimmed[..spaceIdx] : trimmed;
    }
}
