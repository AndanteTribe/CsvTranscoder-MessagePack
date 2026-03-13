using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using Localization;
using MessagePack;
using MessagePack.Resolvers;

namespace CsvTranscoder.MessagePack.Localization.Tests;

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
        formatter.Transcode(ref writer, ref reader, opts);
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
    public void LocalizationCsvResolver_GetFormatter_UnknownType_ReturnsNull()
    {
        var formatter = LocalizationCsvResolver.Instance.GetFormatter<string>();
        Assert.Null(formatter);
    }

    [Fact]
    public void LocalizationCsvResolver_GetFormatter_ReturnsCachedInstance()
    {
        var formatter1 = LocalizationCsvResolver.Instance.GetFormatter<LocalizeFormat>();
        var formatter2 = LocalizationCsvResolver.Instance.GetFormatter<LocalizeFormat>();
        Assert.Same(formatter1, formatter2);
    }
}
