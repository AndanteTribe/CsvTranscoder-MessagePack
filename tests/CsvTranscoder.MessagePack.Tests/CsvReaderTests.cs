using System.Buffers;
using System.Text;
using AndanteTribe.Csv;

namespace CsvTranscoder.MessagePack.Tests;

/// <summary>
/// Helper to create a <see cref="CsvReader"/> from a plain UTF-8 string.
/// </summary>
file static class CsvReaderFactory
{
    public static CsvReader Create(string csv, CsvTranscodeOptions options)
    {
        var bytes = Encoding.UTF8.GetBytes(csv);
        return new CsvReader(new ReadOnlySequence<byte>(bytes), options);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Primitive read methods
// ═══════════════════════════════════════════════════════════════════════

public class ReadBooleanTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("True", true)]
    [InlineData("False", false)]
    [InlineData("true", true)]
    [InlineData("false", false)]
    [InlineData("1", true)]
    [InlineData("0", false)]
    public void ReadBoolean_ValidValues(string input, bool expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadBoolean());
    }

    [Fact]
    public void ReadBoolean_InvalidValue_ThrowsFormatException()
    {
        var reader = CsvReaderFactory.Create("yes\n", Opts);
        // CsvReader is a ref struct and cannot be captured in a lambda,
        // so we use try/catch instead of Assert.Throws.
        try { reader.ReadBoolean(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadByteTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", 0)]
    [InlineData("255", 255)]
    [InlineData("128", 128)]
    public void ReadByte_ValidValues(string input, byte expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadByte());
    }

    [Fact]
    public void ReadByte_InvalidValue_ThrowsFormatException()
    {
        var reader = CsvReaderFactory.Create("abc\n", Opts);
        try { reader.ReadByte(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadSByteTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("-128", -128)]
    [InlineData("0", 0)]
    [InlineData("127", 127)]
    public void ReadSByte_ValidValues(string input, sbyte expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadSByte());
    }
}

public class ReadInt16Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("-32768", short.MinValue)]
    [InlineData("0", 0)]
    [InlineData("32767", short.MaxValue)]
    public void ReadInt16_ValidValues(string input, short expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadInt16());
    }
}

public class ReadUInt16Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", (ushort)0)]
    [InlineData("65535", ushort.MaxValue)]
    public void ReadUInt16_ValidValues(string input, ushort expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadUInt16());
    }
}

public class ReadInt32Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("-2147483648", int.MinValue)]
    [InlineData("0", 0)]
    [InlineData("2147483647", int.MaxValue)]
    public void ReadInt32_ValidValues(string input, int expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadInt32());
    }

    [Fact]
    public void ReadInt32_MultipleFieldsOnRow()
    {
        var reader = CsvReaderFactory.Create("1,2,3\n", Opts);
        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal(3, reader.ReadInt32());
    }

    [Fact]
    public void ReadInt32_MultipleRows()
    {
        var reader = CsvReaderFactory.Create("10\n20\n", Opts);
        Assert.Equal(10, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(20, reader.ReadInt32());
    }
}

public class ReadUInt32Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("4294967295", uint.MaxValue)]
    public void ReadUInt32_ValidValues(string input, uint expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadUInt32());
    }
}

public class ReadInt64Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("-9223372036854775808", long.MinValue)]
    [InlineData("0", 0L)]
    [InlineData("9223372036854775807", long.MaxValue)]
    public void ReadInt64_ValidValues(string input, long expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadInt64());
    }
}

public class ReadUInt64Tests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", 0UL)]
    [InlineData("18446744073709551615", ulong.MaxValue)]
    public void ReadUInt64_ValidValues(string input, ulong expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadUInt64());
    }
}

public class ReadSingleTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", 0f)]
    [InlineData("3.14", 3.14f)]
    [InlineData("-1.5", -1.5f)]
    public void ReadSingle_ValidValues(string input, float expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadSingle(), 3);
    }

    [Fact]
    public void ReadSingle_InvalidValue_ThrowsFormatException()
    {
        var reader = CsvReaderFactory.Create("abc\n", Opts);
        try { reader.ReadSingle(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }
}

public class ReadDoubleTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", 0d)]
    [InlineData("3.14159", 3.14159d)]
    [InlineData("-2.71828", -2.71828d)]
    public void ReadDouble_ValidValues(string input, double expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadDouble(), 5);
    }
}

public class ReadDecimalTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("0", "0")]
    [InlineData("123.456", "123.456")]
    [InlineData("-99.99", "-99.99")]
    public void ReadDecimal_ValidValues(string input, string expectedStr)
    {
        var expected = decimal.Parse(expectedStr);
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadDecimal());
    }
}

