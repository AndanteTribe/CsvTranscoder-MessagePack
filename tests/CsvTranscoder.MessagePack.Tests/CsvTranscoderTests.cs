using System.Buffers;
using System.Text;
using AndanteTribe.Csv;
using MessagePack;
// 'using static' avoids the CsvTranscoder class name being shadowed by the enclosing
// namespace component 'CsvTranscoder' in namespace CsvTranscoder.MessagePack.Tests.
using static global::AndanteTribe.Csv.CsvTranscoder;

namespace CsvTranscoder.MessagePack.Tests;

// ═══════════════════════════════════════════════════════════════════════
//  Helpers
// ═══════════════════════════════════════════════════════════════════════

file static class TranscoderTestHelper
{
    /// <summary>Options with no header, no comments, LF newline.</summary>
    public static CsvTranscodeOptions SimpleOptions => new()
    {
        HasHeader = false,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
    };

    /// <summary>Options with a header row, no comments, LF newline.</summary>
    public static CsvTranscodeOptions HeaderOptions => new()
    {
        HasHeader = true,
        AllowColumnComments = false,
        AllowRowComments = false,
        NewLine = "\n",
        Separator = ',',
    };

    public static ReadOnlySequence<byte> ToSequence(string csv)
        => new(Encoding.UTF8.GetBytes(csv));

    public static ReadOnlyMemory<byte> ToMemory(string csv)
        => Encoding.UTF8.GetBytes(csv);

    public static Stream ToStream(string csv)
        => new MemoryStream(Encoding.UTF8.GetBytes(csv));

    public static T[] Deserialize<T>(ArrayBufferWriter<byte> buffer)
        => MessagePackSerializer.Deserialize<T[]>(buffer.WrittenMemory);

    public static T[] Deserialize<T>(MemoryStream ms)
        => MessagePackSerializer.Deserialize<T[]>(ms.ToArray());
}

