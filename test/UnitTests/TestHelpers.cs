using System.Data;

namespace UnitTests;

/// <summary>
/// Minimal IDataReader backed by an in-memory column array.
/// Supports a single row; subsequent Read() calls return false.
/// </summary>
internal sealed class FakeDataReader : IDataReader
{
    private readonly (string Name, object? Value)[] _columns;
    private int _readCount;

    public FakeDataReader(params (string Name, object? Value)[] columns)
        => _columns = columns;

    // ── Core members used by ConvertTo<T> ─────────────────────────────────────
    public int FieldCount => _columns.Length;
    public bool Read() => _readCount++ == 0;
    public bool IsDBNull(int i) => _columns[i].Value is null or DBNull;
    public string GetName(int i) => _columns[i].Name;
    public object GetValue(int i) => _columns[i].Value ?? DBNull.Value;

    // ── Unused interface members ───────────────────────────────────────────────
    public void Dispose() { }
    public void Close() { }
    public bool NextResult() => false;
    public int Depth => 0;
    public bool IsClosed => false;
    public int RecordsAffected => 0;

    public DataTable? GetSchemaTable() => null;
    public int GetOrdinal(string name) => throw new NotImplementedException();
    public string GetDataTypeName(int i) => throw new NotImplementedException();
    public Type GetFieldType(int i) => throw new NotImplementedException();
    public int GetValues(object[] values) => throw new NotImplementedException();
    public bool GetBoolean(int i) => throw new NotImplementedException();
    public byte GetByte(int i) => throw new NotImplementedException();
    public long GetBytes(int i, long fo, byte[]? b, int bo, int l) => throw new NotImplementedException();
    public char GetChar(int i) => throw new NotImplementedException();
    public long GetChars(int i, long fo, char[]? b, int bo, int l) => throw new NotImplementedException();
    public IDataReader GetData(int i) => throw new NotImplementedException();
    public DateTime GetDateTime(int i) => throw new NotImplementedException();
    public decimal GetDecimal(int i) => throw new NotImplementedException();
    public double GetDouble(int i) => throw new NotImplementedException();
    public float GetFloat(int i) => throw new NotImplementedException();
    public Guid GetGuid(int i) => throw new NotImplementedException();
    public short GetInt16(int i) => throw new NotImplementedException();
    public int GetInt32(int i) => throw new NotImplementedException();
    public long GetInt64(int i) => throw new NotImplementedException();
    public string GetString(int i) => throw new NotImplementedException();
    public object this[int i] => GetValue(i);
    public object this[string name] => throw new NotImplementedException();
}