public class ReadDateTimeTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Fact]
    public void ReadDateTime_Iso8601_RoundTrip()
    {
        var expected = new DateTime(2024, 6, 15, 12, 30, 0, DateTimeKind.Utc);
        var iso = expected.ToString("O");
        var reader = CsvReaderFactory.Create(iso + "\n", Opts);
        Assert.Equal(expected, reader.ReadDateTime().ToUniversalTime());
    }

    [Fact]
    public void ReadDateTime_DateOnly_InvariantFormat()
    {
        var reader = CsvReaderFactory.Create("2024-06-15\n", Opts);
        var result = reader.ReadDateTime();
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
    }

    [Fact]
    public void ReadDateTime_DateTimeWithTime_InvariantFormat()
    {
        var reader = CsvReaderFactory.Create("2024-06-15 12:30:00\n", Opts);
        var result = reader.ReadDateTime();
        Assert.Equal(new DateTime(2024, 6, 15, 12, 30, 0), result);
    }

    [Fact]
    public void ReadDateTime_SlashSeparated_InvariantFormat()
    {
        var reader = CsvReaderFactory.Create("2024/06/15\n", Opts);
        var result = reader.ReadDateTime();
        Assert.Equal(new DateTime(2024, 6, 15), result.Date);
    }

    [Fact]
    public void ReadDateTime_InvalidValue_ThrowsFormatException()
    {
        var reader = CsvReaderFactory.Create("not-a-date\n", Opts);
        // CsvReader is a ref struct and cannot be captured in a lambda,
        // so we use try/catch instead of Assert.Throws.
        try { reader.ReadDateTime(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }

    [Fact]
    public void ReadDateTime_MultipleFieldsOnRow()
    {
        var reader = CsvReaderFactory.Create("2024-01-01,2025-12-31\n", Opts);
        Assert.Equal(new DateTime(2024, 1, 1), reader.ReadDateTime().Date);
        Assert.Equal(new DateTime(2025, 12, 31), reader.ReadDateTime().Date);
    }
}

public class ReadCharTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("A", 'A')]
    [InlineData("z", 'z')]
    [InlineData("0", '0')]
    public void ReadChar_ValidValues(string input, char expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadChar());
    }

    [Fact]
    public void ReadChar_EmptyField_ThrowsFormatException()
    {
        var reader = CsvReaderFactory.Create(",\n", Opts);
        try { reader.ReadChar(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }

    [Fact]
    public void ReadChar_MultiCharField_ReturnsFirstChar()
    {
        var reader = CsvReaderFactory.Create("Hello\n", Opts);
        Assert.Equal('H', reader.ReadChar());
    }
}

public class ReadStringTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Theory]
    [InlineData("hello", "hello")]
    [InlineData("", "")]
    [InlineData("こんにちは", "こんにちは")]
    public void ReadString_ValidValues(string input, string expected)
    {
        var reader = CsvReaderFactory.Create(input + "\n", Opts);
        Assert.Equal(expected, reader.ReadString());
    }

    [Fact]
    public void ReadString_MultipleFields()
    {
        var reader = CsvReaderFactory.Create("foo,bar,baz\n", Opts);
        Assert.Equal("foo", reader.ReadString());
        Assert.Equal("bar", reader.ReadString());
        Assert.Equal("baz", reader.ReadString());
    }

    [Fact]
    public void ReadString_EmptyField_ReturnsEmptyString()
    {
        var reader = CsvReaderFactory.Create(",second\n", Opts);
        Assert.Equal("", reader.ReadString());
        Assert.Equal("second", reader.ReadString());
    }

    [Fact]
    public void ReadString_LastFieldNoTrailingNewline()
    {
        var reader = CsvReaderFactory.Create("only", Opts);
        Assert.Equal("only", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Navigation helpers: SkipHeader, SkipRow, SkipField
// ═══════════════════════════════════════════════════════════════════════

public class NavigationTests
{
    [Fact]
    public void SkipHeader_HasHeader_True_SkipsFirstRow()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("id,name\n1,Alice\n", opts);
        reader.SkipHeader();
        Assert.Equal("1", reader.ReadString());
        Assert.Equal("Alice", reader.ReadString());
    }

    [Fact]
    public void SkipHeader_HasHeader_False_DoesNotSkip()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("1,Alice\n", opts);
        reader.SkipHeader();
        Assert.Equal("1", reader.ReadString());
    }

    [Fact]
    public void SkipRow_AdvancesPastCurrentRow()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("skip,this\nkeep,this\n", opts);
        reader.SkipRow();
        Assert.Equal("keep", reader.ReadString());
        Assert.Equal("this", reader.ReadString());
    }

    [Fact]
    public void SkipField_AdvancesPastCurrentField()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("skip,keep\n", opts);
        reader.SkipField();
        Assert.Equal("keep", reader.ReadString());
    }

    [Fact]
    public void SkipField_MultipleFields()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("a,b,c,d\n", opts);
        reader.SkipField();
        reader.SkipField();
        Assert.Equal("c", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  TryAdvanceToNextRow
// ═══════════════════════════════════════════════════════════════════════

public class TryAdvanceToNextRowTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Fact]
    public void TryAdvanceToNextRow_ReturnsTrueWhenMoreRows()
    {
        var reader = CsvReaderFactory.Create("row1\nrow2\n", Opts);
        reader.ReadString();
        Assert.True(reader.TryAdvanceToNextRow());
    }

    [Fact]
    public void TryAdvanceToNextRow_ReturnsFalseAtEnd()
    {
        var reader = CsvReaderFactory.Create("only\n", Opts);
        reader.ReadString();
        Assert.False(reader.TryAdvanceToNextRow());
    }

    [Fact]
    public void TryAdvanceToNextRow_IteratesAllRows()
    {
        var reader = CsvReaderFactory.Create("1\n2\n3\n", Opts);
        var values = new List<int>();
        do
        {
            values.Add(reader.ReadInt32());
        }
        while (reader.TryAdvanceToNextRow());

        Assert.Equal(new[] { 1, 2, 3 }, values);
    }

    [Fact]
    public void TryAdvanceToNextRow_ResetsColumnIndex()
    {
        var reader = CsvReaderFactory.Create("a,b\nc,d\n", Opts);
        Assert.Equal("a", reader.ReadString());
        Assert.Equal("b", reader.ReadString());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal("c", reader.ReadString());
        Assert.Equal("d", reader.ReadString());
    }

    [Fact]
    public void TryAdvanceToNextRow_WithCrLfNewLine()
    {
        var opts = Opts with { NewLine = "\r\n" };
        var reader = CsvReaderFactory.Create("1\r\n2\r\n", opts);
        Assert.Equal(1, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(2, reader.ReadInt32());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  AllowRowComments
// ═══════════════════════════════════════════════════════════════════════

public class AllowRowCommentsTests
{
    [Fact]
    public void AllowRowComments_True_SkipsCommentRows()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = true,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("1\n#comment\n2\n", opts);
        Assert.Equal(1, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(2, reader.ReadInt32());
    }

    [Fact]
    public void AllowRowComments_False_DoesNotSkipCommentRows()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("1\n#comment\n2\n", opts);
        reader.ReadInt32();
        reader.TryAdvanceToNextRow();
        // Next row starts with '#'
        Assert.Equal("#comment", reader.ReadString());
    }

    [Fact]
    public void AllowRowComments_SkipsMultipleConsecutiveCommentRows()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = true,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("1\n#c1\n#c2\n#c3\n2\n", opts);
        Assert.Equal(1, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(2, reader.ReadInt32());
    }

    [Fact]
    public void AllowRowComments_CommentAtEnd_ReturnsFalse()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = true,
            NewLine = "\n", Separator = ','
        };
        var reader = CsvReaderFactory.Create("1\n#trailing\n", opts);
        reader.ReadInt32();
        Assert.False(reader.TryAdvanceToNextRow());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  AllowColumnComments
// ═══════════════════════════════════════════════════════════════════════

public class AllowColumnCommentsTests
{
    [Fact]
    public void AllowColumnComments_True_SkipsCommentColumn()
    {
        // Mirrors sandbox/csv/text.csv:  ID,#コメント,テキスト(ja),テキスト(en)
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "ID,#コメント,テキスト(ja),テキスト(en)\n" +
                  "Toast.9997,サンプル,こんにちは！,Hello!\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        Assert.Equal("Toast.9997", reader.ReadString());
        Assert.Equal("こんにちは！", reader.ReadString());  // col 2 (col 1 skipped)
        Assert.Equal("Hello!", reader.ReadString());         // col 3
    }

    [Fact]
    public void AllowColumnComments_True_MultipleCommentColumns()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "#note,value,#ignore\nignored,42,alsoIgnored\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        Assert.Equal(42, reader.ReadInt32());
    }

    [Fact]
    public void AllowColumnComments_True_MultipleRows_SkipsConsistently()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "id,#skip,name\n1,X,Alice\n2,Y,Bob\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        Assert.Equal(1, reader.ReadInt32());
        Assert.Equal("Alice", reader.ReadString());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(2, reader.ReadInt32());
        Assert.Equal("Bob", reader.ReadString());
    }

    [Fact]
    public void AllowColumnComments_False_DoesNotSkipHashColumn()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "id,#comment,name\n1,X,Alice\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        Assert.Equal("1", reader.ReadString());
        Assert.Equal("X", reader.ReadString());   // NOT skipped
        Assert.Equal("Alice", reader.ReadString());
    }

    [Fact]
    public void AllowColumnComments_True_HasHeader_False_NoMaskBuilt()
    {
        // When HasHeader=false, SkipHeader is a no-op and mask is never built.
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "#col,42\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();  // no-op

        Assert.Equal("#col", reader.ReadString());
        Assert.Equal(42, reader.ReadInt32());
    }

    [Fact]
    public void AllowColumnComments_SkipField_SkipsLogicalField()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = false,
            NewLine = "\n", Separator = ','
        };
        var csv = "a,#skip,b,c\nA,S,B,C\n";
        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        reader.SkipField();                       // skip "A" (col 0)
        Assert.Equal("B", reader.ReadString());   // col 2 (col 1 auto-skipped)
        Assert.Equal("C", reader.ReadString());   // col 3
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Quote handling
// ═══════════════════════════════════════════════════════════════════════