// ═══════════════════════════════════════════════════════════════════════
//  ReadOnlySequence<byte> inputs
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderSequenceInputTests
{
    private static readonly CsvTranscodeOptions s_opts = TranscoderTestHelper.SimpleOptions;

    [Fact]
    public void ToMessagePack_Sequence_IBufferWriter_ProducesExpectedArray()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n2\n3\n");
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(seq, output, s_opts);
        Assert.Equal([1, 2, 3], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Sequence_IBufferWriter_NullWriterThrows()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(seq, (IBufferWriter<byte>)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_Sequence_RefWriter_ProducesExpectedArray()
    {
        var seq = TranscoderTestHelper.ToSequence("10\n20\n");
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        ToMessagePack<int>(seq, ref writer, s_opts);
        writer.Flush();
        Assert.Equal([10, 20], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Sequence_Stream_ProducesExpectedArray()
    {
        var seq = TranscoderTestHelper.ToSequence("5\n6\n");
        using var ms = new MemoryStream();
        ToMessagePack<int>(seq, ms, s_opts);
        Assert.Equal([5, 6], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public void ToMessagePack_Sequence_Stream_NullStreamThrows()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(seq, (Stream)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_Sequence_Stream_NonWritableStreamThrows()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n");
        using var ms = new MemoryStream(new byte[16], writable: false);
        Assert.Throws<ArgumentException>(() => ToMessagePack<int>(seq, ms, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_Sequence_Stream_ProducesExpectedArray()
    {
        var seq = TranscoderTestHelper.ToSequence("7\n8\n9\n");
        using var ms = new MemoryStream();
        await ToMessagePackAsync<int>(seq, ms, s_opts);
        Assert.Equal([7, 8, 9], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public async Task ToMessagePackAsync_Sequence_Stream_NullStreamThrows()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n");
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ToMessagePackAsync<int>(seq, null!, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_Sequence_Stream_NonWritableStreamThrows()
    {
        var seq = TranscoderTestHelper.ToSequence("1\n");
        using var ms = new MemoryStream(new byte[16], writable: false);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ToMessagePackAsync<int>(seq, ms, s_opts));
    }

    [Fact]
    public void ToMessagePack_Sequence_EmptyCsv_ProducesEmptyArray()
    {
        var seq = TranscoderTestHelper.ToSequence(string.Empty);
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(seq, output, s_opts);
        Assert.Empty(TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Sequence_WithHeader_SkipsHeaderRow()
    {
        var seq = TranscoderTestHelper.ToSequence("value\n100\n200\n");
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(seq, output, TranscoderTestHelper.HeaderOptions);
        Assert.Equal([100, 200], TranscoderTestHelper.Deserialize<int>(output));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ref CsvReader inputs
// CsvReader is a ref struct; ref struct locals cannot be captured in
// lambdas, so exception tests use try/catch directly.
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderCsvReaderInputTests
{
    private static readonly CsvTranscodeOptions s_opts = TranscoderTestHelper.SimpleOptions;

    [Fact]
    public void ToMessagePack_Reader_IBufferWriter_ProducesExpectedArray()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n2\n3\n"));
        var reader = new CsvReader(bytes, s_opts);
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(ref reader, output);
        Assert.Equal([1, 2, 3], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Reader_IBufferWriter_NullWriterThrows()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n"));
        var reader = new CsvReader(bytes, s_opts);
        try
        {
            ToMessagePack<int>(ref reader, (IBufferWriter<byte>)null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException) { }
    }

    [Fact]
    public void ToMessagePack_Reader_RefWriter_ProducesExpectedArray()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("10\n20\n"));
        var reader = new CsvReader(bytes, s_opts);
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        ToMessagePack<int>(ref reader, ref writer);
        writer.Flush();
        Assert.Equal([10, 20], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Reader_Stream_ProducesExpectedArray()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("5\n6\n"));
        var reader = new CsvReader(bytes, s_opts);
        using var ms = new MemoryStream();
        ToMessagePack<int>(ref reader, ms);
        Assert.Equal([5, 6], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public void ToMessagePack_Reader_Stream_NullStreamThrows()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n"));
        var reader = new CsvReader(bytes, s_opts);
        try
        {
            ToMessagePack<int>(ref reader, (Stream)null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException) { }
    }

    [Fact]
    public void ToMessagePack_Reader_Stream_NonWritableStreamThrows()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n"));
        var reader = new CsvReader(bytes, s_opts);
        using var ms = new MemoryStream(new byte[16], writable: false);
        try
        {
            ToMessagePack<int>(ref reader, ms);
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException) { }
    }

    [Fact]
    public async Task ToMessagePackAsync_Reader_Stream_ProducesExpectedArray()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("7\n8\n9\n"));
        var reader = new CsvReader(bytes, s_opts);
        using var ms = new MemoryStream();
        await ToMessagePackAsync<int>(ref reader, ms);
        Assert.Equal([7, 8, 9], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public void ToMessagePackAsync_Reader_Stream_NullStreamThrows()
    {
        // ToMessagePackAsync(ref CsvReader, ...) is not async; validation is synchronous.
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n"));
        var reader = new CsvReader(bytes, s_opts);
        try
        {
            ToMessagePackAsync<int>(ref reader, null!);
            Assert.Fail("Expected ArgumentNullException");
        }
        catch (ArgumentNullException) { }
    }

    [Fact]
    public void ToMessagePackAsync_Reader_Stream_NonWritableStreamThrows()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("1\n"));
        var reader = new CsvReader(bytes, s_opts);
        using var ms = new MemoryStream(new byte[16], writable: false);
        try
        {
            ToMessagePackAsync<int>(ref reader, ms);
            Assert.Fail("Expected ArgumentException");
        }
        catch (ArgumentException) { }
    }

    [Fact]
    public void ToMessagePack_Reader_UsesReaderOptions()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("42\n"));
        var reader = new CsvReader(bytes, s_opts);
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(ref reader, output);
        Assert.Equal([42], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Reader_EmptyCsv_ProducesEmptyArray()
    {
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(string.Empty));
        var reader = new CsvReader(bytes, s_opts);
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(ref reader, output);
        Assert.Empty(TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Reader_WithHeader_SkipsHeaderRow()
    {
        var headerOpts = TranscoderTestHelper.HeaderOptions;
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("value\n100\n200\n"));
        var reader = new CsvReader(bytes, headerOpts);
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(ref reader, output);
        Assert.Equal([100, 200], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Reader_Stream_WithHeader_SkipsHeaderRow()
    {
        var headerOpts = TranscoderTestHelper.HeaderOptions;
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("value\n55\n66\n"));
        var reader = new CsvReader(bytes, headerOpts);
        using var ms = new MemoryStream();
        ToMessagePack<int>(ref reader, ms);
        Assert.Equal([55, 66], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public async Task ToMessagePackAsync_Reader_Stream_WithHeader_SkipsHeaderRow()
    {
        var headerOpts = TranscoderTestHelper.HeaderOptions;
        var bytes = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("value\n77\n88\n"));
        var reader = new CsvReader(bytes, headerOpts);
        using var ms = new MemoryStream();
        await ToMessagePackAsync<int>(ref reader, ms);
        Assert.Equal([77, 88], TranscoderTestHelper.Deserialize<int>(ms));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  ReadOnlyMemory<byte> inputs
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderMemoryInputTests
{
    private static readonly CsvTranscodeOptions s_opts = TranscoderTestHelper.SimpleOptions;

    [Fact]
    public void ToMessagePack_Memory_IBufferWriter_ProducesExpectedArray()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n2\n3\n");
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(mem, output, s_opts);
        Assert.Equal([1, 2, 3], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Memory_RefWriter_ProducesExpectedArray()
    {
        var mem = TranscoderTestHelper.ToMemory("10\n20\n");
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        ToMessagePack<int>(mem, ref writer, s_opts);
        writer.Flush();
        Assert.Equal([10, 20], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Memory_Stream_ProducesExpectedArray()
    {
        var mem = TranscoderTestHelper.ToMemory("5\n6\n");
        using var ms = new MemoryStream();
        ToMessagePack<int>(mem, ms, s_opts);
        Assert.Equal([5, 6], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public async Task ToMessagePackAsync_Memory_Stream_ProducesExpectedArray()
    {
        var mem = TranscoderTestHelper.ToMemory("7\n8\n9\n");
        using var ms = new MemoryStream();
        await ToMessagePackAsync<int>(mem, ms, s_opts);
        Assert.Equal([7, 8, 9], TranscoderTestHelper.Deserialize<int>(ms));
    }

    [Fact]
    public void ToMessagePack_Memory_IBufferWriter_NullWriterThrows()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(mem, (IBufferWriter<byte>)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_Memory_Stream_NullStreamThrows()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(mem, (Stream)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_Memory_Stream_NonWritableStreamThrows()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n");
        using var ms = new MemoryStream(new byte[16], writable: false);
        Assert.Throws<ArgumentException>(() => ToMessagePack<int>(mem, ms, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_Memory_Stream_NullStreamThrows()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n");
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ToMessagePackAsync<int>(mem, null!, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_Memory_Stream_NonWritableStreamThrows()
    {
        var mem = TranscoderTestHelper.ToMemory("1\n");
        using var ms = new MemoryStream(new byte[16], writable: false);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ToMessagePackAsync<int>(mem, ms, s_opts));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Stream inputs
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderStreamInputTests
{
    private static readonly CsvTranscodeOptions s_opts = TranscoderTestHelper.SimpleOptions;

    [Fact]
    public void ToMessagePack_Stream_IBufferWriter_ProducesExpectedArray()
    {
        using var input = TranscoderTestHelper.ToStream("1\n2\n3\n");
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(input, output, s_opts);
        Assert.Equal([1, 2, 3], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Stream_IBufferWriter_NullInputStreamThrows()
    {
        var output = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>((Stream)null!, output, s_opts));
    }

    [Fact]
    public void ToMessagePack_Stream_IBufferWriter_NonReadableInputStreamThrows()
    {
        var noRead = new WriteOnlyStream(new MemoryStream());
        var output = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentException>(() =>
            ToMessagePack<int>(noRead, output, s_opts));
    }

    [Fact]
    public void ToMessagePack_Stream_IBufferWriter_NullWriterThrows()
    {
        using var input = TranscoderTestHelper.ToStream("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(input, (IBufferWriter<byte>)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_Stream_RefWriter_ProducesExpectedArray()
    {
        using var input = TranscoderTestHelper.ToStream("10\n20\n");
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        ToMessagePack<int>(input, ref writer, s_opts);
        writer.Flush();
        Assert.Equal([10, 20], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_Stream_RefWriter_NullInputStreamThrows()
    {
        // MessagePackWriter is a ref struct; instantiate inline to avoid capture in lambda.
        var output = new ArrayBufferWriter<byte>();
        Assert.Throws<ArgumentNullException>(() =>
        {
            var w = new MessagePackWriter(output);
            ToMessagePack<int>((Stream)null!, ref w, s_opts);
        });
    }

    [Fact]
    public void ToMessagePack_Stream_RefWriter_NonReadableInputStreamThrows()
    {
        var noRead = new WriteOnlyStream(new MemoryStream());
        Assert.Throws<ArgumentException>(() =>
        {
            var output = new ArrayBufferWriter<byte>();
            var w = new MessagePackWriter(output);
            ToMessagePack<int>(noRead, ref w, s_opts);
        });
    }

    [Fact]
    public void ToMessagePack_StreamStream_ProducesExpectedArray()
    {
        using var input = TranscoderTestHelper.ToStream("5\n6\n");
        using var output = new MemoryStream();
        ToMessagePack<int>(input, output, s_opts);
        Assert.Equal([5, 6], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public void ToMessagePack_StreamStream_NullInputStreamThrows()
    {
        using var output = new MemoryStream();
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>((Stream)null!, output, s_opts));
    }

    [Fact]
    public void ToMessagePack_StreamStream_NullOutputStreamThrows()
    {
        using var input = TranscoderTestHelper.ToStream("1\n");
        Assert.Throws<ArgumentNullException>(() =>
            ToMessagePack<int>(input, (Stream)null!, s_opts));
    }

    [Fact]
    public void ToMessagePack_StreamStream_NonReadableInputStreamThrows()
    {
        var noRead = new WriteOnlyStream(new MemoryStream());
        using var output = new MemoryStream();
        Assert.Throws<ArgumentException>(() => ToMessagePack<int>(noRead, output, s_opts));
    }

    [Fact]
    public void ToMessagePack_StreamStream_NonWritableOutputStreamThrows()
    {
        using var input = TranscoderTestHelper.ToStream("1\n");
        using var output = new MemoryStream(new byte[16], writable: false);
        Assert.Throws<ArgumentException>(() => ToMessagePack<int>(input, output, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_StreamStream_ProducesExpectedArray()
    {
        using var input = TranscoderTestHelper.ToStream("7\n8\n9\n");
        using var output = new MemoryStream();
        await ToMessagePackAsync<int>(input, output, s_opts);
        Assert.Equal([7, 8, 9], TranscoderTestHelper.Deserialize<int>(output));
    }

    [Fact]
    public async Task ToMessagePackAsync_StreamStream_NullInputStreamThrows()
    {
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ToMessagePackAsync<int>((Stream)null!, output, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_StreamStream_NullOutputStreamThrows()
    {
        using var input = TranscoderTestHelper.ToStream("1\n");
        await Assert.ThrowsAsync<ArgumentNullException>(async () =>
            await ToMessagePackAsync<int>(input, null!, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_StreamStream_NonReadableInputStreamThrows()
    {
        var noRead = new WriteOnlyStream(new MemoryStream());
        using var output = new MemoryStream();
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ToMessagePackAsync<int>(noRead, output, s_opts));
    }

    [Fact]
    public async Task ToMessagePackAsync_StreamStream_NonWritableOutputStreamThrows()
    {
        using var input = TranscoderTestHelper.ToStream("1\n");
        using var output = new MemoryStream(new byte[16], writable: false);
        await Assert.ThrowsAsync<ArgumentException>(async () =>
            await ToMessagePackAsync<int>(input, output, s_opts));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Integration – header, comments, string values, edge cases
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderIntegrationTests
{
    [Fact]
    public void ToMessagePack_StringValues_ProducesExpectedArray()
    {
        var opts = new CsvTranscodeOptions { HasHeader = false, AllowColumnComments = false, AllowRowComments = false, NewLine = "\n" };
        var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("hello\nworld\n"));
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<string>(seq, output, opts);
        var result = MessagePackSerializer.Deserialize<string[]>(output.WrittenMemory);
        Assert.Equal(["hello", "world"], result);
    }

    [Fact]
    public void ToMessagePack_WithHeader_SkipsHeaderRow()
    {
        var opts = new CsvTranscodeOptions { HasHeader = true, AllowColumnComments = false, AllowRowComments = false, NewLine = "\n" };
        var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("name\nalice\nbob\n"));
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<string>(seq, output, opts);
        var result = MessagePackSerializer.Deserialize<string[]>(output.WrittenMemory);
        Assert.Equal(["alice", "bob"], result);
    }

    [Fact]
    public void ToMessagePack_SingleRow_NoTrailingNewline()
    {
        var opts = new CsvTranscodeOptions { HasHeader = false, AllowColumnComments = false, AllowRowComments = false, NewLine = "\n" };
        var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("99"));
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(seq, output, opts);
        var result = MessagePackSerializer.Deserialize<int[]>(output.WrittenMemory);
        Assert.Equal([99], result);
    }

    [Fact]
    public void ToMessagePack_OnlyHeader_ProducesEmptyArray()
    {
        var opts = new CsvTranscodeOptions { HasHeader = true, AllowColumnComments = false, AllowRowComments = false, NewLine = "\n" };
        var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes("value\n"));
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(seq, output, opts);
        var result = MessagePackSerializer.Deserialize<int[]>(output.WrittenMemory);
        Assert.Empty(result);
    }

    [Fact]
    public void ToMessagePack_LargeInput_TriggersByteBufferWriterGrow()
    {
        // Write enough rows to force ByteBufferWriter to grow past its 65536-byte initial capacity.
        // Each MessagePack str8 value of 100 bytes encodes to ~103 bytes (2 bytes header + 1 byte len + 100 bytes payload).
        // 700 rows × ~103 bytes ≈ 72 100 bytes, exceeding the 65 536-byte initial capacity.
        const int valueLength = 100;
        const int rowCount = 700;
        var value = new string('x', valueLength);
        var sb = new StringBuilder();
        for (var i = 0; i < rowCount; i++)
        {
            sb.Append(value);
            sb.Append('\n');
        }

        var opts = new CsvTranscodeOptions { HasHeader = false, AllowColumnComments = false, AllowRowComments = false, NewLine = "\n" };
        var seq = new ReadOnlySequence<byte>(Encoding.UTF8.GetBytes(sb.ToString()));
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<string>(seq, output, opts);
        var result = MessagePackSerializer.Deserialize<string[]>(output.WrittenMemory);
        Assert.Equal(rowCount, result.Length);
        Assert.All(result, r => Assert.Equal(value, r));
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Null options — default CsvTranscodeOptions is used when null
//  Covers the `options ?? new CsvTranscodeOptions()` branch in each
//  ReadOnlySequence<byte> overload.
// ═══════════════════════════════════════════════════════════════════════

public class CsvTranscoderNullOptionsTests
{
    // Build a single-row CSV that is valid with the default CsvTranscodeOptions
    // (HasHeader=true, NewLine=Environment.NewLine, Separator=',').
    private static ReadOnlySequence<byte> MakeCsv()
    {
        var nl = Environment.NewLine;
        var csv = $"value{nl}7{nl}";
        return TranscoderTestHelper.ToSequence(csv);
    }

    [Fact]
    public void ToMessagePack_Sequence_IBufferWriter_NullOptions_UsesDefaults()
    {
        var output = new ArrayBufferWriter<byte>();
        ToMessagePack<int>(MakeCsv(), output, options: null);
        var result = MessagePackSerializer.Deserialize<int[]>(output.WrittenMemory);
        Assert.Equal([7], result);
    }

    [Fact]
    public void ToMessagePack_Sequence_RefWriter_NullOptions_UsesDefaults()
    {
        var output = new ArrayBufferWriter<byte>();
        var writer = new MessagePackWriter(output);
        ToMessagePack<int>(MakeCsv(), ref writer, options: null);
        writer.Flush();
        var result = MessagePackSerializer.Deserialize<int[]>(output.WrittenMemory);
        Assert.Equal([7], result);
    }

    [Fact]
    public void ToMessagePack_Sequence_Stream_NullOptions_UsesDefaults()
    {
        using var ms = new MemoryStream();
        ToMessagePack<int>(MakeCsv(), ms, options: null);
        var result = MessagePackSerializer.Deserialize<int[]>(ms.ToArray());
        Assert.Equal([7], result);
    }

    [Fact]
    public async Task ToMessagePackAsync_Sequence_Stream_NullOptions_UsesDefaults()
    {
        using var ms = new MemoryStream();
        await ToMessagePackAsync<int>(MakeCsv(), ms, options: null);
        var result = MessagePackSerializer.Deserialize<int[]>(ms.ToArray());
        Assert.Equal([7], result);
    }
}

// ═══════════════════════════════════════════════════════════════════════
//  Helper – write-only stream for validation tests
// ═══════════════════════════════════════════════════════════════════════

/// <summary>A stream wrapper that disables reading, used to test non-readable stream validation.</summary>
file sealed class WriteOnlyStream(Stream inner) : Stream
{
    public override bool CanRead => false;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }
    public override void Flush() => inner.Flush();
    public override int Read(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);

    protected override void Dispose(bool disposing)
    {
        if (disposing) inner.Dispose();
        base.Dispose(disposing);
    }
}
