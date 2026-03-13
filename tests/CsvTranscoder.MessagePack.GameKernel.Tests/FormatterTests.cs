using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;
using GameKernel;
using MessagePack;

namespace CsvTranscoder.MessagePack.GameKernel.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  Test enum
// ═══════════════════════════════════════════════════════════════════════

public enum TestGroup : int { Air = 0, Ground = 1, Common = 2 }

// ═══════════════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════════════

file static class FormatterTestHelper
{
    public static readonly MessagePackSerializerOptions GameKernelMpOptions =
        MessagePackSerializerOptions.Standard.WithResolver(
            global::MessagePack.Resolvers.CompositeResolver.Create(
                global::GameKernel.MessagePack.GameKernelResolver.Shared,
                global::MessagePack.Resolvers.StandardResolver.Instance));

    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
        Resolver = GameKernelCsvResolver.Instance,
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
        return MessagePackSerializer.Deserialize<T>(buffer.WrittenMemory, mpOptions ?? GameKernelMpOptions);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  MasterIdCsvFormatter tests
// ═══════════════════════════════════════════════════════════════════════

public class MasterIdCsvFormatterTests
{
    [Theory]
    [InlineData("Air.0001", TestGroup.Air, 1u)]
    [InlineData("Ground.0001", TestGroup.Ground, 1u)]
    [InlineData("Common.0002", TestGroup.Common, 2u)]
    [InlineData("air.0010", TestGroup.Air, 10u)]
    public void MasterIdCsvFormatter_Roundtrip(string input, TestGroup expectedGroup, uint expectedId)
    {
        var result = FormatterTestHelper.Transcode(input, MasterIdCsvFormatter<TestGroup>.Instance);
        Assert.Equal(expectedGroup, result.Group);
        Assert.Equal(expectedId, result.Id);
    }

    [Fact]
    public void MasterIdCsvFormatter_MissingSeparator_Throws()
    {
        Assert.Throws<FormatException>(() =>
            FormatterTestHelper.Transcode("Air0001", MasterIdCsvFormatter<TestGroup>.Instance));
    }

    [Fact]
    public void MasterIdCsvFormatter_InvalidGroup_Throws()
    {
        Assert.Throws<FormatException>(() =>
            FormatterTestHelper.Transcode("Unknown.0001", MasterIdCsvFormatter<TestGroup>.Instance));
    }

    [Fact]
    public void MasterIdCsvFormatter_InvalidId_Throws()
    {
        Assert.Throws<FormatException>(() =>
            FormatterTestHelper.Transcode("Air.abc", MasterIdCsvFormatter<TestGroup>.Instance));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ObscuredCsvFormatter tests
// ═══════════════════════════════════════════════════════════════════════

public class ObscuredCsvFormatterTests
{
    // ObscuredCsvFormatter<T> delegates to options.Resolver for the inner T formatter.
    // Use StandardResolver so it can find formatters for primitive types.
    private static readonly CsvTranscodeOptions s_standardOptions = new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
        Resolver = AndanteTribe.Csv.StandardResolver.Instance,
    };

    [Theory]
    [InlineData("0", 0u)]
    [InlineData("42", 42u)]
    [InlineData("4294967295", uint.MaxValue)]
    public void ObscuredCsvFormatter_UInt_Roundtrip(string input, uint expectedValue)
    {
        var result = FormatterTestHelper.Transcode(
            input,
            ObscuredCsvFormatter<uint>.Instance,
            s_standardOptions);
        Assert.Equal(expectedValue, result.Value);
    }

    [Theory]
    [InlineData("-2147483648", int.MinValue)]
    [InlineData("0", 0)]
    [InlineData("2147483647", int.MaxValue)]
    public void ObscuredCsvFormatter_Int_Roundtrip(string input, int expectedValue)
    {
        var result = FormatterTestHelper.Transcode(
            input,
            ObscuredCsvFormatter<int>.Instance,
            s_standardOptions);
        Assert.Equal(expectedValue, result.Value);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  GameKernelCsvResolver tests
// ═══════════════════════════════════════════════════════════════════════

public class GameKernelCsvResolverTests
{
    [Fact]
    public void GameKernelCsvResolver_GetFormatter_MasterId_ReturnsFormatter()
    {
        var formatter = GameKernelCsvResolver.Instance.GetFormatter<MasterId<TestGroup>>();
        Assert.NotNull(formatter);
        Assert.IsType<MasterIdCsvFormatter<TestGroup>>(formatter);
    }

    [Fact]
    public void GameKernelCsvResolver_GetFormatter_Obscured_ReturnsFormatter()
    {
        var formatter = GameKernelCsvResolver.Instance.GetFormatter<Obscured<uint>>();
        Assert.NotNull(formatter);
        Assert.IsType<ObscuredCsvFormatter<uint>>(formatter);
    }

    [Fact]
    public void GameKernelCsvResolver_GetFormatter_UnknownType_ReturnsNull()
    {
        var formatter = GameKernelCsvResolver.Instance.GetFormatter<string>();
        Assert.Null(formatter);
    }

    [Fact]
    public void GameKernelCsvResolver_GetFormatter_MasterId_ReturnsCachedInstance()
    {
        var formatter1 = GameKernelCsvResolver.Instance.GetFormatter<MasterId<TestGroup>>();
        var formatter2 = GameKernelCsvResolver.Instance.GetFormatter<MasterId<TestGroup>>();
        Assert.Same(formatter1, formatter2);
    }
}
