using System.Buffers;

namespace AndanteTribe.Csv;

public ref struct CsvReader
{
    private readonly SequenceReader<byte> _reader;
    private readonly CsvTranscodeOptions _options;
    private readonly ReadOnlyMemory<byte> _newLine;
    private readonly byte _separator;

    public readonly CsvTranscodeOptions Options => _options;
    public readonly long Consumed => _reader.Consumed;
    public readonly long Remaining => _reader.Remaining;

    public CsvReader(ReadOnlySequence<byte> sequence, CsvTranscodeOptions options)
    {
        _reader = new SequenceReader<byte>(sequence);
        _options = options;
        _newLine = System.Text.Encoding.UTF8.GetBytes(options.NewLine);
        _separator = (byte)options.Separator;
    }
}