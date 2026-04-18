using System.Data;
using System.Text.RegularExpressions;

namespace TestTrackingDiagrams.Extensions.EfCore.Relational;

public static partial class SqlOperationClassifier
{
    // Strips Spanner statement hints like @{PDML_MAX_PARALLELISM=10}
    [GeneratedRegex(@"^\s*@\{[^}]*\}\s*", RegexOptions.Compiled)]
    private static partial Regex SpannerHintRegex();

    // Strips SET statements before real DML (e.g. SET NOCOUNT ON;\n)
    [GeneratedRegex(@"^\s*SET\s[^;]*;\s*", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex SetPrefixRegex();

    // Strips CTEs: WITH name AS (...) — handles nested parentheses
    [GeneratedRegex(@"^\s*WITH\s+.+?\)\s+(?=SELECT|INSERT|UPDATE|DELETE|MERGE)", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex CtePrefixRegex();

    // Matches the first keyword after prefix stripping
    [GeneratedRegex(@"^\s*(?<keyword>SELECT|INSERT|UPDATE|DELETE|MERGE|EXEC|EXECUTE|CALL|EXPLAIN|COPY|LOAD|BULK|TRUNCATE|BEGIN|CREATE|ALTER|DROP)\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FirstKeywordRegex();

    // Matches INSERT OR (UPDATE|REPLACE|IGNORE) INTO pattern
    [GeneratedRegex(@"^\s*INSERT\s+OR\s+(?<modifier>UPDATE|REPLACE|IGNORE)\s+(?:INTO\s+)?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex InsertOrModifierRegex();

    // Detects ON CONFLICT ... DO UPDATE (PostgreSQL, SQLite, Spanner)
    [GeneratedRegex(@"\bON\s+CONFLICT\b.*?\bDO\s+UPDATE\b", RegexOptions.IgnoreCase | RegexOptions.Singleline | RegexOptions.Compiled)]
    private static partial Regex OnConflictDoUpdateRegex();

    // Detects ON DUPLICATE KEY UPDATE (MySQL)
    [GeneratedRegex(@"\bON\s+DUPLICATE\s+KEY\s+UPDATE\b", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex OnDuplicateKeyUpdateRegex();

    // Table name after FROM (optional)
    [GeneratedRegex(@"\bFROM\s+" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex FromTableRegex();

    // Table name after INTO
    [GeneratedRegex(@"\bINTO\s+" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex IntoTableRegex();

    // Table name after UPDATE keyword (at start)
    [GeneratedRegex(@"^\s*UPDATE\s+" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex UpdateTableRegex();

    // Table name after DELETE (optional FROM)
    [GeneratedRegex(@"^\s*DELETE\s+(?:FROM\s+)?" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex DeleteTableRegex();

    // Table name after MERGE (optional INTO)
    [GeneratedRegex(@"^\s*MERGE\s+(?:INTO\s+)?" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex MergeTableRegex();

    // Proc name after EXEC/EXECUTE
    [GeneratedRegex(@"^\s*(?:EXEC|EXECUTE)\s+" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex ExecProcRegex();

    // Proc name after CALL
    [GeneratedRegex(@"^\s*CALL\s+" + @"(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase | RegexOptions.Compiled)]
    private static partial Regex CallProcRegex();

    public static SqlOperationInfo Classify(string? commandText, CommandType commandType = CommandType.Text)
    {
        if (commandType == CommandType.StoredProcedure)
        {
            var procName = ExtractLastIdentifierPart(commandText);
            return new SqlOperationInfo(SqlOperation.StoredProc, procName, commandText);
        }

        if (string.IsNullOrWhiteSpace(commandText))
            return new SqlOperationInfo(SqlOperation.Other, null, commandText);

        // Strip prefixes: Spanner hints, SET statements, CTEs
        var sql = commandText;
        sql = SpannerHintRegex().Replace(sql, "");
        sql = SetPrefixRegex().Replace(sql, "");
        sql = CtePrefixRegex().Replace(sql, "");

        var keywordMatch = FirstKeywordRegex().Match(sql);
        if (!keywordMatch.Success)
            return new SqlOperationInfo(SqlOperation.Other, null, commandText);

        var keyword = keywordMatch.Groups["keyword"].Value.ToUpperInvariant();

        return keyword switch
        {
            "SELECT" => new SqlOperationInfo(SqlOperation.Select, ExtractSelectTable(sql), commandText),
            "INSERT" => ClassifyInsert(sql, commandText),
            "UPDATE" => new SqlOperationInfo(SqlOperation.Update, ExtractUpdateTable(sql), commandText),
            "DELETE" => new SqlOperationInfo(SqlOperation.Delete, ExtractDeleteTable(sql), commandText),
            "MERGE" => new SqlOperationInfo(SqlOperation.Merge, ExtractMergeTable(sql), commandText),
            "EXEC" or "EXECUTE" => new SqlOperationInfo(SqlOperation.StoredProc, ExtractExecProc(sql), commandText),
            "CALL" => new SqlOperationInfo(SqlOperation.StoredProc, ExtractCallProc(sql), commandText),
            _ => new SqlOperationInfo(SqlOperation.Other, null, commandText),
        };
    }

    public static string? GetDiagramLabel(SqlOperationInfo op, SqlTrackingVerbosity verbosity)
    {
        return verbosity switch
        {
            SqlTrackingVerbosity.Summarised or SqlTrackingVerbosity.Detailed => op.Operation.ToString(),
            _ => null
        };
    }

    public static string? GetRawKeyword(string? commandText)
    {
        if (string.IsNullOrWhiteSpace(commandText))
            return null;

        var sql = commandText;
        sql = SpannerHintRegex().Replace(sql, "");
        sql = SetPrefixRegex().Replace(sql, "");
        sql = CtePrefixRegex().Replace(sql, "");

        var match = FirstKeywordRegex().Match(sql);
        return match.Success ? match.Groups["keyword"].Value.ToUpperInvariant() : null;
    }

    private static SqlOperationInfo ClassifyInsert(string sql, string? originalText)
    {
        // Check INSERT OR (UPDATE|REPLACE|IGNORE) pattern
        var orMatch = InsertOrModifierRegex().Match(sql);
        if (orMatch.Success)
        {
            var modifier = orMatch.Groups["modifier"].Value.ToUpperInvariant();
            var tableName = ExtractIntoTable(sql) ?? ExtractTableAfterInsertOr(sql);
            return modifier switch
            {
                "UPDATE" or "REPLACE" => new SqlOperationInfo(SqlOperation.Upsert, tableName, originalText),
                _ => new SqlOperationInfo(SqlOperation.Insert, tableName, originalText), // IGNORE
            };
        }

        var table = ExtractIntoTable(sql);

        // Check ON CONFLICT DO UPDATE (PostgreSQL, SQLite, Spanner)
        if (OnConflictDoUpdateRegex().IsMatch(sql))
            return new SqlOperationInfo(SqlOperation.Upsert, table, originalText);

        // Check ON DUPLICATE KEY UPDATE (MySQL)
        if (OnDuplicateKeyUpdateRegex().IsMatch(sql))
            return new SqlOperationInfo(SqlOperation.Upsert, table, originalText);

        return new SqlOperationInfo(SqlOperation.Insert, table, originalText);
    }

    private static string? ExtractSelectTable(string sql)
    {
        var match = FromTableRegex().Match(sql);
        if (!match.Success) return null;

        var table = match.Groups["table"].Value;
        // Subquery detection: if table starts with ( it's a subquery
        return table.StartsWith('(') ? null : StripQuotes(table);
    }

    private static string? ExtractIntoTable(string sql)
    {
        var match = IntoTableRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractTableAfterInsertOr(string sql)
    {
        // For INSERT OR UPDATE INTO Table — try FROM after stripping INSERT OR modifier
        var match = IntoTableRegex().Match(sql);
        if (match.Success) return StripQuotes(match.Groups["table"].Value);

        // Fallback: look for table name directly after the modifier
        var afterModifier = InsertOrModifierRegex().Replace(sql, "");
        var identMatch = Regex.Match(afterModifier, @"^\s*(?:[\[\]""`]?[\w=+/]+[\[\]""`]?\.)*[\[\]""`]?(?<table>[\w=+/]+)[\[\]""`]?", RegexOptions.IgnoreCase);
        return identMatch.Success ? StripQuotes(identMatch.Groups["table"].Value) : null;
    }

    private static string? ExtractUpdateTable(string sql)
    {
        var match = UpdateTableRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractDeleteTable(string sql)
    {
        var match = DeleteTableRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractMergeTable(string sql)
    {
        var match = MergeTableRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractExecProc(string sql)
    {
        var match = ExecProcRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractCallProc(string sql)
    {
        var match = CallProcRegex().Match(sql);
        return match.Success ? StripQuotes(match.Groups["table"].Value) : null;
    }

    private static string? ExtractLastIdentifierPart(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var clean = StripQuotes(text.Trim());
        var dotIndex = clean.LastIndexOf('.');
        return dotIndex >= 0 ? clean[(dotIndex + 1)..] : clean;
    }

    private static string StripQuotes(string value)
    {
        return value
            .Replace("[", "")
            .Replace("]", "")
            .Replace("\"", "")
            .Replace("`", "");
    }
}
