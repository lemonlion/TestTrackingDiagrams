using System.Data;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.Spanner;

/// <summary>
/// Classifies Google Cloud Spanner HTTP requests into specific operations based on URL patterns and HTTP methods.
/// </summary>
public static partial class SpannerOperationClassifier
{
    [GeneratedRegex(@"\bFROM\s+`?(\w+)`?", RegexOptions.IgnoreCase)]
    private static partial Regex FromTableRegex();

    [GeneratedRegex(@"\bINSERT\s+(?:INTO\s+)?`?(\w+)`?", RegexOptions.IgnoreCase)]
    private static partial Regex InsertTableRegex();

    [GeneratedRegex(@"\bUPDATE\s+`?(\w+)`?", RegexOptions.IgnoreCase)]
    private static partial Regex UpdateTableRegex();

    [GeneratedRegex(@"\bDELETE\s+FROM\s+`?(\w+)`?", RegexOptions.IgnoreCase)]
    private static partial Regex DeleteTableRegex();

    /// <summary>
    /// Classify a SQL command (ADO.NET path via SpannerConnection).
    /// </summary>
    public static SpannerOperationInfo ClassifySql(string? commandText, CommandType commandType = CommandType.Text)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return new SpannerOperationInfo(SpannerOperation.Other);

        var trimmed = commandText.TrimStart();

        var (operation, tableRegex) = trimmed switch
        {
            _ when trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Query, FromTableRegex()),
            _ when trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Insert, InsertTableRegex()),
            _ when trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Update, UpdateTableRegex()),
            _ when trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Delete, DeleteTableRegex()),
            _ when trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Ddl, (Regex?)null),
            _ when trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Ddl, (Regex?)null),
            _ when trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase) =>
                (SpannerOperation.Ddl, (Regex?)null),
            _ => (SpannerOperation.Other, (Regex?)null)
        };

        string? tableName = null;
        if (tableRegex is not null)
        {
            var match = tableRegex.Match(commandText);
            if (match.Success)
                tableName = match.Groups[1].Value.Trim('`');
        }

        return new SpannerOperationInfo(operation, tableName, SqlText: commandText);
    }

    /// <summary>
    /// Classify a gRPC method name (low-level SpannerClient path).
    /// </summary>
    public static SpannerOperationInfo ClassifyGrpc(string methodName, string? tableName = null, string? databaseId = null)
    {
        var operation = methodName switch
        {
            "ExecuteSql" or "ExecuteSqlAsync" => SpannerOperation.Query,
            "ExecuteStreamingSql" or "ExecuteStreamingSqlAsync" => SpannerOperation.Query,
            "Read" or "ReadAsync" => SpannerOperation.Read,
            "StreamingRead" or "StreamingReadAsync" => SpannerOperation.StreamingRead,
            "Commit" or "CommitAsync" => SpannerOperation.Commit,
            "Rollback" or "RollbackAsync" => SpannerOperation.Rollback,
            "BeginTransaction" or "BeginTransactionAsync" => SpannerOperation.BeginTransaction,
            "ExecuteBatchDml" or "ExecuteBatchDmlAsync" => SpannerOperation.BatchDml,
            "PartitionQuery" or "PartitionQueryAsync" => SpannerOperation.PartitionQuery,
            "PartitionRead" or "PartitionReadAsync" => SpannerOperation.PartitionRead,
            "CreateSession" or "CreateSessionAsync" => SpannerOperation.CreateSession,
            "BatchCreateSessions" or "BatchCreateSessionsAsync" => SpannerOperation.CreateSession,
            "DeleteSession" or "DeleteSessionAsync" => SpannerOperation.DeleteSession,
            _ => SpannerOperation.Other
        };

        return new SpannerOperationInfo(operation, tableName, databaseId);
    }

    public static string GetDiagramLabel(SpannerOperationInfo op, SpannerTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            SpannerTrackingVerbosity.Raw => op.SqlText ?? op.Operation.ToString(),
            SpannerTrackingVerbosity.Detailed => op.Operation switch
            {
                SpannerOperation.Query => $"SELECT FROM {op.TableName ?? "?"}",
                SpannerOperation.Insert => $"INSERT INTO {op.TableName ?? "?"}",
                SpannerOperation.Update => $"UPDATE {op.TableName ?? "?"}",
                SpannerOperation.Delete => $"DELETE FROM {op.TableName ?? "?"}",
                SpannerOperation.Read => $"Read {op.TableName ?? "?"}",
                SpannerOperation.StreamingRead => $"StreamingRead {op.TableName ?? "?"}",
                SpannerOperation.InsertOrUpdate => $"InsertOrUpdate {op.TableName ?? "?"}",
                SpannerOperation.Replace => $"Replace {op.TableName ?? "?"}",
                SpannerOperation.BatchDml => "BatchDml",
                SpannerOperation.PartitionQuery => "PartitionQuery",
                SpannerOperation.PartitionRead => "PartitionRead",
                _ => op.Operation.ToString()
            },
            SpannerTrackingVerbosity.Summarised => op.Operation switch
            {
                SpannerOperation.Query => "SELECT",
                SpannerOperation.Insert => "INSERT",
                SpannerOperation.Update => "UPDATE",
                SpannerOperation.Delete => "DELETE",
                SpannerOperation.StreamingRead => "Read",
                _ => op.Operation.ToString()
            },
            _ => op.Operation.ToString()
        };
    }

    /// <summary>
    /// Gets a short keyword suitable for component diagram arrow labels from a Spanner operation.
    /// For SQL operations, extracts the SQL keyword (Select, Insert, etc.).
    /// For non-SQL operations (mutations), returns the operation name.
    /// </summary>
    public static string? GetRawKeyword(SpannerOperationInfo op)
    {
        if (op.SqlText is not null)
            return GetRawKeyword(op.SqlText);

        return op.Operation.ToString();
    }

    /// <summary>
    /// Gets the raw SQL keyword from command text (for Raw verbosity arrow labels).
    /// Returns null if command text is null/empty or doesn't start with a known SQL keyword.
    /// </summary>
    public static string? GetRawKeyword(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return null;

        var trimmed = commandText.TrimStart();

        return trimmed switch
        {
            _ when trimmed.StartsWith("SELECT", StringComparison.OrdinalIgnoreCase) => "Select",
            _ when trimmed.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase) => "Insert",
            _ when trimmed.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase) => "Update",
            _ when trimmed.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase) => "Delete",
            _ when trimmed.StartsWith("CREATE", StringComparison.OrdinalIgnoreCase) => "Create",
            _ when trimmed.StartsWith("ALTER", StringComparison.OrdinalIgnoreCase) => "Alter",
            _ when trimmed.StartsWith("DROP", StringComparison.OrdinalIgnoreCase) => "Drop",
            _ => null
        };
    }
}