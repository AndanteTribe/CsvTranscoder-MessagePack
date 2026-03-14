using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using MessagePack;

namespace CsvTranscoder.MessagePack.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════════════

file static class FormatterTestHelper
{
    public static CsvReader CreateReader(string csv, CsvTranscodeOptions options)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return new CsvReader(new ReadOnlySequence<byte>(bytes), options);
    }

    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ','
    };

    public static T Transcode<T>(string csvField, ICsvFormatter<T> formatter, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? SimpleOptions;
        var reader = CreateReader(csvField + "\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Transcode(ref writer, ref reader);
        writer.Flush();
        return MessagePackSerializer.Deserialize<T>(buffer.WrittenMemory);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Primitive formatters
// ═══════════════════════════════════════════════════════════════════════

public class BooleanFormatterTests
{
    [Theory]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("True", true)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void BooleanFormatter_Roundtrip(string input, bool expected)
    {
        var result = FormatterTestHelper.Transcode(input, BooleanFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class ByteFormatterTests
{
    [Theory]
    [InlineData("0", (byte)0)]
    [InlineData("255", (byte)255)]
    [InlineData("128", (byte)128)]
    public void ByteFormatter_Roundtrip(string input, byte expected)
    {
        var result = FormatterTestHelper.Transcode(input, ByteFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class SByteFormatterTests
{
    [Theory]
    [InlineData("-128", (sbyte)-128)]
    [InlineData("0", (sbyte)0)]
    [InlineData("127", (sbyte)127)]
    public void SByteFormatter_Roundtrip(string input, sbyte expected)
    {
        var result = FormatterTestHelper.Transcode(input, SByteFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class Int16FormatterTests
{
    [Theory]
    [InlineData("-32768", (short)-32768)]
    [InlineData("0", (short)0)]
    [InlineData("32767", (short)32767)]
    public void Int16Formatter_Roundtrip(string input, short expected)
    {
        var result = FormatterTestHelper.Transcode(input, Int16Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class UInt16FormatterTests
{
    [Theory]
    [InlineData("0", (ushort)0)]
    [InlineData("65535", (ushort)65535)]
    public void UInt16Formatter_Roundtrip(string input, ushort expected)
    {
        var result = FormatterTestHelper.Transcode(input, UInt16Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class Int32FormatterTests
{
    [Theory]
    [InlineData("0", 0)]
    [InlineData("-2147483648", int.MinValue)]
    [InlineData("2147483647", int.MaxValue)]
    [InlineData("42", 42)]
    public void Int32Formatter_Roundtrip(string input, int expected)
    {
        var result = FormatterTestHelper.Transcode(input, Int32Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class UInt32FormatterTests
{
    [Theory]
    [InlineData("0", 0u)]
    [InlineData("4294967295", uint.MaxValue)]
    public void UInt32Formatter_Roundtrip(string input, uint expected)
    {
        var result = FormatterTestHelper.Transcode(input, UInt32Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class Int64FormatterTests
{
    [Theory]
    [InlineData("0", 0L)]
    [InlineData("-9223372036854775808", long.MinValue)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void Int64Formatter_Roundtrip(string input, long expected)
    {
        var result = FormatterTestHelper.Transcode(input, Int64Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class UInt64FormatterTests
{
    [Theory]
    [InlineData("0", 0UL)]
    [InlineData("18446744073709551615", ulong.MaxValue)]
    public void UInt64Formatter_Roundtrip(string input, ulong expected)
    {
        var result = FormatterTestHelper.Transcode(input, UInt64Formatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class SingleFormatterTests
{
    [Theory]
    [InlineData("0", 0f)]
    [InlineData("3.14", 3.14f)]
    [InlineData("-1.5", -1.5f)]
    public void SingleFormatter_Roundtrip(string input, float expected)
    {
        var result = FormatterTestHelper.Transcode(input, SingleFormatter.Instance);
        Assert.Equal(expected, result, 5);
    }
}

public class DoubleFormatterTests
{
    [Theory]
    [InlineData("0", 0.0)]
    [InlineData("3.14159265358979", 3.14159265358979)]
    [InlineData("-99.9", -99.9)]
    public void DoubleFormatter_Roundtrip(string input, double expected)
    {
        var result = FormatterTestHelper.Transcode(input, DoubleFormatter.Instance);
        Assert.Equal(expected, result, 5);
    }
}

public class DecimalFormatterTests
{
    [Fact]
    public void DecimalFormatter_Roundtrip_Zero()
    {
        var result = FormatterTestHelper.Transcode("0", DecimalFormatter.Instance);
        Assert.Equal(0m, result);
    }

    [Fact]
    public void DecimalFormatter_Roundtrip_Value()
    {
        var result = FormatterTestHelper.Transcode("123.456", DecimalFormatter.Instance);
        Assert.Equal(123.456m, result);
    }
}

public class CharFormatterTests
{
    [Theory]
    [InlineData("A", 'A')]
    [InlineData("z", 'z')]
    [InlineData("0", '0')]
    public void CharFormatter_Roundtrip(string input, char expected)
    {
        var result = FormatterTestHelper.Transcode(input, CharFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

public class StringFormatterTests
{
    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData("日本語", "日本語")]
    public void StringFormatter_Roundtrip(string input, string expected)
    {
        var result = FormatterTestHelper.Transcode(input, StringFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  DateTime formatter
// ═══════════════════════════════════════════════════════════════════════

public class DateTimeFormatterTests
{
    [Fact]
    public void DateTimeFormatter_ISO8601_Roundtrip()
    {
        var expected = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
        var result = FormatterTestHelper.Transcode("2024-06-15T12:30:00.0000000Z", DateTimeFormatter.Instance);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DateTimeFormatter_YearMonthDay_Roundtrip()
    {
        var expected = new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);
        var result = FormatterTestHelper.Transcode("2024/01/01", DateTimeFormatter.Instance);
        Assert.Equal(expected, result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  TimeSpan formatter
// ═══════════════════════════════════════════════════════════════════════

public class TimeSpanFormatterTests
{
    [Fact]
    public void TimeSpanFormatter_Roundtrip_Hours()
    {
        var expected = TimeSpan.FromHours(1.5);
        var result = FormatterTestHelper.Transcode("01:30:00", TimeSpanFormatter.Instance);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void TimeSpanFormatter_Roundtrip_Zero()
    {
        var result = FormatterTestHelper.Transcode("00:00:00", TimeSpanFormatter.Instance);
        Assert.Equal(TimeSpan.Zero, result);
    }

    [Fact]
    public void TimeSpanFormatter_EmptyField_ThrowsFormatException()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { TimeSpanFormatter.Instance.Transcode(ref writer, ref reader); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Guid formatter
// ═══════════════════════════════════════════════════════════════════════

public class GuidFormatterTests
{
    [Fact]
    public void GuidFormatter_Roundtrip()
    {
        var expected = Guid.Parse("12345678-1234-1234-1234-123456789012");
        var result = FormatterTestHelper.Transcode("12345678-1234-1234-1234-123456789012", GuidFormatter.Instance);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void GuidFormatter_EmptyField_ThrowsFormatException()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { GuidFormatter.Instance.Transcode(ref writer, ref reader); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Enum formatter
// ═══════════════════════════════════════════════════════════════════════

file enum SampleStatus { Unknown = 0, Active = 1, Inactive = 2 }

public class EnumFormatterTests
{
    [Theory]
    [InlineData("Active", 1)]
    [InlineData("Inactive", 2)]
    [InlineData("Unknown", 0)]
    public void EnumFormatter_ByName_WritesUnderlyingInt(string input, int expectedInt)
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader(input + "\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        EnumFormatter<SampleStatus>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<int>(buffer.WrittenMemory);
        Assert.Equal(expectedInt, result);
    }

    [Fact]
    public void EnumFormatter_InvalidName_WritesDefault()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("NonExistent\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        EnumFormatter<SampleStatus>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<int>(buffer.WrittenMemory);
        Assert.Equal(0, result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Nullable formatter
// ═══════════════════════════════════════════════════════════════════════

public class NullableFormatterTests
{
    [Fact]
    public void NullableFormatter_EmptyField_WritesNil()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        NullableFormatter<int>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<int?>(buffer.WrittenMemory);
        Assert.Null(result);
    }

    [Fact]
    public void NullableFormatter_NonEmptyField_WritesValue()
    {
        var result = FormatterTestHelper.Transcode<int?>("42", NullableFormatter<int>.Instance);
        Assert.Equal(42, result);
    }

    [Fact]
    public void NullableFormatter_SeparatorBetweenFields_WritesNilForEmptyAndValueForNonEmpty()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        // CSV: ",42" — first field empty, second field 42
        var reader = FormatterTestHelper.CreateReader(",42\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        NullableFormatter<int>.Instance.Transcode(ref writer, ref reader);
        Int32Formatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();

        var msgpackReader = new MessagePackReader(new ReadOnlySequence<byte>(buffer.WrittenMemory));
        Assert.True(msgpackReader.TryReadNil());
        Assert.Equal(42, msgpackReader.ReadInt32());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  StandardResolver
// ═══════════════════════════════════════════════════════════════════════

public class StandardResolverTests
{
    [Fact]
    public void StandardResolver_GetFormatter_ReturnsFormatterForPrimitives()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<bool>());
        Assert.NotNull(resolver.GetFormatter<byte>());
        Assert.NotNull(resolver.GetFormatter<sbyte>());
        Assert.NotNull(resolver.GetFormatter<short>());
        Assert.NotNull(resolver.GetFormatter<ushort>());
        Assert.NotNull(resolver.GetFormatter<int>());
        Assert.NotNull(resolver.GetFormatter<uint>());
        Assert.NotNull(resolver.GetFormatter<long>());
        Assert.NotNull(resolver.GetFormatter<ulong>());
        Assert.NotNull(resolver.GetFormatter<float>());
        Assert.NotNull(resolver.GetFormatter<double>());
        Assert.NotNull(resolver.GetFormatter<decimal>());
        Assert.NotNull(resolver.GetFormatter<char>());
        Assert.NotNull(resolver.GetFormatter<string>());
        Assert.NotNull(resolver.GetFormatter<DateTime>());
        Assert.NotNull(resolver.GetFormatter<DateTimeOffset>());
        Assert.NotNull(resolver.GetFormatter<TimeSpan>());
        Assert.NotNull(resolver.GetFormatter<Guid>());
        Assert.NotNull(resolver.GetFormatter<Uri?>());
        Assert.NotNull(resolver.GetFormatter<Version?>());
    }

    [Fact]
    public void StandardResolver_GetFormatter_ReturnsFormatterForEnum()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<SampleStatus>());
    }

    [Fact]
    public void StandardResolver_GetFormatter_ReturnsFormatterForNullable()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<int?>());
        Assert.NotNull(resolver.GetFormatter<DateTime?>());
    }

    [Fact]
    public void StandardResolver_GetFormatter_ReturnsNullForUnknownType()
    {
        var resolver = StandardResolver.Instance;
        Assert.Null(resolver.GetFormatter<object>());
    }

    [Fact]
    public void CsvTranscodeOptions_DefaultResolver_IsStandardResolver()
    {
        var opts = new CsvTranscodeOptions();
        Assert.Same(StandardResolver.Instance, opts.Resolver);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  IsNextFieldEmpty
// ═══════════════════════════════════════════════════════════════════════

public class IsNextFieldEmptyTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Fact]
    public void IsNextFieldEmpty_EmptyField_ReturnsTrue()
    {
        var reader = FormatterTestHelper.CreateReader(",\n", Opts);
        Assert.True(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_NonEmptyField_ReturnsFalse()
    {
        var reader = FormatterTestHelper.CreateReader("42,\n", Opts);
        Assert.False(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_EndOfData_ReturnsTrue()
    {
        // Reader consumed all data
        var reader = FormatterTestHelper.CreateReader("1\n", Opts);
        reader.ReadInt32();
        Assert.True(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_EmptyLastField_ReturnsTrue()
    {
        // CSV: "1," — after reading "1", the next field is empty
        var reader = FormatterTestHelper.CreateReader("1,\n", Opts);
        reader.ReadInt32(); // reads "1" and advances past separator
        Assert.True(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_CrLfNewline_EmptyFieldAtEndOfRow_ReturnsTrue()
    {
        // With CRLF newline, an empty field before the newline should still return true
        var crlfOpts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\r\n", Separator = ','
        };
        var reader = FormatterTestHelper.CreateReader("1,\r\n", crlfOpts);
        reader.ReadInt32();
        Assert.True(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_CrLfNewline_NonEmptyField_ReturnsFalse()
    {
        var crlfOpts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\r\n", Separator = ','
        };
        var reader = FormatterTestHelper.CreateReader("42\r\n", crlfOpts);
        Assert.False(reader.IsNextFieldEmpty());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  DateTimeOffset formatter
// ═══════════════════════════════════════════════════════════════════════

public class DateTimeOffsetFormatterTests
{
    [Fact]
    public void DateTimeOffsetFormatter_ISO8601WithOffset_Roundtrip()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("2024-06-15T12:30:00+09:00\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        DateTimeOffsetFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<DateTimeOffset>(buffer.WrittenMemory);
        var expected = new DateTimeOffset(2024, 6, 15, 12, 30, 0, TimeSpan.FromHours(9));
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DateTimeOffsetFormatter_UTC_Roundtrip()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("2024-01-01T00:00:00+00:00\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        DateTimeOffsetFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<DateTimeOffset>(buffer.WrittenMemory);
        Assert.Equal(DateTimeOffset.UnixEpoch.AddYears(54), result);
    }

    [Fact]
    public void DateTimeOffsetFormatter_EmptyField_ThrowsFormatException()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { DateTimeOffsetFormatter.Instance.Transcode(ref writer, ref reader); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Uri formatter
// ═══════════════════════════════════════════════════════════════════════

public class UriFormatterTests
{
    [Fact]
    public void UriFormatter_AbsoluteUri_Roundtrip()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("https://example.com/path?q=1\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        UriFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<Uri>(buffer.WrittenMemory);
        Assert.Equal(new Uri("https://example.com/path?q=1"), result);
    }

    [Fact]
    public void UriFormatter_EmptyField_WritesNil()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        UriFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var msgpackReader = new MessagePackReader(new ReadOnlySequence<byte>(buffer.WrittenMemory));
        Assert.True(msgpackReader.TryReadNil());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Version formatter
// ═══════════════════════════════════════════════════════════════════════

public class VersionFormatterTests
{
    [Fact]
    public void VersionFormatter_Version_Roundtrip()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("1.2.3.4\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        VersionFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<Version>(buffer.WrittenMemory);
        Assert.Equal(new Version(1, 2, 3, 4), result);
    }

    [Fact]
    public void VersionFormatter_EmptyField_WritesNil()
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var reader = FormatterTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        VersionFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var msgpackReader = new MessagePackReader(new ReadOnlySequence<byte>(buffer.WrittenMemory));
        Assert.True(msgpackReader.TryReadNil());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ValueTuple formatters
// ═══════════════════════════════════════════════════════════════════════

public class ValueTupleFormatterTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Fact]
    public void ValueTupleFormatter1_Roundtrip()
    {
        var opts = Opts;
        var reader = FormatterTestHelper.CreateReader("42\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<ValueTuple<int>>(buffer.WrittenMemory);
        Assert.Equal(new ValueTuple<int>(42), result);
    }

    [Fact]
    public void ValueTupleFormatter2_Roundtrip()
    {
        var opts = Opts;
        var reader = FormatterTestHelper.CreateReader("42,hello\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, string>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, string)>(buffer.WrittenMemory);
        Assert.Equal((42, "hello"), result);
    }

    [Fact]
    public void ValueTupleFormatter3_Roundtrip()
    {
        var opts = Opts;
        var reader = FormatterTestHelper.CreateReader("1,2,3\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, int, int>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, int, int)>(buffer.WrittenMemory);
        Assert.Equal((1, 2, 3), result);
    }

    [Fact]
    public void ValueTupleFormatter4_Roundtrip()
    {
        var opts = Opts;
        var reader = FormatterTestHelper.CreateReader("1,2,3,4\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, int, int, int>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, int, int, int)>(buffer.WrittenMemory);
        Assert.Equal((1, 2, 3, 4), result);
    }

    [Fact]
    public void ValueTupleFormatter7_MixedTypes_Roundtrip()
    {
        var opts = Opts;
        var reader = FormatterTestHelper.CreateReader("1,hello,true,42,3.14,2024-01-01T00:00:00.0000000Z,a\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, string, bool, long, double, DateTime, char>.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, string, bool, long, double, DateTime, char)>(buffer.WrittenMemory);
        Assert.Equal(1, result.Item1);
        Assert.Equal("hello", result.Item2);
        Assert.True(result.Item3);
        Assert.Equal(42L, result.Item4);
        Assert.Equal(3.14, result.Item5, 5);
        Assert.Equal(new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc), result.Item6);
        Assert.Equal('a', result.Item7);
    }

    [Fact]
    public void StandardResolver_GetFormatter_ReturnsFormatterForValueTuple()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<ValueTuple<int>>());
        Assert.NotNull(resolver.GetFormatter<(int, string)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long, double)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long, double, DateTime)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long, double, DateTime, char)>());
    }

    [Fact]
    public void StandardResolver_GetFormatter_ReturnsFormatterForNewTypes()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<DateTimeOffset>());
        Assert.NotNull(resolver.GetFormatter<Uri?>());
        Assert.NotNull(resolver.GetFormatter<Version?>());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EnumMemberCsvFormatter
// ═══════════════════════════════════════════════════════════════════════

file enum SimpleStatus { Unknown = 0, Active = 1, Inactive = 2 }

[System.Runtime.Serialization.DataContract]
file enum LabeledStatus
{
    [System.Runtime.Serialization.EnumMember(Value = "有効")]
    Active = 1,

    [System.Runtime.Serialization.EnumMember(Value = "無効")]
    Inactive = 2,
}

[Flags]
[System.Runtime.Serialization.DataContract]
file enum Permission
{
    None = 0,
    [System.Runtime.Serialization.EnumMember(Value = "読取")]
    Read = 1 << 0,
    [System.Runtime.Serialization.EnumMember(Value = "書込")]
    Write = 1 << 1,
    [System.Runtime.Serialization.EnumMember(Value = "実行")]
    Execute = 1 << 2,
}

public class EnumMemberCsvFormatterTests
{
    [Theory]
    [InlineData("Active", (int)SimpleStatus.Active)]
    [InlineData("Inactive", (int)SimpleStatus.Inactive)]
    [InlineData("Unknown", (int)SimpleStatus.Unknown)]
    [InlineData("active", (int)SimpleStatus.Active)]
    public void EnumMemberCsvFormatter_NoEnumMemberAttr_FallsBackToTryParse(string input, int expectedInt)
    {
        var result = FormatterTestHelper.Transcode(input, EnumMemberCsvFormatter<SimpleStatus>.Instance);
        Assert.Equal(expectedInt, (int)result);
    }

    [Fact]
    public void EnumMemberCsvFormatter_InvalidName_WritesDefault()
    {
        var result = FormatterTestHelper.Transcode("NonExistent", EnumMemberCsvFormatter<SimpleStatus>.Instance);
        Assert.Equal(SimpleStatus.Unknown, result);
    }

    [Theory]
    [InlineData("有効", (int)LabeledStatus.Active)]
    [InlineData("無効", (int)LabeledStatus.Inactive)]
    public void EnumMemberCsvFormatter_EnumMemberAttrValue_ParsedCorrectly(string input, int expectedInt)
    {
        var result = FormatterTestHelper.Transcode(input, EnumMemberCsvFormatter<LabeledStatus>.Instance);
        Assert.Equal(expectedInt, (int)result);
    }

    [Fact]
    public void EnumMemberCsvFormatter_EnumMemberAttrFallbackToName_Works()
    {
        // Fallback: the CSV uses the C# name "Active" instead of the EnumMember value "有効"
        var result = FormatterTestHelper.Transcode("Active", EnumMemberCsvFormatter<LabeledStatus>.Instance);
        Assert.Equal(LabeledStatus.Active, result);
    }

    [Theory]
    [InlineData("読取", (int)Permission.Read)]
    [InlineData("書込", (int)Permission.Write)]
    [InlineData("実行", (int)Permission.Execute)]
    public void EnumMemberCsvFormatter_FlagsEnum_SingleValue(string input, int expectedInt)
    {
        var result = FormatterTestHelper.Transcode(input, EnumMemberCsvFormatter<Permission>.Instance);
        Assert.Equal(expectedInt, (int)result);
    }

    [Theory]
    [InlineData("読取_書込", (int)(Permission.Read | Permission.Write))]
    [InlineData("読取_実行", (int)(Permission.Read | Permission.Execute))]
    [InlineData("読取_書込_実行", (int)(Permission.Read | Permission.Write | Permission.Execute))]
    public void EnumMemberCsvFormatter_FlagsEnum_CombinedValues_ParsedCorrectly(string input, int expectedInt)
    {
        var result = FormatterTestHelper.Transcode(input, EnumMemberCsvFormatter<Permission>.Instance);
        Assert.Equal(expectedInt, (int)result);
    }

    [Fact]
    public void EnumMemberCsvFormatter_Empty_WritesDefault()
    {
        var result = FormatterTestHelper.Transcode("", EnumMemberCsvFormatter<Permission>.Instance);
        Assert.Equal(Permission.None, result);
    }

    [Fact]
    public void StandardResolver_Enum_UsesEnumMemberCsvFormatter()
    {
        var formatter = StandardResolver.Instance.GetFormatter<LabeledStatus>();
        Assert.NotNull(formatter);
        Assert.IsType<EnumMemberCsvFormatter<LabeledStatus>>(formatter);
    }
}