public class QuoteTests
{
    [Fact]
    public void Quote_None_TreatsQuoteAsRegularCharacter()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.None
        };
        // The double-quote is read as part of the value.
        var reader = CsvReaderFactory.Create("\"hello\"\n", opts);
        Assert.Equal("\"hello\"", reader.ReadString());
    }

    [Fact]
    public void Quote_Minimal_ParsesQuotedField()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.Minimal
        };
        var reader = CsvReaderFactory.Create("\"hello world\"\n", opts);
        Assert.Equal("hello world", reader.ReadString());
    }

    [Fact]
    public void Quote_Minimal_MixedQuotedAndUnquotedFields()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.Minimal
        };
        var reader = CsvReaderFactory.Create("plain,\"quoted value\",42\n", opts);
        Assert.Equal("plain", reader.ReadString());
        Assert.Equal("quoted value", reader.ReadString());
        Assert.Equal(42, reader.ReadInt32());
    }

    [Fact]
    public void Quote_Minimal_QuotedFieldContainingComma()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.Minimal
        };
        var reader = CsvReaderFactory.Create("\"a,b,c\",end\n", opts);
        Assert.Equal("a,b,c", reader.ReadString());
        Assert.Equal("end", reader.ReadString());
    }

    [Fact]
    public void Quote_Minimal_MalformedUnclosedQuote_ReadsToEnd()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.Minimal
        };
        // Malformed: no closing quote — reader should consume to end of sequence.
        var reader = CsvReaderFactory.Create("\"unclosed", opts);
        var val = reader.ReadString();
        Assert.Equal("unclosed", val);
    }

    [Fact]
    public void Quote_All_ParsesQuotedField()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.All
        };
        var reader = CsvReaderFactory.Create("\"hello\",\"world\"\n", opts);
        Assert.Equal("hello", reader.ReadString());
        Assert.Equal("world", reader.ReadString());
    }

    [Fact]
    public void Quote_All_UnquotedField_ThrowsFormatException()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.All
        };
        // Quote.All requires all fields to be quoted; an unquoted field must throw.
        var reader = CsvReaderFactory.Create("unquoted\n", opts);
        try { reader.ReadString(); Assert.Fail("Expected FormatException"); }
        catch (FormatException) { }
    }

    [Fact]
    public void Quote_NoneNumeric_ParsesQuotedField()
    {
        var opts = new CsvTranscodeOptions
        {
            HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
            NewLine = "\n", Separator = ',', Quote = Quote.NoneNumeric
        };
        var reader = CsvReaderFactory.Create("\"text\",42\n", opts);
        Assert.Equal("text", reader.ReadString());
        Assert.Equal(42, reader.ReadInt32());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Multi-byte newline (CRLF) correctness
// ═══════════════════════════════════════════════════════════════════════

public class CrLfNewLineTests
{
    private static CsvTranscodeOptions CrLfOpts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\r\n", Separator = ','
    };

    [Fact]
    public void CrLf_SingleField_ParsesCorrectly()
    {
        var reader = CsvReaderFactory.Create("hello\r\n", CrLfOpts);
        Assert.Equal("hello", reader.ReadString());
    }

    [Fact]
    public void CrLf_MultipleRows_ParsesCorrectly()
    {
        var reader = CsvReaderFactory.Create("1\r\n2\r\n3\r\n", CrLfOpts);
        Assert.Equal(1, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(2, reader.ReadInt32());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal(3, reader.ReadInt32());
        Assert.False(reader.TryAdvanceToNextRow());
    }

    [Fact]
    public void CrLf_MultipleFieldsPerRow_ParsesCorrectly()
    {
        var reader = CsvReaderFactory.Create("a,b,c\r\nx,y,z\r\n", CrLfOpts);
        Assert.Equal("a", reader.ReadString());
        Assert.Equal("b", reader.ReadString());
        Assert.Equal("c", reader.ReadString());
        Assert.True(reader.TryAdvanceToNextRow());
        Assert.Equal("x", reader.ReadString());
        Assert.Equal("y", reader.ReadString());
        Assert.Equal("z", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  End property and edge cases
// ═══════════════════════════════════════════════════════════════════════

public class EdgeCaseTests
{
    private static CsvTranscodeOptions Opts => new()
    {
        HasHeader = false, AllowColumnComments = false, AllowRowComments = false,
        NewLine = "\n", Separator = ','
    };

    [Fact]
    public void End_InitiallyFalseWhenDataPresent()
    {
        var reader = CsvReaderFactory.Create("data\n", Opts);
        Assert.False(reader.End);
    }

    [Fact]
    public void End_TrueAfterAllDataConsumed()
    {
        var reader = CsvReaderFactory.Create("data\n", Opts);
        reader.ReadString();
        reader.TryAdvanceToNextRow();
        Assert.True(reader.End);
    }

    [Fact]
    public void Consumed_IncreasesAsDataIsRead()
    {
        var reader = CsvReaderFactory.Create("ab,cd\n", Opts);
        var before = reader.Consumed;
        reader.ReadString();
        Assert.True(reader.Consumed > before);
    }

    [Fact]
    public void Remaining_DecreasesAsDataIsRead()
    {
        var reader = CsvReaderFactory.Create("ab,cd\n", Opts);
        var before = reader.Remaining;
        reader.ReadString();
        Assert.True(reader.Remaining < before);
    }

    [Fact]
    public void Options_ReturnsConfiguredOptions()
    {
        var opts = Opts with { Separator = ';' };
        var reader = CsvReaderFactory.Create("a;b\n", opts);
        Assert.Equal(';', reader.Options.Separator);
    }

    [Fact]
    public void SemicolonSeparator_ParsesCorrectly()
    {
        var opts = Opts with { Separator = ';' };
        var reader = CsvReaderFactory.Create("hello;world\n", opts);
        Assert.Equal("hello", reader.ReadString());
        Assert.Equal("world", reader.ReadString());
    }

    [Fact]
    public void TabSeparator_ParsesCorrectly()
    {
        var opts = Opts with { Separator = '\t' };
        var reader = CsvReaderFactory.Create("col1\tcol2\n", opts);
        Assert.Equal("col1", reader.ReadString());
        Assert.Equal("col2", reader.ReadString());
    }

    [Fact]
    public void EmptyInput_EndIsTrue()
    {
        var reader = CsvReaderFactory.Create("", Opts);
        Assert.True(reader.End);
    }

    [Fact]
    public void FullRoundTrip_TextCsvLike()
    {
        // Mirrors the structure of sandbox/csv/text.csv
        var opts = new CsvTranscodeOptions
        {
            HasHeader = true, AllowColumnComments = true, AllowRowComments = true,
            NewLine = "\n", Separator = ','
        };
        var csv =
            "ID,#コメント,テキスト(ja),テキスト(en)\n" +
            "#IDは数字4桁までしか想定していないので注意。\n" +
            "Toast.9997,サンプル(埋め込みなし),こんにちは！,Hello!\n" +
            "Toast.9998,サンプル(埋め込みあり),{0}が渡しました。,{0} gave.\n" +
            "Toast.9999,サンプル(書式),魔法石 {0:N0}個,Magic Store {0:N0}\n";

        var reader = CsvReaderFactory.Create(csv, opts);
        reader.SkipHeader();

        var ids = new List<string>();
        var jaTexts = new List<string>();
        var enTexts = new List<string>();
        do
        {
            ids.Add(reader.ReadString());
            jaTexts.Add(reader.ReadString());
            enTexts.Add(reader.ReadString());
        }
        while (reader.TryAdvanceToNextRow());

        Assert.Equal(new[] { "Toast.9997", "Toast.9998", "Toast.9999" }, ids);
        Assert.Equal(new[] { "こんにちは！", "{0}が渡しました。", "魔法石 {0:N0}個" }, jaTexts);
        Assert.Equal(new[] { "Hello!", "{0} gave.", "Magic Store {0:N0}" }, enTexts);
    }
}
