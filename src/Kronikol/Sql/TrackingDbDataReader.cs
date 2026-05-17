using System.Collections;
using System.Data.Common;
using System.Text;
using System.Text.Json;

namespace Kronikol.Sql;

/// <summary>
/// Wraps a DbDataReader to capture row data as rows are consumed, then logs
/// the formatted content when the reader is closed or disposed.
/// </summary>
public sealed class TrackingDbDataReader : DbDataReader
{
    private const int MaxDisplayedColumns = 20;

    private readonly DbDataReader _inner;
    private readonly SqlResponseDetail _detail;
    private readonly int _maxRows;
    private readonly int _maxValueLen;
    private readonly Action<string?> _onComplete;
    private readonly List<Dictionary<string, object?>> _capturedRows = [];
    private bool _logged;
    private int _totalRowsRead;

    public TrackingDbDataReader(
        DbDataReader inner,
        SqlResponseDetail detail,
        int maxRows,
        int maxValueLen,
        Action<string?> onComplete)
    {
        _inner = inner;
        _detail = detail;
        _maxRows = Math.Max(0, maxRows);
        _maxValueLen = maxValueLen;
        _onComplete = onComplete;
    }

    // ─── Read + capture ─────────────────────────────────────

    public override bool Read()
    {
        if (!_inner.Read())
        {
            LogIfNotDone();
            return false;
        }

        _totalRowsRead++;
        CaptureCurrentRowIfNeeded();
        return true;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        if (!await _inner.ReadAsync(cancellationToken))
        {
            LogIfNotDone();
            return false;
        }

        _totalRowsRead++;
        CaptureCurrentRowIfNeeded();
        return true;
    }

    private void CaptureCurrentRowIfNeeded()
    {
        if (_detail == SqlResponseDetail.RowCountOnly) return;
        if (_maxRows > 0 && _capturedRows.Count >= _maxRows) return;
        if (_maxRows == 0) return; // row count only when maxRows is 0

        var row = new Dictionary<string, object?>();
        for (var i = 0; i < FieldCount; i++)
        {
            var name = GetName(i);
            if (IsDBNull(i))
            {
                row[name] = null;
            }
            else
            {
                var value = GetValue(i);
                row[name] = FormatCellValue(value);
            }
        }
        _capturedRows.Add(row);
    }

    private object FormatCellValue(object value)
    {
        if (value is byte[] bytes)
            return $"[bytes: {bytes.Length}]";

        var str = value.ToString() ?? "";
        if (str.Length > _maxValueLen)
            return $"{str[.._maxValueLen]}... ({str.Length} chars)";

        return value;
    }

    // ─── Logging ────────────────────────────────────────────

    private void LogIfNotDone()
    {
        if (_logged) return;
        _logged = true;
        _onComplete(FormatContent());
    }

    private string FormatContent()
    {
        var rowLabel = _totalRowsRead == 1 ? "1 row" : $"{_totalRowsRead} rows";

        if (_detail == SqlResponseDetail.RowCountOnly)
            return rowLabel;

        var columnNames = GetColumnNames();

        if (_detail == SqlResponseDetail.RowCountAndColumns || _maxRows == 0)
            return columnNames is not null ? $"{rowLabel} [{columnNames}]" : rowLabel;

        // FullRows
        var sb = new StringBuilder();

        if (_maxRows > 0 && _totalRowsRead > _maxRows)
            sb.Append($"{rowLabel} (showing first {_maxRows})");
        else
            sb.Append(rowLabel);

        if (_capturedRows.Count > 0)
        {
            sb.AppendLine();
            sb.Append(JsonSerializer.Serialize(_capturedRows, JsonOptions));
        }

        if (_maxRows > 0 && _totalRowsRead > _maxRows)
            sb.Append($"\n... ({_totalRowsRead - _maxRows} more)");

        return sb.ToString();
    }

    private string? GetColumnNames()
    {
        if (FieldCount == 0) return null;

        if (FieldCount <= MaxDisplayedColumns)
        {
            var names = new string[FieldCount];
            for (var i = 0; i < FieldCount; i++)
                names[i] = _inner.GetName(i);
            return string.Join(", ", names);
        }

        var displayed = new string[MaxDisplayedColumns];
        for (var i = 0; i < MaxDisplayedColumns; i++)
            displayed[i] = _inner.GetName(i);
        return $"{string.Join(", ", displayed)} ... (+{FieldCount - MaxDisplayedColumns} more)";
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = false,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    // ─── Close / Dispose ────────────────────────────────────

    public override void Close()
    {
        LogIfNotDone();
        _inner.Close();
    }

    public override async Task CloseAsync()
    {
        LogIfNotDone();
        await _inner.CloseAsync();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            LogIfNotDone();
            _inner.Dispose();
        }
        base.Dispose(disposing);
    }

    public override async ValueTask DisposeAsync()
    {
        LogIfNotDone();
        await _inner.DisposeAsync();
        await base.DisposeAsync();
    }

    // ─── Delegation ─────────────────────────────────────────

    public override int FieldCount => _inner.FieldCount;
    public override int RecordsAffected => _inner.RecordsAffected;
    public override bool HasRows => _inner.HasRows;
    public override bool IsClosed => _inner.IsClosed;
    public override int Depth => _inner.Depth;

    public override object this[int ordinal] => _inner[ordinal];
    public override object this[string name] => _inner[name];

    public override string GetName(int ordinal) => _inner.GetName(ordinal);
    public override int GetOrdinal(string name) => _inner.GetOrdinal(name);
    public override object GetValue(int ordinal) => _inner.GetValue(ordinal);
    public override bool IsDBNull(int ordinal) => _inner.IsDBNull(ordinal);
    public override Type GetFieldType(int ordinal) => _inner.GetFieldType(ordinal);
    public override string GetDataTypeName(int ordinal) => _inner.GetDataTypeName(ordinal);

    public override bool GetBoolean(int ordinal) => _inner.GetBoolean(ordinal);
    public override byte GetByte(int ordinal) => _inner.GetByte(ordinal);
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) =>
        _inner.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);
    public override char GetChar(int ordinal) => _inner.GetChar(ordinal);
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) =>
        _inner.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);
    public override DateTime GetDateTime(int ordinal) => _inner.GetDateTime(ordinal);
    public override decimal GetDecimal(int ordinal) => _inner.GetDecimal(ordinal);
    public override double GetDouble(int ordinal) => _inner.GetDouble(ordinal);
    public override float GetFloat(int ordinal) => _inner.GetFloat(ordinal);
    public override Guid GetGuid(int ordinal) => _inner.GetGuid(ordinal);
    public override short GetInt16(int ordinal) => _inner.GetInt16(ordinal);
    public override int GetInt32(int ordinal) => _inner.GetInt32(ordinal);
    public override long GetInt64(int ordinal) => _inner.GetInt64(ordinal);
    public override string GetString(int ordinal) => _inner.GetString(ordinal);
    public override int GetValues(object[] values) => _inner.GetValues(values);
    public override bool NextResult() => _inner.NextResult();
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => _inner.NextResultAsync(cancellationToken);

    public override IEnumerator GetEnumerator() => _inner.GetEnumerator();
}
