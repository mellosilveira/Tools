using MelloSilveiraTools.Core.ExtensionMethods;

namespace UnitTests.Core.ExtensionMethods;

public sealed class DictionaryExtensionsTests
{
    // ── Basic property mapping ─────────────────────────────────────────────────

    [Fact]
    public void ConvertTo_StringAndIntProperties_AreMappedByName()
    {
        FakeDataReader reader = new(("Name", "Widget"), ("Count", 7));

        SimpleDto result = reader.ConvertTo<SimpleDto>();

        Assert.Equal("Widget", result.Name);
        Assert.Equal(7, result.Count);
    }

    [Fact]
    public void ConvertTo_DbNullColumn_LeavesPropertyAtDefault()
    {
        FakeDataReader reader = new(("Name", DBNull.Value), ("Count", 0));

        SimpleDto result = reader.ConvertTo<SimpleDto>();

        // DBNull is skipped — property stays at its type default
        Assert.Null(result.Name);
        Assert.Equal(0, result.Count);
    }

    [Fact]
    public void ConvertTo_UnknownColumnName_IsIgnored()
    {
        FakeDataReader reader = new(("NonExistent", "ignored"), ("Name", "Widget"));

        SimpleDto result = reader.ConvertTo<SimpleDto>();

        Assert.Equal("Widget", result.Name);
    }

    // ── DateTimeOffset handling (covers both nullable and non-nullable) ────────

    [Fact]
    public void ConvertTo_DateTimeOffsetProperty_FromDateTimeValue()
    {
        // Npgsql can return DateTime for timestamptz columns in legacy mode.
        DateTime dt = new(2024, 6, 15, 12, 0, 0, DateTimeKind.Utc);
        FakeDataReader reader = new(("Timestamp", dt));

        TimestampDto result = reader.ConvertTo<TimestampDto>();

        Assert.Equal(new DateTimeOffset(dt), result.Timestamp);
    }

    [Fact]
    public void ConvertTo_DateTimeOffsetProperty_FromDateTimeOffsetValue()
    {
        DateTimeOffset dto = new(2024, 6, 15, 12, 0, 0, TimeSpan.Zero);
        FakeDataReader reader = new(("Timestamp", dto));

        TimestampDto result = reader.ConvertTo<TimestampDto>();

        Assert.Equal(dto, result.Timestamp);
    }

    [Fact]
    public void ConvertTo_NullableDateTimeOffsetProperty_FromDateTimeValue()
    {
        // Regression: before the fix, DateTimeOffset? fell through to Convert.ChangeType
        // which threw InvalidCastException for DateTime→DateTimeOffset.
        DateTime dt = new(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        FakeDataReader reader = new(("Timestamp", dt));

        NullableTimestampDto result = reader.ConvertTo<NullableTimestampDto>();

        Assert.Equal(new DateTimeOffset(dt), result.Timestamp);
    }

    [Fact]
    public void ConvertTo_NullableDateTimeOffsetProperty_DbNull_RemainsNull()
    {
        FakeDataReader reader = new(("Timestamp", DBNull.Value));

        NullableTimestampDto result = reader.ConvertTo<NullableTimestampDto>();

        Assert.Null(result.Timestamp);
    }

    // ── Enum handling ──────────────────────────────────────────────────────────

    [Fact]
    public void ConvertTo_EnumProperty_MappedFromIntValue()
    {
        FakeDataReader reader = new(("Status", 1));

        EnumDto result = reader.ConvertTo<EnumDto>();

        Assert.Equal(TestStatus.Active, result.Status);
    }

    [Fact]
    public void ConvertTo_NullableEnumProperty_MappedFromIntValue()
    {
        FakeDataReader reader = new(("Status", 2));

        NullableEnumDto result = reader.ConvertTo<NullableEnumDto>();

        Assert.Equal(TestStatus.Inactive, result.Status);
    }

    // ── Caching: setters are reused across multiple instances ─────────────────

    [Fact]
    public void ConvertTo_CalledTwice_ProducesSameResult()
    {
        FakeDataReader reader1 = new(("Name", "A"), ("Count", 1));
        FakeDataReader reader2 = new(("Name", "B"), ("Count", 2));

        SimpleDto r1 = reader1.ConvertTo<SimpleDto>();
        SimpleDto r2 = reader2.ConvertTo<SimpleDto>();

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
