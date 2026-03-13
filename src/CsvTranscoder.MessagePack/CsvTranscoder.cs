using System.Buffers;
using MessagePack;

namespace AndanteTribe.Csv;

/// <summary>
/// High-level API for transcoding CSV data into MessagePack format.
/// Produces a MessagePack array where each element corresponds to one CSV data row,
/// converted by the <see cref="ICsvFormatter{T}"/> resolved from <see cref="CsvTranscodeOptions.Resolver"/>.
/// </summary>
public static class CsvTranscoder
{
    /// <summary>
    /// Counts data rows in the CSV sequence (excluding header and comment rows)
    /// without producing any MessagePack output.
    /// </summary>
    private static int CountDataRows(ReadOnlySequence<byte> sequence, CsvTranscodeOptions options)
    {
        var counter = new CsvReader(sequence, options);
        if (options.HasHeader)
        {
            counter.SkipHeader();
        }

        if (counter.End) return 0;

        var count = 0;
        do
        {
            count++;
        }
        while (counter.TryAdvanceToNextRow());

        return count;
    }

    /// <summary>
    /// Core sequence-based transcoding using a two-pass approach:
    /// first counts data rows to write the correct array header, then transcodes directly into <paramref name="writer"/>.
    /// </summary>
    private static void TranscodeCore<T>(ReadOnlySequence<byte> sequence, ref MessagePackWriter writer, CsvTranscodeOptions options)
    {
        var count = CountDataRows(sequence, options);
        writer.WriteArrayHeader(count);

        if (count == 0) return;

        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        var reader = new CsvReader(sequence, options);
        if (options.HasHeader)
        {
            reader.SkipHeader();
        }

        do
        {
            formatter.Transcode(ref writer, ref reader, options);
        }
        while (reader.TryAdvanceToNextRow());
    }

    /// <summary>
    /// Core <see cref="CsvReader"/>-based transcoding: counts rows via a fresh reader from the same
    /// underlying sequence, then transcodes using the supplied <paramref name="reader"/>.
    /// The supplied reader is advanced through all data rows by this call.
    /// </summary>
    private static void TranscodeCore<T>(ref CsvReader reader, ref MessagePackWriter writer, CsvTranscodeOptions options)
    {
        var count = CountDataRows(reader.Sequence, options);
        writer.WriteArrayHeader(count);

        if (count == 0) return;

        var formatter = options.Resolver.GetFormatterWithVerify<T>();
        if (options.HasHeader)
        {
            reader.SkipHeader();
        }

        do
        {
            formatter.Transcode(ref writer, ref reader, options);
        }
        while (reader.TryAdvanceToNextRow());
    }

