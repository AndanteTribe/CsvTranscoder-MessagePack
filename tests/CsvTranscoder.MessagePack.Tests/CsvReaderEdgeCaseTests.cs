using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using MessagePack;

namespace CsvTranscoder.MessagePack.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  Multi-segment sequence helper
// ═══════════════════════════════════════════════════════════════════════

file static class EdgeCaseHelpers
{
    private sealed class MemorySegment<T> : ReadOnlySequenceSegment<T>
    {
        public MemorySegment(ReadOnlyMemory<T> memory) => Memory = memory;

        public MemorySegment<T> Append(ReadOnlyMemory<T> memory)
        {
            var segment = new MemorySegment<T>(memory) { RunningIndex = RunningIndex + Memory.Length };
            Next = segment;
            return segment;
        }
    }

    /// <summary>Creates a two-segment <see cref="ReadOnlySequence{T}"/> from two byte arrays.</summary>
    public static ReadOnlySequence<byte> CreateMultiSegment(byte[] first, byte[] second)
    {
        var firstSeg = new MemorySegment<byte>(first);
        var lastSeg = firstSeg.Append(second);
        return new ReadOnlySequence<byte>(firstSeg, 0, lastSeg, lastSeg.Memory.Length);
    }

    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
    };

    public static CsvTranscodeOptions CrLfOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\r\n",
        Separator = ',',
    };
}

// ═══════════════════════════════════════════════════════════════════════
//  CsvReader constructor validation
//  Note: CsvReader is a ref struct; try/catch is used where Assert.Throws
//  cannot capture ref struct operands in a lambda.
// ═══════════════════════════════════════════════════════════════════════

