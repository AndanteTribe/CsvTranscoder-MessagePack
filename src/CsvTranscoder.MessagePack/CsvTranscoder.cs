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
    /// Core transcoding loop: optionally skips the header, then calls the formatter for each
    /// data row and assembles the results into a MessagePack array written to <paramref name="writer"/>.
    /// </summary>
    private static void TranscodeCore<T>(ref CsvReader reader, ref MessagePackWriter writer, CsvTranscodeOptions options)
    {
        var formatter = options.Resolver.GetFormatterWithVerify<T>();

        if (options.HasHeader)
        {
            reader.SkipHeader();
        }

        // Buffer all row payloads so we know the element count before writing the array header.
        var bodyBuffer = new ArrayBufferWriter<byte>();
        var bodyWriter = new MessagePackWriter(bodyBuffer);
        var count = 0;

        if (!reader.End)
        {
            do
            {
                formatter.Transcode(ref bodyWriter, ref reader, options);
                count++;
            }
            while (reader.TryAdvanceToNextRow());
        }

        bodyWriter.Flush();

        writer.WriteArrayHeader(count);
        writer.WriteRaw(bodyBuffer.WrittenSpan);
    }

    /// <summary>Reads all bytes from <paramref name="stream"/> into an <see cref="ArrayBufferWriter{T}"/>.</summary>
    private static ArrayBufferWriter<byte> ReadAllBytes(Stream stream)
    {
        var buffer = new ArrayBufferWriter<byte>();
        const int chunkSize = 4096;
        while (true)
        {
            var memory = buffer.GetMemory(chunkSize);
            var read = stream.Read(memory.Span);
            if (read == 0) break;
            buffer.Advance(read);
        }

        return buffer;
    }

    /// <summary>Asynchronously reads all bytes from <paramref name="stream"/> into an <see cref="ArrayBufferWriter{T}"/>.</summary>
    private static async ValueTask<ArrayBufferWriter<byte>> ReadAllBytesAsync(Stream stream, CancellationToken cancellationToken)
    {
        var buffer = new ArrayBufferWriter<byte>();
        const int chunkSize = 4096;
        while (true)
        {
            var memory = buffer.GetMemory(chunkSize);
            var read = await stream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
            if (read == 0) break;
            buffer.Advance(read);
        }

        return buffer;
    }

    #region ReadOnlySequence<byte> inputs

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, IBufferWriter<byte> writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(writer);
        var opts = options ?? new CsvTranscodeOptions();
        var msgWriter = new MessagePackWriter(writer);
        var reader = new CsvReader(byteSequence, opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
    }

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
    {
        var opts = options ?? new CsvTranscodeOptions();
        var reader = new CsvReader(byteSequence, opts);
        TranscodeCore<T>(ref reader, ref writer, opts);
    }

    /// <summary>Transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static void ToMessagePack<T>(ReadOnlySequence<byte> byteSequence, Stream stream, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? new CsvTranscodeOptions();
        var buffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(buffer);
        var reader = new CsvReader(byteSequence, opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        stream.Write(buffer.WrittenSpan);
    }

    /// <summary>Asynchronously transcodes CSV data in <paramref name="byteSequence"/> to MessagePack, writing the result to <paramref name="stream"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="stream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="stream"/> is not writable.</exception>
    public static async ValueTask ToMessagePackAsync<T>(ReadOnlySequence<byte> byteSequence, Stream stream, CsvTranscodeOptions? options = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        if (!stream.CanWrite) throw new ArgumentException("Stream must be writable.", nameof(stream));

        var opts = options ?? new CsvTranscodeOptions();
        var buffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(buffer);
        var reader = new CsvReader(byteSequence, opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        await stream.WriteAsync(buffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
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
        var buffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(buffer);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        stream.Write(buffer.WrittenSpan);
    }

    /// <summary>
    /// Transcodes CSV data from <paramref name="reader"/> to MessagePack synchronously, then
    /// asynchronously writes the result to <paramref name="stream"/>.
    /// </summary>
    /// <remarks>
    /// The transcoding step is always performed synchronously.
    /// Only the final write to <paramref name="stream"/> is asynchronous.
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
        var buffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(buffer);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        return stream.WriteAsync(buffer.WrittenMemory, cancellationToken);
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

        var inputBuffer = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        var msgWriter = new MessagePackWriter(writer);
        var reader = new CsvReader(new ReadOnlySequence<byte>(inputBuffer.WrittenMemory), opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
    }

    /// <summary>Transcodes CSV data from <paramref name="inputStream"/> to MessagePack, writing the result via <paramref name="writer"/>.</summary>
    /// <exception cref="ArgumentNullException"><paramref name="inputStream"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="inputStream"/> is not readable.</exception>
    public static void ToMessagePack<T>(Stream inputStream, ref MessagePackWriter writer, CsvTranscodeOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(inputStream);
        if (!inputStream.CanRead) throw new ArgumentException("Stream must be readable.", nameof(inputStream));

        var inputBuffer = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        var reader = new CsvReader(new ReadOnlySequence<byte>(inputBuffer.WrittenMemory), opts);
        TranscodeCore<T>(ref reader, ref writer, opts);
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

        var inputBuffer = ReadAllBytes(inputStream);
        var opts = options ?? new CsvTranscodeOptions();
        var outputBuffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(outputBuffer);
        var reader = new CsvReader(new ReadOnlySequence<byte>(inputBuffer.WrittenMemory), opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        outputStream.Write(outputBuffer.WrittenSpan);
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

        var inputBuffer = await ReadAllBytesAsync(inputStream, cancellationToken).ConfigureAwait(false);
        var opts = options ?? new CsvTranscodeOptions();
        var outputBuffer = new ArrayBufferWriter<byte>();
        var msgWriter = new MessagePackWriter(outputBuffer);
        var reader = new CsvReader(new ReadOnlySequence<byte>(inputBuffer.WrittenMemory), opts);
        TranscodeCore<T>(ref reader, ref msgWriter, opts);
        msgWriter.Flush();
        await outputStream.WriteAsync(outputBuffer.WrittenMemory, cancellationToken).ConfigureAwait(false);
    }

    #endregion
}
