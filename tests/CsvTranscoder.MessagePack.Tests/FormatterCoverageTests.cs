using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using MessagePack;

namespace CsvTranscoder.MessagePack.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  Helpers (file-scoped to avoid conflict with FormatterTests.cs)
// ═══════════════════════════════════════════════════════════════════════

file static class CoverageTestHelper
{
    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
    };

    public static CsvReader CreateReader(string csv, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csv);
        return new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
    }

    public static T Transcode<T>(string csvField, ICsvFormatter<T> formatter, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? SimpleOptions;
        var reader = CreateReader(csvField + "\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Transcode(ref writer, ref reader, opts);
        writer.Flush();
        return MessagePackSerializer.Deserialize<T>(buffer.WrittenMemory);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  DateTimeFormatter — additional coverage
//  Note: CsvReader and MessagePackWriter are ref structs; try/catch is used
//  where Assert.Throws cannot capture ref struct operands in a lambda.
// ═══════════════════════════════════════════════════════════════════════

public class DateTimeFormatterCoverageTests
{
    [Fact]
    public void DateTimeFormatter_Rfc1123Format_Roundtrip()
    {
        // RFC 1123 ('R') format: "Sat, 15 Jun 2024 12:30:00 GMT"
        // Use a semicolon separator to avoid the comma in the date string being treated as
        // a field separator.  Utf8Parser 'O' fails; Utf8Parser 'R' succeeds (lines 25-28).
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ';',
        };
        var expected = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
        var reader = CoverageTestHelper.CreateReader("Sat, 15 Jun 2024 12:30:00 GMT\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        DateTimeFormatter.Instance.Transcode(ref writer, ref reader, opts);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<DateTime>(buffer.WrittenMemory);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void DateTimeFormatter_InvalidFormat_ThrowsFormatException()
    {
        // An unparseable string reaches line 61 (final throw).
        var opts = CoverageTestHelper.SimpleOptions;
        var reader = CoverageTestHelper.CreateReader("not-a-date\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { DateTimeFormatter.Instance.Transcode(ref writer, ref reader, opts); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FieldSpanOwner empty-field path via DateTimeFormatter
// ═══════════════════════════════════════════════════════════════════════

public class FieldSpanOwnerEmptyPathTests
{
    [Fact]
    public void DateTimeFormatter_EmptyField_ThrowsFormatException()
    {
        // An empty field causes DateTimeFormatter to create FieldSpanOwner with an empty
        // ReadOnlySequence — hitting the field.IsEmpty path in FieldSpanOwner (lines 599-600)
        // and eventually the final throw in DateTimeFormatter (line 61).
        var opts = CoverageTestHelper.SimpleOptions;
        var reader = CoverageTestHelper.CreateReader("\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { DateTimeFormatter.Instance.Transcode(ref writer, ref reader, opts); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GuidFormatter — invalid GUID throw
// ═══════════════════════════════════════════════════════════════════════

public class GuidFormatterInvalidTests
{
    [Fact]
    public void GuidFormatter_InvalidGuid_ThrowsFormatException()
    {
        // A non-GUID string causes GuidFormatter to throw (line 20).
        var opts = CoverageTestHelper.SimpleOptions;
        var reader = CoverageTestHelper.CreateReader("not-a-guid\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        try { GuidFormatter.Instance.Transcode(ref writer, ref reader, opts); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  EnumFormatter — all underlying integer types
// ═══════════════════════════════════════════════════════════════════════

file enum ByteBackedEnum : byte { Zero = 0, One = 1, Max = 255 }
file enum SByteBackedEnum : sbyte { Min = -128, Zero = 0, Max = 127 }
file enum Int16BackedEnum : short { Min = short.MinValue, Zero = 0, Max = short.MaxValue }
file enum UInt16BackedEnum : ushort { Zero = 0, Max = ushort.MaxValue }
file enum UInt32BackedEnum : uint { Zero = 0, Big = 100000 }
file enum Int64BackedEnum : long { Min = long.MinValue, Zero = 0, Max = long.MaxValue }
file enum UInt64BackedEnum : ulong { Zero = 0, Big = 1000000000 }

public class EnumFormatterUnderlyingTypeTests
{
    private static void TranscodeEnum<TEnum, TResult>(string input, out TResult result)
        where TEnum : struct, Enum
    {
        var opts = CoverageTestHelper.SimpleOptions;
        var reader = CoverageTestHelper.CreateReader(input + "\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        EnumFormatter<TEnum>.Instance.Transcode(ref writer, ref reader, opts);
        writer.Flush();
        result = MessagePackSerializer.Deserialize<TResult>(buffer.WrittenMemory);
    }

    [Fact]
    public void EnumFormatter_ByteUnderlying_WritesCorrectValue()
    {
        TranscodeEnum<ByteBackedEnum, byte>("One", out var result);
        Assert.Equal((byte)ByteBackedEnum.One, result);
    }

    [Fact]
    public void EnumFormatter_SByteUnderlying_WritesCorrectValue()
    {
        TranscodeEnum<SByteBackedEnum, sbyte>("Max", out var result);
        Assert.Equal((sbyte)SByteBackedEnum.Max, result);
    }

    [Fact]
    public void EnumFormatter_Int16Underlying_WritesCorrectValue()
    {
        TranscodeEnum<Int16BackedEnum, short>("Max", out var result);
        Assert.Equal((short)Int16BackedEnum.Max, result);
    }

    [Fact]
    public void EnumFormatter_UInt16Underlying_WritesCorrectValue()
    {
        TranscodeEnum<UInt16BackedEnum, ushort>("Max", out var result);
        Assert.Equal((ushort)UInt16BackedEnum.Max, result);
    }

    [Fact]
    public void EnumFormatter_UInt32Underlying_WritesCorrectValue()
    {
        TranscodeEnum<UInt32BackedEnum, uint>("Big", out var result);
        Assert.Equal((uint)UInt32BackedEnum.Big, result);
    }

    [Fact]
    public void EnumFormatter_Int64Underlying_WritesCorrectValue()
    {
        TranscodeEnum<Int64BackedEnum, long>("Zero", out var result);
        Assert.Equal((long)Int64BackedEnum.Zero, result);
    }

    [Fact]
    public void EnumFormatter_UInt64Underlying_WritesCorrectValue()
    {
        TranscodeEnum<UInt64BackedEnum, ulong>("Big", out var result);
        Assert.Equal((ulong)UInt64BackedEnum.Big, result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ValueTupleFormatter<T1..T5> and <T1..T6>
// ═══════════════════════════════════════════════════════════════════════

public class ValueTupleFormatter5And6Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ',',
    };

    [Fact]
    public void ValueTupleFormatter5_Roundtrip()
    {
        var opts = Opts;
        var reader = CoverageTestHelper.CreateReader("1,2,3,4,5\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, int, int, int, int>.Instance.Transcode(ref writer, ref reader, opts);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, int, int, int, int)>(buffer.WrittenMemory);
        Assert.Equal((1, 2, 3, 4, 5), result);
    }

    [Fact]
    public void ValueTupleFormatter6_Roundtrip()
    {
        var opts = Opts;
        var reader = CoverageTestHelper.CreateReader("1,2,3,4,5,6\n", opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        ValueTupleFormatter<int, int, int, int, int, int>.Instance.Transcode(ref writer, ref reader, opts);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<(int, int, int, int, int, int)>(buffer.WrittenMemory);
        Assert.Equal((1, 2, 3, 4, 5, 6), result);
    }

    [Fact]
    public void StandardResolver_ReturnsFormatter_For5And6Tuples()
    {
        var resolver = StandardResolver.Instance;
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long, double)>());
        Assert.NotNull(resolver.GetFormatter<(int, string, bool, long, double, DateTime)>());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FormatterResolverExtensions — GetFormatterWithVerify throw path
// ═══════════════════════════════════════════════════════════════════════

public class FormatterResolverExtensionsTests
{
    [Fact]
    public void GetFormatterWithVerify_NullFormatter_ThrowsInvalidOperationException()
    {
        // StandardResolver returns null for unknown types; GetFormatterWithVerify must throw.
        Assert.Throws<InvalidOperationException>(() => StandardResolver.Instance.GetFormatterWithVerify<object>());
    }
}
