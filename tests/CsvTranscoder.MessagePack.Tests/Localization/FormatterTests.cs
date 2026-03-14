using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using Localization;
using MessagePack;
using MessagePack.Resolvers;

namespace CsvTranscoder.MessagePack.Tests.Localization;

// ═══════════════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════════════

file static class FormatterTestHelper
{
    public static readonly MessagePackSerializerOptions LocalizationMpOptions =
        MessagePackSerializerOptions.Standard.WithResolver(
            global::MessagePack.Resolvers.CompositeResolver.Create(
                global::Localization.MessagePack.LocalizationResolver.Shared,
                global::MessagePack.Resolvers.StandardResolver.Instance));

    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
        Resolver = LocalizationCsvResolver.Instance,
    };

    public static T Transcode<T>(
        string csvField,
        ICsvFormatter<T> formatter,
        CsvTranscodeOptions? options = null,
        MessagePackSerializerOptions? mpOptions = null)
    {
        var opts = options ?? SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csvField + "\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        formatter.Transcode(ref writer, ref reader);
        writer.Flush();
        return MessagePackSerializer.Deserialize<T>(buffer.WrittenMemory, mpOptions ?? LocalizationMpOptions);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  LocalizeFormatCsvFormatter tests
// ═══════════════════════════════════════════════════════════════════════

public class LocalizeFormatCsvFormatterTests
{
    [Theory]
    [InlineData("こんにちは！")]
    [InlineData("Hello!")]
    [InlineData("Simple text")]
    public void LocalizeFormatCsvFormatter_PlainText_Roundtrip(string input)
    {
        var result = FormatterTestHelper.Transcode(input, LocalizeFormatCsvFormatter.Instance);
        Assert.Equal(input, result.ToString());
    }

    [Theory]
    [InlineData("{0}が{1}にアイテム{2}を{3}個渡しました。")]
    [InlineData("{0} items")]
    [InlineData("{0:N0} items")]
    public void LocalizeFormatCsvFormatter_WithEmbeds_Roundtrip(string input)
    {
        var result = FormatterTestHelper.Transcode(input, LocalizeFormatCsvFormatter.Instance);
        Assert.Equal(input, result.ToString());
    }

    [Fact]
    public void LocalizeFormatCsvFormatter_CommaSeparatedValue_Roundtrip()
    {
        // Values containing commas must be quoted in CSV. This test verifies that a
        // properly quoted field is handled correctly.
        const string expectedText = "Hello, {0}!";
        const string quotedCsvField = "\"Hello, {0}!\"";
        var result = FormatterTestHelper.Transcode(quotedCsvField, LocalizeFormatCsvFormatter.Instance);
        Assert.Equal(expectedText, result.ToString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  LocalizationCsvResolver tests
// ═══════════════════════════════════════════════════════════════════════

public class LocalizationCsvResolverTests
{
    [Fact]
    public void LocalizationCsvResolver_GetFormatter_LocalizeFormat_ReturnsFormatter()
    {
        var formatter = LocalizationCsvResolver.Instance.GetFormatter<LocalizeFormat>();
        Assert.NotNull(formatter);
        Assert.IsType<LocalizeFormatCsvFormatter>(formatter);
    }

    [Fact]
    public void LocalizationCsvResolver_GetFormatter_String_ReturnsLocalizedMemberJapaneseFormatter()
    {
        var formatter = LocalizationCsvResolver.Instance.GetFormatter<string>();
        Assert.NotNull(formatter);
        Assert.IsType<LocalizedMemberJapaneseCsvFormatter>(formatter);
    }

    [Fact]
    public void LocalizationCsvResolver_GetFormatter_UnknownType_ReturnsNull()
    {
        var formatter = LocalizationCsvResolver.Instance.GetFormatter<int>();
        Assert.Null(formatter);
    }

    [Fact]
    public void LocalizationCsvResolver_GetFormatter_ReturnsCachedInstance()
    {
        var formatter1 = LocalizationCsvResolver.Instance.GetFormatter<LocalizeFormat>();
        var formatter2 = LocalizationCsvResolver.Instance.GetFormatter<LocalizeFormat>();
        Assert.Same(formatter1, formatter2);
    }

    [Fact]
    public void LocalizationCsvResolver_StringFormatter_TreatsAllStringsAsLocalizedPairs()
    {
        // When LocalizationCsvResolver is placed before StandardResolver in a composite,
        // every string column is handled by LocalizedMemberJapaneseCsvFormatter.
        // This means ALL string [Key] members are expected to come as ja+en column pairs,
        // even those without [LocalizedMember]. Only use this resolver for entities whose
        // string properties are all localized pairs.
        var options = new CsvTranscodeOptions
        {
            HasHeader = false,
            NewLine = "\n",
            Separator = ',',
            Resolver = AndanteTribe.Csv.CompositeResolver.Create(LocalizationCsvResolver.Instance, AndanteTribe.Csv.StandardResolver.Instance),
        };

        // CSV row with a ja+en pair: the resolver picks Japanese, skips English.
        var bytes = Encoding.UTF8.GetBytes("日本語,English\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), options);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        options.Resolver.GetFormatterWithVerify<string>().Transcode(ref writer, ref reader);
        writer.Flush();

        var result = MessagePackSerializer.Deserialize<string>(buffer.WrittenMemory);
        Assert.Equal("日本語", result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  LocalizedMemberJapaneseCsvFormatter tests
// ═══════════════════════════════════════════════════════════════════════

public class LocalizedMemberJapaneseCsvFormatterTests
{
    [Theory]
    [InlineData("ゲイザー,Gazer", "ゲイザー")]
    [InlineData("こんにちは,Hello", "こんにちは")]
    [InlineData("日本語,English", "日本語")]
    public void LocalizedMemberJapaneseCsvFormatter_ReadsJapanese_SkipsEnglish(string csvRow, string expectedJa)
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csvRow + "\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        LocalizedMemberJapaneseCsvFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();

        var result = MessagePackSerializer.Deserialize<string>(buffer.WrittenMemory);
        Assert.Equal(expectedJa, result);
    }

    [Fact]
    public void LocalizedMemberJapaneseCsvFormatter_ConsumesExactlyTwoColumns()
    {
        // CSV row: ja,en,extra  — only ja should be stored; extra should be readable after.
        const string csv = "日本語,English,追加\n";
        var opts = FormatterTestHelper.SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csv);
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        LocalizedMemberJapaneseCsvFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();

        // The next field in the reader should be the third column ("追加").
        Assert.Equal("追加", reader.ReadString());
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  LocalizedMemberEnglishCsvFormatter tests
// ═══════════════════════════════════════════════════════════════════════

public class LocalizedMemberEnglishCsvFormatterTests
{
    [Theory]
    [InlineData("ゲイザー,Gazer", "Gazer")]
    [InlineData("こんにちは,Hello", "Hello")]
    [InlineData("日本語,English", "English")]
    public void LocalizedMemberEnglishCsvFormatter_ReadsEnglish_SkipsJapanese(string csvRow, string expectedEn)
    {
        var opts = FormatterTestHelper.SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csvRow + "\n");
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);
        LocalizedMemberEnglishCsvFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();

        var result = MessagePackSerializer.Deserialize<string>(buffer.WrittenMemory);
        Assert.Equal(expectedEn, result);
    }

    [Fact]
    public void LocalizedMemberEnglishCsvFormatter_ConsumesExactlyTwoColumns()
    {
        const string csv = "日本語,English,追加\n";
        var opts = FormatterTestHelper.SimpleOptions;
        var bytes = Encoding.UTF8.GetBytes(csv);
        var reader = new CsvReader(new ReadOnlySequence<byte>(bytes), opts);
        var buffer = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(buffer);

        LocalizedMemberEnglishCsvFormatter.Instance.Transcode(ref writer, ref reader);
        writer.Flush();

        // The next field should be the third column ("追加").
        Assert.Equal("追加", reader.ReadString());
    }
}
