using System.Collections;
using System.Data.Common;

namespace TestTrackingDiagrams.Tests.Dapper.Fakes;

public class FakeDbDataReader : DbDataReader
{
    public override int FieldCount => 0;
    public override int RecordsAffected => 0;
    public override bool HasRows => false;
    public override bool IsClosed => true;
    public override int Depth => 0;

    public override object this[int ordinal] => throw new IndexOutOfRangeException();
    public override object this[string name] => throw new IndexOutOfRangeException();

    public override bool Read() => false;
    public override Task<bool> ReadAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    public override bool NextResult() => false;
    public override Task<bool> NextResultAsync(CancellationToken cancellationToken) => Task.FromResult(false);

    public override bool GetBoolean(int ordinal) => throw new IndexOutOfRangeException();
    public override byte GetByte(int ordinal) => throw new IndexOutOfRangeException();
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length) => 0;
    public override char GetChar(int ordinal) => throw new IndexOutOfRangeException();
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length) => 0;
    public override string GetDataTypeName(int ordinal) => throw new IndexOutOfRangeException();
    public override DateTime GetDateTime(int ordinal) => throw new IndexOutOfRangeException();
    public override decimal GetDecimal(int ordinal) => throw new IndexOutOfRangeException();
    public override double GetDouble(int ordinal) => throw new IndexOutOfRangeException();
    public override Type GetFieldType(int ordinal) => throw new IndexOutOfRangeException();
    public override float GetFloat(int ordinal) => throw new IndexOutOfRangeException();
    public override Guid GetGuid(int ordinal) => throw new IndexOutOfRangeException();
    public override short GetInt16(int ordinal) => throw new IndexOutOfRangeException();
    public override int GetInt32(int ordinal) => throw new IndexOutOfRangeException();
    public override long GetInt64(int ordinal) => throw new IndexOutOfRangeException();
    public override string GetName(int ordinal) => throw new IndexOutOfRangeException();
    public override int GetOrdinal(string name) => throw new IndexOutOfRangeException();
    public override string GetString(int ordinal) => throw new IndexOutOfRangeException();
    public override object GetValue(int ordinal) => throw new IndexOutOfRangeException();
    public override int GetValues(object[] values) => 0;
    public override bool IsDBNull(int ordinal) => throw new IndexOutOfRangeException();

    public override IEnumerator GetEnumerator() => Array.Empty<object>().GetEnumerator();
}