public class CsvReaderConstructorTests
{
    [Fact]
    public void Constructor_EmptyNewLine_ThrowsArgumentException()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "", Separator = ','
        };
        var bytes = Encoding.UTF8.GetBytes("data\n");
        var seq = new ReadOnlySequence<byte>(bytes);

        try { _ = new CsvReader(seq, opts); Assert.Fail("Expected ArgumentException"); }
        catch (ArgumentException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ReadXxx invalid-value paths (ThrowFormatException)
//  Note: CsvReader is a ref struct and cannot be captured in a lambda,
//  so we use try/catch instead of Assert.Throws throughout this section.
// ═══════════════════════════════════════════════════════════════════════

public class ReadSByteInvalidTests
{
    [Fact]
    public void ReadSByte_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadSByte(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadInt16InvalidTests
{
    [Fact]
    public void ReadInt16_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadInt16(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadUInt16InvalidTests
{
    [Fact]
    public void ReadUInt16_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadUInt16(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadInt32InvalidTests
{
    [Fact]
    public void ReadInt32_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadInt32(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadUInt32InvalidTests
{
    [Fact]
    public void ReadUInt32_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadUInt32(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadInt64InvalidTests
{
    [Fact]
    public void ReadInt64_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadInt64(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadUInt64InvalidTests
{
    [Fact]
    public void ReadUInt64_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadUInt64(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadDoubleInvalidTests
{
    [Fact]
    public void ReadDouble_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadDouble(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadDecimalInvalidTests
{
    [Fact]
    public void ReadDecimal_InvalidValue_ThrowsFormatException()
    {
        var bytes = Encoding.UTF8.GetBytes("abc\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        try { reader.ReadDecimal(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  IsNextFieldEmpty — quoted empty field coverage
// ═══════════════════════════════════════════════════════════════════════

public class IsNextFieldEmptyQuotedTests
{
    private static CsvTranscodeOptions QuotedOpts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ',', Quote = Quote.Minimal,
    };

    [Fact]
    public void IsNextFieldEmpty_QuotedEmptyField_ReturnsTrue()
    {
        // "","" — first field is a quoted empty: ""
        var bytes = Encoding.UTF8.GetBytes("\"\",other\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), QuotedOpts);
        Assert.True(reader.IsNextFieldEmpty());
    }

    [Fact]
    public void IsNextFieldEmpty_QuotedNonEmptyField_ReturnsFalse()
    {
        // "x" — quoted non-empty
        var bytes = Encoding.UTF8.GetBytes("\"x\",other\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), QuotedOpts);
        Assert.False(reader.IsNextFieldEmpty());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ReadString — multi-segment ReadOnlySequence path
// ═══════════════════════════════════════════════════════════════════════

public class ReadStringMultiSegmentTests
{
    [Fact]
    public void ReadString_MultiSegmentField_ReturnsCorrectString()
    {
        // Split "hello\n" across two segments: "hel" + "lo\n"
        var seq = EdgeCaseHelpers.CreateMultiSegment(
            Encoding.UTF8.GetBytes("hel"),
            Encoding.UTF8.GetBytes("lo\n"));
        var reader = new CsvReader(seq, EdgeCaseHelpers.SimpleOptions);
        Assert.Equal("hello", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  SkipToEndOfRow edge cases
// ═══════════════════════════════════════════════════════════════════════

public class SkipToEndOfRowEdgeCaseTests
{
    [Fact]
    public void SkipRow_SingleByteNewline_NoNewlineRemaining_ReachesEnd()
    {
        // After consuming all data including the last newline, calling TryAdvanceToNextRow
        // again triggers the AdvanceToEnd() path in SkipToEndOfRow (line 123).
        var bytes = Encoding.UTF8.GetBytes("data\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        reader.ReadString();
        Assert.False(reader.TryAdvanceToNextRow()); // consumes '\n', now at end
        Assert.False(reader.TryAdvanceToNextRow()); // AdvanceToEnd() path
    }

    [Fact]
    public void SkipRow_CrLf_NoCarriageReturnRemaining_ReachesEnd()
    {
        // After consuming all data including the last CRLF, calling TryAdvanceToNextRow
        // triggers the AdvanceToEnd() path in SkipToEndOfRow for the multi-byte case (lines 134-135).
        var bytes = Encoding.UTF8.GetBytes("data\r\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.CrLfOptions);
        reader.ReadString();
        Assert.False(reader.TryAdvanceToNextRow()); // consumes "\r\n", now at end
        Assert.False(reader.TryAdvanceToNextRow()); // TryAdvanceTo('\r') fails → AdvanceToEnd
    }

    [Fact]
    public void SkipRow_CrLf_BareCarriageReturn_Skips()
    {
        // "a\rb\r\n": the bare '\r' (not followed by '\n') must be skipped (line 144).
        // After SkipRow the reader should be at end.
        var bytes = Encoding.UTF8.GetBytes("a\rb\r\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.CrLfOptions);
        reader.SkipRow();
        Assert.True(reader.End);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ReadFieldRaw CRLF edge cases
// ═══════════════════════════════════════════════════════════════════════

public class ReadFieldRawCrLfEdgeCaseTests
{
    [Fact]
    public void ReadFieldRaw_CrLf_NoTerminatorFound_ReadsToEnd()
    {
        // "hello" with CRLF mode and no '\r' or ',' — TryReadToAny fails and
        // the reader falls back to AdvanceToEnd (lines 258-260).
        var bytes = Encoding.UTF8.GetBytes("hello");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.CrLfOptions);
        Assert.Equal("hello", reader.ReadString());
    }

    [Fact]
    public void ReadFieldRaw_CrLf_BareCarriageReturn_IncludedInField()
    {
        // "a\rb\r\n": the bare '\r' at position 1 (not followed by '\n') is included
        // in the field value (line 281). The field should be "a\rb".
        var bytes = Encoding.UTF8.GetBytes("a\rb\r\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.CrLfOptions);
        Assert.Equal("a\rb", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  BuildCommentColumnMask — natural loop exit when reader reaches end
// ═══════════════════════════════════════════════════════════════════════

public class BuildCommentColumnMaskLoopExitTests
{
    [Fact]
    public void BuildCommentColumnMask_HeaderWithNoTrailingNewline_ExitsNaturally()
    {
        // When the header row has no trailing newline, BuildCommentColumnMask exits
        // via the while-condition becoming false (line 95 / natural loop termination).
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ',',
        };
        // Header "id,#skip,name" with no trailing newline
        var bytes = Encoding.UTF8.GetBytes("id,#skip,name");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        reader.SkipHeader(); // BuildCommentColumnMask reads header fields then _reader.End == true
        Assert.True(reader.End);
    }

    [Fact]
    public void BuildCommentColumnMask_MoreThan64Columns_SkipsExtraCommentColumns()
    {
        // The first MaxCommentColumns (64) comment columns set mask bits;
        // columns beyond index 63 are never masked (line 88 condition, line 95 path).
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ',',
        };

        // Build a header with 66 columns: first two are "#c0","#c1", then "val", then 63 more "#cn" columns
        var headerCols = new string[66];
        for (int i = 0; i < 65; i++) headerCols[i] = $"#c{i}";
        headerCols[65] = "val";
        var header = string.Join(",", headerCols);

        // Data row: 66 fields, last one is "42"
        var dataCols = new string[66];
        for (int i = 0; i < 65; i++) dataCols[i] = $"x{i}";
        dataCols[65] = "42";
        var dataRow = string.Join(",", dataCols);

        var csv = header + "\n" + dataRow + "\n";
        var bytes = Encoding.UTF8.GetBytes(csv);
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        reader.SkipHeader();

        // Column 65 (index 65) is beyond MaxCommentColumns (64), so its "#" header is ignored
        // and it is treated as a data column.
        // Columns 0-63 are comment columns (masked). Column 64 is also a comment column but > 63 index
        // so it is NOT masked, meaning it shows up as a data field.
        // Actually column 64 (#c64) has index 64 which equals MaxCommentColumns (64), so it is NOT masked.
        // The first non-masked column encountered while reading will be column 64 ("x64").
        Assert.Equal("x64", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  FieldSpanOwner — empty field and multi-segment paths via formatters
// ═══════════════════════════════════════════════════════════════════════

public class FieldSpanOwnerViaMultiSegmentTests
{
    [Fact]
    public void MultiSegment_BooleanFormatter_ReadsCorrectly()
    {
        // Multi-segment field: "tru" + "e\n". FieldSpanOwner will use the stack buffer
        // to flatten the multi-segment field (field.IsSingleSegment == false, length <= stackalloc).
        var seq = EdgeCaseHelpers.CreateMultiSegment(
            Encoding.UTF8.GetBytes("tru"),
            Encoding.UTF8.GetBytes("e\n"));
        var opts = EdgeCaseHelpers.SimpleOptions;
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        var reader = new CsvReader(seq, opts);
        BooleanFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();
        Assert.True(MessagePackSerializer.Deserialize<bool>(buffer.WrittenMemory));
    }

    [Fact]
    public void MultiSegment_LargeField_UsesPooledArray()
    {
        // Multi-segment field of 11 bytes content ("not-a-bo" = 8 bytes, "ol!" = 3 bytes, then "\n").
        // 11 bytes exceeds ReadBoolean's 8-byte stack buffer, so FieldSpanOwner rents from
        // ArrayPool (lines 618-621). On Dispose, the rented array is returned (lines 627-628).
        // The value is not a valid boolean, so FormatException is expected.
        // Note: CsvReader and MessagePackWriter are ref structs and cannot be captured in
        // a lambda, so try/catch is used instead of Assert.Throws.
        var seq = EdgeCaseHelpers.CreateMultiSegment(
            Encoding.UTF8.GetBytes("not-a-bo"),
            Encoding.UTF8.GetBytes("ol!\n"));
        var opts = EdgeCaseHelpers.SimpleOptions;
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        var reader = new CsvReader(seq, opts);
        try { BooleanFormatter.Instance.Transcode(ref writer, ref reader); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  SkipToEndOfRow — CRLF with no \r in content (lines 132-135)
// ═══════════════════════════════════════════════════════════════════════

public class SkipToEndOfRowCrLfNoCarriageReturnTests
{
    [Fact]
    public void SkipRow_CrLf_NoCarriageReturnFound_AdvancesToEnd()
    {
        // Data "abc" with CRLF newline and no '\r' anywhere:
        // TryAdvanceTo('\r') returns false → AdvanceToEnd() + return (lines 134-135).
        var bytes = Encoding.UTF8.GetBytes("abc");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.CrLfOptions);
        reader.SkipRow();
        Assert.True(reader.End);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  TryReadField — returning false after all comment columns consumed
//  (lines 182-185: _reader.End becomes true inside the comment-skip loop)
// ═══════════════════════════════════════════════════════════════════════

public class TryReadFieldAllCommentColumnsTests
{
    [Fact]
    public void ReadString_AllColumnsAreComments_ReturnsEmpty()
    {
        // Header "#a" is a comment column. Data row "x" (no trailing newline) is the only
        // value; it fills the single comment column. After consuming it, the loop in
        // TryReadField checks _reader.End which is now true → field = default, return false.
        // ReadString then returns string.Empty.
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ',',
        };
        var bytes = Encoding.UTF8.GetBytes("#a\nx");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        reader.SkipHeader();
        Assert.Equal(string.Empty, reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  IsNextFieldEmpty — reader at end returns true (line 522)
// ═══════════════════════════════════════════════════════════════════════

public class IsNextFieldEmptyAtEndTests
{
    [Fact]
    public void IsNextFieldEmpty_ReaderAtEnd_ReturnsTrue()
    {
        // An empty sequence means the reader is already at end; IsNextFieldEmpty must return true.
        var bytes = Encoding.UTF8.GetBytes("");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), EdgeCaseHelpers.SimpleOptions);
        Assert.True(reader.IsNextFieldEmpty());
    }
}
