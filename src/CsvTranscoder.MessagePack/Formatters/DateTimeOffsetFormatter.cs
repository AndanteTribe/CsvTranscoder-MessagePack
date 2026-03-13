using System.Globalization;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeOffsetFormatter : ICsvFormatter<DateTimeOffset>
{
    public static readonly DateTimeOffsetFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        Span<char> buf = stackalloc char[64];
        var overflow = reader.ReadChars(buf, out var len);
        if (len == 0)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpan<char> chars = overflow is null ? buf[..len] : overflow.AsSpan();
        var value = DateTimeOffset.Parse(chars, CultureInfo.InvariantCulture);
        MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
