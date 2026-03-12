using System.Buffers;
using System.Buffers.Text;
using System.Runtime.CompilerServices;
using System.Text;

namespace AndanteTribe.Csv;

public ref struct CsvReader
{
    private SequenceReader<byte> _reader;
    private readonly CsvTranscodeOptions _options;
    private readonly ReadOnlyMemory<byte> _newLine;
    private readonly byte _separator;

    public readonly CsvTranscodeOptions Options => _options;
    public readonly long Consumed => _reader.Consumed;
    public readonly long Remaining => _reader.Remaining;
    public readonly bool End => _reader.End;

    public CsvReader(ReadOnlySequence<byte> sequence, CsvTranscodeOptions options)
    {
        _reader = new SequenceReader<byte>(sequence);
        _options = options;
        _newLine = Encoding.UTF8.GetBytes(options.NewLine);
        _separator = (byte)options.Separator;
    }

    /// <summary>
    /// Skips the header row when <see cref="CsvTranscodeOptions.HasHeader"/> is <see langword="true"/>.
    /// </summary>
    public void SkipHeader()
    {
        if (_options.HasHeader)
        {
            SkipRow();
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
        var newLine = _newLine.Span;
        if (!_reader.TryAdvanceTo(newLine[0], advancePastDelimiter: false))
        {
            _reader.AdvanceToEnd();
            return false;
        }

        _reader.Advance(newLine.Length);

        if (_options.AllowRowComments)
        {
            while (!_reader.End && _reader.IsNext((byte)'#', advancePast: false))
            {
                if (!_reader.TryAdvanceTo(newLine[0], advancePastDelimiter: false))
                {
                    _reader.AdvanceToEnd();
                    return false;
                }

                _reader.Advance(newLine.Length);
            }
        }

        return !_reader.End;
    }

    /// <summary>Skips the remainder of the current row including its newline sequence.</summary>
    public void SkipRow()
    {
        var newLine = _newLine.Span;
        if (!_reader.TryAdvanceTo(newLine[0], advancePastDelimiter: false))
        {
            _reader.AdvanceToEnd();
            return;
        }

        _reader.Advance(newLine.Length);
    }

    /// <summary>Reads and discards the current field.</summary>
    public void SkipField() => TryReadField(out _);

    private bool TryReadField(out ReadOnlySequence<byte> field)
    {
        if (_reader.End)
        {
            field = default;
            return false;
        }

        if (_options.Quote != Quote.None && _reader.IsNext((byte)'"', advancePast: false))
        {
            return TryReadQuotedField(out field);
        }

        var newLine = _newLine.Span;
        Span<byte> terminators = stackalloc byte[] { _separator, newLine[0] };

        if (_reader.TryReadToAny(out field, terminators, advancePastDelimiter: false))
        {
            // Consume the separator if that is what we stopped at; leave a newline for TryAdvanceToNextRow.
            if (!_reader.End && _reader.IsNext(_separator, advancePast: true))
            {
                // separator consumed
            }

            return true;
        }

        // No terminator — read to end of sequence.
        field = _reader.UnreadSequence;
        _reader.AdvanceToEnd();
        return true;
    }

    private bool TryReadQuotedField(out ReadOnlySequence<byte> field)
    {
        _reader.Advance(1); // skip opening '"'

        if (!_reader.TryReadTo(out field, (byte)'"', advancePastDelimiter: true))
        {
            // Malformed — no closing quote; treat rest as field value.
            field = _reader.UnreadSequence;
            _reader.AdvanceToEnd();
            return true;
        }

        // Skip the trailing field separator if present.
        if (!_reader.End)
        {
            _reader.IsNext(_separator, advancePast: true);
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ReadOnlySpan<byte> GetFieldSpan(in ReadOnlySequence<byte> field, Span<byte> buffer)
    {
        if (field.IsSingleSegment)
        {
            return field.FirstSpan;
        }

        field.CopyTo(buffer);
        return buffer[..(int)field.Length];
    }

    /// <summary>Reads the current field and parses it as <see cref="bool"/>.</summary>
    public bool ReadBoolean()
    {
        TryReadField(out var field);
        Span<byte> buffer = stackalloc byte[8];
        var span = GetFieldSpan(in field, buffer);

        if (Utf8Parser.TryParse(span, out bool value, out _))
        {
            return value;
        }

        if (span.Length == 1)
        {
            if (span[0] == (byte)'1') return true;
            if (span[0] == (byte)'0') return false;
        }

        return ThrowFormatException<bool>(span);
    }

    /// <summary>Reads the current field and parses it as <see cref="byte"/>.</summary>
    public byte ReadByte()
    {
        TryReadField(out var field);
        Span<byte> buffer = stackalloc byte[8];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[8];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[8];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[8];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[16];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[16];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[24];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[24];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[32];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[32];
        var span = GetFieldSpan(in field, buffer);

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
        Span<byte> buffer = stackalloc byte[32];
        var span = GetFieldSpan(in field, buffer);

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

        if (field.IsSingleSegment)
        {
            return Encoding.UTF8.GetString(field.FirstSpan)[0];
        }

        return Encoding.UTF8.GetString(field.ToArray())[0];
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

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static T ThrowFormatException<T>(ReadOnlySpan<byte> span)
        => throw new FormatException($"Cannot parse '{Encoding.UTF8.GetString(span)}' as {typeof(T).Name}.");

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowEmptyFieldException<T>()
        => throw new FormatException($"Cannot parse empty field as {typeof(T).Name}.");
}