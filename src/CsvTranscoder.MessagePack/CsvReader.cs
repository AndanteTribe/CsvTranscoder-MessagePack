using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace AndanteTribe.Csv;

public ref struct CsvReader
{
    /// <summary>Maximum number of columns that can be tracked as comment columns.</summary>
    private const int MaxCommentColumns = 64;

    private SequenceReader<byte> _reader;
    private readonly CsvTranscodeOptions _options;
    private readonly ReadOnlyMemory<byte> _newLine;
    private readonly byte _separator;
    private ulong _commentColumnMask;
    private int _currentColumn;

    public readonly CsvTranscodeOptions Options => _options;
    public readonly long Consumed => _reader.Consumed;
    public readonly long Remaining => _reader.Remaining;
    public readonly bool End => _reader.End;

    public CsvReader(ReadOnlySequence<byte> sequence, CsvTranscodeOptions options)
    {
        if (string.IsNullOrEmpty(options.NewLine))
        {
            throw new ArgumentException("NewLine must not be null or empty.", nameof(options));
        }

        _reader = new SequenceReader<byte>(sequence);
        _options = options;
        _newLine = Encoding.UTF8.GetBytes(options.NewLine);
        _separator = (byte)options.Separator;
    }

    /// <summary>
    /// Skips the header row when <see cref="CsvTranscodeOptions.HasHeader"/> is <see langword="true"/>.
    /// When <see cref="CsvTranscodeOptions.AllowColumnComments"/> is also <see langword="true"/>,
    /// the header is parsed to identify comment columns (those whose header value starts with <c>#</c>);
    /// those columns are then silently skipped during all subsequent field reads.
    /// </summary>
    public void SkipHeader()
    {
        if (_options.HasHeader)
        {
            if (_options.AllowColumnComments)
            {
                BuildCommentColumnMask();
            }
            else
            {
                SkipRow();
            }

            // Skip any comment rows that immediately follow the header.
            if (_options.AllowRowComments)
            {
                SkipLeadingCommentRows();
            }
        }
    }

    /// <summary>
    /// Reads through the header row and records which column indices start with <c>#</c>
    /// into <see cref="_commentColumnMask"/>. Advances past the header row's newline on exit.
    /// Only the first <see cref="MaxCommentColumns"/> (64) columns can be tracked as comment columns;
    /// columns beyond that index are always treated as data columns.
    /// </summary>
    private void BuildCommentColumnMask()
    {
        var column = 0;
        var newLine = _newLine.Span;

        while (!_reader.End)
        {
            // Check for the full newline sequence (handles multi-byte newlines like \r\n correctly).
            if (_reader.IsNext(newLine, advancePast: false))
            {
                _reader.Advance(newLine.Length);
                break;
            }

            var isComment = _reader.IsNext((byte)'#', advancePast: false);
            ReadFieldRaw(out _);

            if (isComment && column < MaxCommentColumns)
            {
                _commentColumnMask |= 1UL << column;
            }

            column++;
        }
    }

    /// <summary>
    /// Skips any rows starting with <c>#</c> from the current position.
    /// Used both after <see cref="SkipHeader"/> and inside <see cref="TryAdvanceToNextRow"/>.
    /// </summary>
    private void SkipLeadingCommentRows()
    {
        while (!_reader.End && _reader.IsNext((byte)'#', advancePast: false))
        {
            SkipToEndOfRow();
        }
    }

    /// <summary>
    /// Advances past the next occurrence of the configured newline sequence.
    /// Correctly handles multi-byte newlines (e.g. <c>\r\n</c>): a bare occurrence of the
    /// first byte that is not followed by the rest of the sequence is treated as data and skipped.
    /// </summary>
    private void SkipToEndOfRow()
    {
        var newLine = _newLine.Span;

        if (newLine.Length == 1)
        {
            // Fast path for single-byte newlines (e.g. '\n').
            if (!_reader.TryAdvanceTo(newLine[0], advancePastDelimiter: true))
            {
                _reader.Advance(_reader.Remaining);
            }

            return;
        }

        // Multi-byte newline: scan for the first byte, then verify the full sequence.
        while (!_reader.End)
        {
            if (!_reader.TryAdvanceTo(newLine[0], advancePastDelimiter: false))
            {
                _reader.Advance(_reader.Remaining);
                return;
            }

            if (_reader.IsNext(newLine, advancePast: true))
            {
                return; // consumed the full newline sequence
            }

            // Bare occurrence of the first byte that is not a real newline — skip it.
            _reader.Advance(1);
        }
    }

    /// <summary>
    /// Advances past the current row's newline and positions the reader at the start of the next row.
    /// Comment rows (lines starting with <c>#</c>) are skipped automatically when
    /// <see cref="CsvTranscodeOptions.AllowRowComments"/> is <see langword="true"/>.
    /// </summary>
    /// <returns><see langword="true"/> if there is more data to read; otherwise <see langword="false"/>.</returns>
    public bool TryAdvanceToNextRow()
    {
        _currentColumn = 0;
        SkipToEndOfRow();

        if (_options.AllowRowComments)
        {
            SkipLeadingCommentRows();
        }

        return !_reader.End;
    }

    /// <summary>Skips the remainder of the current row including its newline sequence.</summary>
    public void SkipRow() => SkipToEndOfRow();

    /// <summary>Reads and discards the current field.</summary>
    public void SkipField() => TryReadField(out _);

    /// <summary>
    /// Reads the next logical field, automatically skipping any comment columns
    /// (as identified during <see cref="SkipHeader"/> when
    /// <see cref="CsvTranscodeOptions.AllowColumnComments"/> is <see langword="true"/>).
    /// </summary>
    private bool TryReadField(out ReadOnlySequence<byte> field)
    {
        while (true)
        {
            if (_reader.End)
            {
                field = default;
                return false;
            }

            var col = _currentColumn++;
            var isCommentCol = _options.AllowColumnComments && _commentColumnMask != 0 && col < MaxCommentColumns && (_commentColumnMask & (1UL << col)) != 0;

            if (isCommentCol)
            {
                ReadFieldRaw(out _);
                continue;
            }

            return ReadFieldRaw(out field);
        }
    }

    private bool ReadFieldRaw(out ReadOnlySequence<byte> field)
    {
        if (_reader.End)
        {
            field = default;
            return false;
        }

        var quote = _options.Quote;

        // Quote.All: every field must be quoted; throw if opening '"' is absent.
        if (quote == Quote.All)
        {
            if (!_reader.IsNext((byte)'"', advancePast: false))
            {
                ThrowExpectedQuoteException();
            }

            return TryReadQuotedField(out field);
        }

        // Quote.Minimal / Quote.NoneNumeric: strip quotes when a field starts with '"'.
        if (quote != Quote.None && _reader.IsNext((byte)'"', advancePast: false))
        {
            return TryReadQuotedField(out field);
        }

        var newLine = _newLine.Span;

        // Fast path for single-byte newlines (e.g. '\n').
        if (newLine.Length == 1)
        {
#if NET5_0_OR_GREATER
            var terminators = (Span<byte>)stackalloc byte[] { _separator, newLine[0] };
#else
            ReadOnlySpan<byte> terminators = new byte[] { _separator, newLine[0] };
#endif
            if (_reader.TryReadToAny(out field, terminators, advancePastDelimiter: false))
            {
                // Consume the separator if that is what we stopped at; leave a newline for TryAdvanceToNextRow.
                _reader.IsNext(_separator, advancePast: true);
                return true;
            }

            // No terminator — read to end of sequence.
            field = _reader.Sequence.Slice(_reader.Position);
            _reader.Advance(_reader.Remaining);
            return true;
        }

        // Multi-byte newline path (e.g. '\r\n'): scan for separator or full newline sequence.
        // We track where the field started so that a Sequence.Slice captures all data including
        // any bare occurrences of the first newline byte that turned out not to be a real newline.
        var start = _reader.Position;
#if NET5_0_OR_GREATER
        var firstByteTerminators = (Span<byte>)[_separator, newLine[0]];
#else
        ReadOnlySpan<byte> firstByteTerminators = new byte[] { _separator, newLine[0] };
#endif

        while (true)
        {
            if (!_reader.TryReadToAny(out ReadOnlySequence<byte> _, firstByteTerminators, advancePastDelimiter: false))
            {
                // No separator or newline first-byte found; the rest of the data is all field content.
                field = _reader.Sequence.Slice(start);
                _reader.Advance(_reader.Remaining);
                return true;
            }

            // Stopped at the field separator.
            if (_reader.IsNext(_separator, advancePast: false))
            {
                field = _reader.Sequence.Slice(start, _reader.Position);
                _reader.Advance(1); // consume separator
                return true;
            }

            // Stopped at the first byte of the newline sequence. Verify the full sequence.
            if (_reader.IsNext(newLine, advancePast: false))
            {
                // Full newline found — field ends here; leave the newline for TryAdvanceToNextRow.
                field = _reader.Sequence.Slice(start, _reader.Position);
                return true;
            }

            // Bare first-byte-of-newline inside field data (e.g. a bare '\r'). Advance past it and
            // continue scanning. The byte will be included in the final Slice because 'start' is fixed.
            _reader.Advance(1);
        }
    }

    private bool TryReadQuotedField(out ReadOnlySequence<byte> field)
    {
        _reader.Advance(1); // skip opening '"'

        if (!_reader.TryReadTo(out field, (byte)'"', advancePastDelimiter: true))
        {
            // Malformed — no closing quote; treat rest as field value.
            field = _reader.Sequence.Slice(_reader.Position);
            _reader.Advance(_reader.Remaining);
            return true;
        }

        // Skip the trailing field separator if present.
        if (!_reader.End)
        {
            _reader.IsNext(_separator, advancePast: true);
        }

        return true;
    }

    /// <summary>Reads the current field and parses it as <see cref="bool"/>.</summary>
    public bool ReadBoolean()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[8]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out bool value, out _))
        {
            return value;
        }

        if (span.Length == 1)
        {
            if (span[0] == (byte)'1')
            {
                return true;
            }

            if (span[0] == (byte)'0')
            {
                return false;
            }
        }

        return ThrowFormatException<bool>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="byte"/>.</summary>
    public byte ReadByte()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[8]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out byte value, out _))
        {
            return value;
        }

        return ThrowFormatException<byte>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="sbyte"/>.</summary>
    public sbyte ReadSByte()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[8]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out sbyte value, out _))
        {
            return value;
        }

        return ThrowFormatException<sbyte>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="short"/>.</summary>
    public short ReadInt16()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[8]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out short value, out _))
        {
            return value;
        }

        return ThrowFormatException<short>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="ushort"/>.</summary>
    public ushort ReadUInt16()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[8]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out ushort value, out _))
        {
            return value;
        }

        return ThrowFormatException<ushort>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="int"/>.</summary>
    public int ReadInt32()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[16]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out int value, out _))
        {
            return value;
        }

        return ThrowFormatException<int>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="uint"/>.</summary>
    public uint ReadUInt32()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[16]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out uint value, out _))
        {
            return value;
        }

        return ThrowFormatException<uint>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="long"/>.</summary>
    public long ReadInt64()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[24]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out long value, out _))
        {
            return value;
        }

        return ThrowFormatException<long>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="ulong"/>.</summary>
    public ulong ReadUInt64()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[24]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out ulong value, out _))
        {
            return value;
        }

        return ThrowFormatException<ulong>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="float"/>.</summary>
    public float ReadSingle()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out float value, out _))
        {
            return value;
        }

        return ThrowFormatException<float>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="double"/>.</summary>
    public double ReadDouble()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out double value, out _))
        {
            return value;
        }

        return ThrowFormatException<double>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="decimal"/>.</summary>
    public decimal ReadDecimal()
    {
        TryReadField(out var field);
        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out decimal value, out _))
        {
            return value;
        }

        return ThrowFormatException<decimal>(span);
    }

    /// <summary>Reads the current field and returns the first <see cref="char"/> of its UTF-8 decoded value.</summary>
    public char ReadChar()
    {
        TryReadField(out var field);

        if (field.IsEmpty)
        {
            ThrowEmptyFieldException<char>();
        }

        var temp = (Span<char>)stackalloc char[Encoding.UTF8.GetCharCount(field.FirstSpan)];
        Encoding.UTF8.GetChars(field.FirstSpan, temp);
        return temp[0];
    }

    /// <summary>
    /// Returns <see langword="true"/> if the next field is empty (i.e., zero-length value),
    /// without consuming any data from the reader.
    /// Handles both unquoted empty fields (adjacent separators or end-of-row) and
    /// quoted empty fields (<c>""</c>).
    /// </summary>
    public bool IsNextFieldEmpty()
    {
        if (_reader.End)
        {
            return true;
        }

        if (!_reader.TryPeek(out var b))
        {
            return true;
        }

        // Unquoted empty field: the very next byte is a separator or the start of a newline.
        if (b == _separator)
        {
            return true;
        }

        var newLineSpan = _newLine.Span;
        if (newLineSpan.Length > 0 && _reader.IsNext(newLineSpan, advancePast: false))
        {
            return true;
        }

        // Quoted empty field: "".
        if (b == (byte)'"' && _options.Quote != Quote.None)
        {
#if NET5_0_OR_GREATER
            if (_reader.TryPeek(1, out var second))
            {
                return second == (byte)'"';
            }
#else
            var remaining = _reader.Sequence.Slice(_reader.Position);
            if (remaining.Length >= 2)
            {
                return remaining.Slice(1, 1).FirstSpan[0] == (byte)'"';
            }
#endif
        }

        return false;
    }

    /// <summary>Reads the current field and returns it as a UTF-8 decoded <see cref="string"/>.</summary>
    public string ReadString()
    {
        TryReadField(out var field);

        if (field.IsEmpty)
        {
            return string.Empty;
        }

        if (field.IsSingleSegment)
        {
            return Encoding.UTF8.GetString(field.FirstSpan);
        }

        return Encoding.UTF8.GetString(field.ToArray());
    }

    /// <summary>
    /// Reads the current field and returns the raw UTF-8 bytes without any decoding.
    /// The caller is responsible for interpreting the byte sequence (e.g. using
    /// <see cref="System.Buffers.Text.Utf8Parser"/> or <see cref="Encoding.UTF8"/>).
    /// </summary>
    public ReadOnlySequence<byte> ReadRaw()
    {
        TryReadField(out var field);
        return field;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ThrowFormatException<T>(ReadOnlySpan<byte> span)
        => throw new FormatException($"Cannot parse '{Encoding.UTF8.GetString(span)}' as {typeof(T).Name}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEmptyFieldException<T>()
        => throw new FormatException($"Cannot parse empty field as {typeof(T).Name}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowExpectedQuoteException()
        => throw new FormatException("Expected a quoted field (Quote.All requires all fields to be quoted).");
}

/// <summary>
/// Provides a <see cref="ReadOnlySpan{T}"/> view over a CSV field, using a stack-allocated buffer
/// for small multi-segment sequences and a pooled array for larger ones.
/// Must be disposed to return any pooled array to <see cref="ArrayPool{T}.Shared"/>.
/// </summary>
internal ref struct FieldSpanOwner
{
    private byte[]? _rented;

    public ReadOnlySpan<byte> Span { get; }

    public FieldSpanOwner(in ReadOnlySequence<byte> field, Span<byte> stackBuffer)
    {
        if (field.IsEmpty)
        {
            Span = default;
            return;
        }

        if (field.IsSingleSegment)
        {
            Span = field.FirstSpan;
            return;
        }

        var length = (int)field.Length;
        if (length <= stackBuffer.Length)
        {
            field.CopyTo(stackBuffer);
            Span = stackBuffer[..length];
            return;
        }

        // Field is larger than the stack buffer; fall back to a pooled array.
        _rented = ArrayPool<byte>.Shared.Rent(length);
        field.CopyTo(_rented);
        Span = _rented.AsSpan(0, length);
    }

    public void Dispose()
    {
        if (_rented != null)
        {
            ArrayPool<byte>.Shared.Return(_rented);
            _rented = null;
        }
    }
}
