using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class GuidFormatter : ICsvFormatter<Guid>
{
    public static readonly GuidFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        Span<char> buf = stackalloc char[40];
        var overflow = reader.ReadChars(buf, out var len);
        if (len == 0)
        {
            writer.WriteNil();
            return;
        }

        ReadOnlySpan<char> chars = overflow is null ? buf[..len] : overflow.AsSpan();
        MessagePack.Formatters.GuidFormatter.Instance.Serialize(ref writer, Guid.Parse(chars), MessagePackSerializerOptions.Standard);
    }
}