    /// <summary>Reads all bytes from <paramref name="stream"/> into a contiguous buffer.</summary>
    private static ReadOnlyMemory<byte> ReadAllBytes(Stream stream)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        return ms.ToArray();
    }

    /// <summary>Asynchronously reads all bytes from <paramref name="stream"/> into a contiguous buffer.</summary>
    private static async ValueTask<ReadOnlyMemory<byte>> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, cancellationToken).ConfigureAwait(false);
        return ms.ToArray();
    }

    #region ReadOnlySequence<byte> inputs

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, IBufferWriter<byte> writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var opts = options ?? new CsvTranscodeOptions();
        var msgWriter = new MessagePackWriter(writer);
        TranscodeCore<T>(byteSequence, ref msgWriter, opts);
        msgWriter.Flush();
    }

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? new CsvTranscodeOptions();
        TranscodeCore<T>(byteSequence, ref writer, opts);
    }

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, Stream stream, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? new CsvTranscodeOptions();
        var bufferWriter = new StreamBufferWriter(stream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(byteSequence, ref msgWriter, opts);
        msgWriter.Flush();
        bufferWriter.Flush();
    }

    /// <summary>Asynchronously transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static async ValueTask ToMessagePackAsync<T>(ReadOnlySequence<byte> byteSequence, Stream stream, CsvTranscodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? new CsvTranscodeOptions();
        var bufferWriter = new StreamBufferWriter(stream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(byteSequence, ref msgWriter, opts);
        msgWriter.Flush();
        await bufferWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion

    #region ref CsvReader inputs

    /// <summary>Transcodes CSV data from <paramref name="reader"/> to MessagePack, writing the result to <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ref CsvReader reader, IBufferWriter<byte> writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var opts = options ?? reader.Options;
        var msgWriter = new MessagePackWriter(writer);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
    }

    /// <summary>Transcodes CSV data from <paramref name="reader"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ref CsvReader reader, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? reader.Options;
        TranscodeCore<T>(ref reader, ref writer, opts);
    }

    /// <summary>Transcodes CSV data from <paramref name="reader"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void ToMessagePack<T>(ref CsvReader reader, Stream stream, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? reader.Options;
        var bufferWriter = new StreamBufferWriter(stream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        bufferWriter.Flush();
    }

    /// <summary>
    /// Transcodes CSV data from <paramref name="reader"/> to MessagePack synchronously, then
    /// asynchronously flushes the result to <paramref name="stream"/>.
    /// </summary>
    /// <remarks>
    /// The transcoding step is always performed synchronously.
    /// Only the final flush to <paramref name="stream"/> is asynchronous.
    /// This overload cannot be declared <see langword="async"/> because <c>ref</c> parameters
    /// are not allowed in async methods; the method returns a <see cref="ValueTask"/> directly.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static ValueTask ToMessagePackAsync<T>(ref CsvReader reader, Stream stream, CsvTranscodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? reader.Options;
        var bufferWriter = new StreamBufferWriter(stream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        return bufferWriter.FlushAsync(cancellationToken);
    }

    #endregion

    #region ReadOnlyMemory<byte> inputs

    /// <summary>Transcodes CSV data in <paramref name="buffer"/> to MessagePack, writing the result to <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlyMemory<byte> buffer, IBufferWriter<byte> writer, CsvTranscodeOptions? options = null)
        => ToMessagePack<T>(new ReadOnlySequence<byte>(buffer), writer, options);

    /// <summary>Transcodes CSV data in <paramref name="buffer"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlyMemory<byte> buffer, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
        => ToMessagePack<T>(new ReadOnlySequence<byte>(buffer), ref writer, options);

    /// <summary>Transcodes CSV data in <paramref name="buffer"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void ToMessagePack<T>(ReadOnlyMemory<byte> buffer, Stream stream, CsvTranscodeOptions? options = null)
        => ToMessagePack<T>(new ReadOnlySequence<byte>(buffer), stream, options);

    /// <summary>Asynchronously transcodes CSV data in <paramref name="buffer"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static ValueTask ToMessagePackAsync<T>(ReadOnlyMemory<byte> buffer, Stream stream, CsvTranscodeOptions? options = null, CancellationToken cancellationToken = default)
        => ToMessagePackAsync<T>(new ReadOnlySequence<byte>(buffer), stream, options, cancellationToken);

    #endregion

    #region Stream inputs

    /// <summary>Transcodes CSV data from <paramref name="inputStream"/> to MessagePack, writing the result to <paramref name="writer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> or <paramref name="writer"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable.</exception>
    public static void ToMessagePack<T>(Stream inputStream, IBufferWriter<byte> writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        if (!inputStream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(inputStream));
        ArgumentNullException.ThrowIfNull(writer);

        var inputBytes = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        var msgWriter = new MessagePackWriter(writer);
        TranscodeCore<T>(new ReadOnlySequence<byte>(inputBytes), ref msgWriter, opts);
        msgWriter.Flush();
    }

    /// <summary>Transcodes CSV data from <paramref name="inputStream"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable.</exception>
    public static void ToMessagePack<T>(Stream inputStream, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        if (!inputStream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(inputStream));

        var inputBytes = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        TranscodeCore<T>(new ReadOnlySequence<byte>(inputBytes), ref writer, opts);
    }

    /// <summary>Transcodes CSV data from <paramref name="inputStream"/> to MessagePack, writing the result to <paramref name="outputStream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> or <paramref name="outputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable, or <paramref name="outputStream"/> is not writable.</exception>
    public static void ToMessagePack<T>(Stream inputStream, Stream outputStream, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        if (!inputStream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(outputStream));

        var inputBytes = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        var bufferWriter = new StreamBufferWriter(outputStream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(new ReadOnlySequence<byte>(inputBytes), ref msgWriter, opts);
        msgWriter.Flush();
        bufferWriter.Flush();
    }

    /// <summary>Asynchronously transcodes CSV data from <paramref name="inputStream"/> to MessagePack, writing the result to <paramref name="outputStream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> or <paramref name="outputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable, or <paramref name="outputStream"/> is not writable.</exception>
    public static async ValueTask ToMessagePackAsync<T>(Stream inputStream, Stream outputStream, CsvTranscodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        if (!inputStream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(inputStream));
        ArgumentNullException.ThrowIfNull(outputStream);
        if (!outputStream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(outputStream));

        var inputBytes = await ReadAllBytesAsync(inputStream, cancellationToken).ConfigureAwait(false);
        var opts = options ?? new CsvTranscodeOptions();
        var bufferWriter = new StreamBufferWriter(outputStream);
        var msgWriter = new MessagePackWriter(bufferWriter);
        TranscodeCore<T>(new ReadOnlySequence<byte>(inputBytes), ref msgWriter, opts);
        msgWriter.Flush();
        await bufferWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
    }

    #endregion
}

/// <summary>
/// An <see cref="IBufferWriter{T}"/> that writes to a <see cref="Stream"/> in chunks.
/// Buffers data internally and flushes to the stream on <see cref="Flush"/> / <see cref="FlushAsync"/>.
/// </summary>
file sealed class StreamBufferWriter(Stream stream) : IBufferWriter<byte>
{
    private const int InitialBufferSize = 65536;

    private byte[] _buffer = new byte[InitialBufferSize];
    private int _written;

    public void Advance(int count) => _written += count;

    public Memory<byte> GetMemory(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsMemory(_written);
    }

    public Span<byte> GetSpan(int sizeHint = 0)
    {
        EnsureCapacity(sizeHint);
        return _buffer.AsSpan(_written);
    }

    private void EnsureCapacity(int sizeHint)
    {
        var needed = _written + Math.Max(sizeHint, 1);
        if (needed <= _buffer.Length) return;

        // Flush accumulated data before growing the buffer.
        stream.Write(_buffer, 0, _written);
        _written = 0;

        if (sizeHint > _buffer.Length)
        {
            _buffer = new byte[Math.Max(sizeHint, _buffer.Length * 2)];
        }
    }

    public void Flush()
    {
        if (_written > 0)
        {
            stream.Write(_buffer, 0, _written);
            _written = 0;
        }
    }

    public ValueTask FlushAsync(CancellationToken cancellationToken = default)
    {
        if (_written == 0) return ValueTask.CompletedTask;
        return FlushAsyncCore(cancellationToken);
    }

    private async ValueTask FlushAsyncCore(CancellationToken cancellationToken)
    {
        await stream.WriteAsync(_buffer.AsMemory(0, _written), cancellationToken).ConfigureAwait(false);
        _written = 0;
    }
}
