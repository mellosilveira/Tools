using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests;

public sealed class DictionaryExtensionsTests
{
    // ── Basic property mapping ─────────────────────────────────────────────────

    [Fact]
    public void ConvertTo_StringAndIntProperties_AreMappedByName()
    {
        var reader = new FakeDataReader(("Name", "Widget"), ("Count", 7));

        var result = reader.ConvertTo<SimpleDto>();

        Assert.Equal("Widget", result.Name);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void ConvertTo_DbNullColumn_LeavesPropertyAtDefault()
    {
        var reader = new FakeDataReader(("Name", DBNull.Value), ("Count", 0));

        var result = reader.ConvertTo<SimpleDto>();

        // DBNull is skipped — property stays at its type default
        Assert.Null(result.Name);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ConvertTo_UnknownColumnName_IsIgnored()
    {
        var reader = new FakeDataReader(("NonExistent", "ignored"), ("Name", "Widget"));

        var result = reader.ConvertTo<SimpleDto>();

        Assert.Equal("Widget", result.Name);
    }

    // ── DateTimeOffset handling (covers both nullable and non-nullable) ────────

    [Fact]
    public void ConvertTo_DateTimeOffsetProperty_FromDateTimeValue()
    {
        // Npgsql can return DateTime for timestamptz columns in legacy mode.
        var dt = new DateTime(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        var reader = new FakeDataReader(("Timestamp", dt));

        var result = reader.ConvertTo<TimestampDto>();

        Assert.Equal(new DateTimeOffset(dt), result.Timestamp);
    }

    [Fact]
    public void ConvertTo_DateTimeOffsetProperty_FromDateTimeOffsetValue()
    {
        var dto = new DateTimeOffset(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        var reader = new FakeDataReader(("Timestamp", dto));

        var result = reader.ConvertTo<TimestampDto>();

        Assert.Equal(dto, result.Timestamp);
    }

    [Fact]
    public void ConvertTo_NullableDateTimeOffsetProperty_FromDateTimeValue()
    {
        // Regression: before the fix, DateTimeOffset? fell through to Convert.ChangeType
        // which threw InvalidCastException for DateTime→DateTimeOffset.
        var dt = new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        var reader = new FakeDataReader(("Timestamp", dt));

        var result = reader.ConvertTo<NullableTimestampDto>();

        Assert.Equal(new DateTimeOffset(dt), result.Timestamp);
    }

    [Fact]
    public void ConvertTo_NullableDateTimeOffsetProperty_DbNull_RemainsNull()
    {
        var reader = new FakeDataReader(("Timestamp", DBNull.Value));

        var result = reader.ConvertTo<NullableTimestampDto>();

        Assert.Null(result.Timestamp);
    }

    // ── Enum handling ──────────────────────────────────────────────────────────

    [Fact]
    public void ConvertTo_EnumProperty_MappedFromIntValue()
    {
        var reader = new FakeDataReader(("Status", 1));

        var result = reader.ConvertTo<EnumDto>();

        Assert.Equal(TestStatus.Active, result.Status);
    }

    [Fact]
    public void ConvertTo_NullableEnumProperty_MappedFromIntValue()
    {
        var reader = new FakeDataReader(("Status", 2));

        var result = reader.ConvertTo<NullableEnumDto>();

        Assert.Equal(TestStatus.Inactive, result.Status);
    }

    // ── Caching: setters are reused across multiple instances ─────────────────

    [Fact]
    public void ConvertTo_CalledTwice_ProducesSameResult()
    {
        var reader1 = new FakeDataReader(("Name", "A"), ("Count", 1));
        var reader2 = new FakeDataReader(("Name", "B"), ("Count", 2));

        var r1 = reader1.ConvertTo<SimpleDto>();
        var r2 = reader2.ConvertTo<SimpleDto>();

        Assert.Equal("A", r1.Name);
        Assert.Equal("B", r2.Name);
    }

    // ── Test DTOs ──────────────────────────────────────────────────────────────

    public sealed class SimpleDto
    {
        public string? Name { get; set; }
        public int Count { get; set; }
    }

    public sealed class TimestampDto
    {
        public DateTimeOffset Timestamp { get; set; }
    }

    public sealed class NullableTimestampDto
    {
        public DateTimeOffset? Timestamp { get; set; }
    }

    public sealed class EnumDto
    {
        public TestStatus Status { get; set; }
    }

    public sealed class NullableEnumDto
    {
        public TestStatus? Status { get; set; }
    }

    public enum TestStatus { Active = 1, Inactive = 2 }
}
