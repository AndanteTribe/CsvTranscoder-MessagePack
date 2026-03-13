using System.Buffers.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class GuidFormatter : ICsvFormatter<Guid>
{
    public static readonly GuidFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var field = reader.ReadRaw();
        if (field.IsEmpty)
            throw new FormatException("Cannot parse empty field as Guid.");

        using var owner = new FieldSpanOwner(in field, stackalloc byte[40]);
        var span = owner.Span;
        if (!Utf8Parser.TryParse(span, out Guid value, out _))
        {
            throw new FormatException($"Cannot parse '{System.Text.Encoding.UTF8.GetString(span)}' as Guid.");
        }

        MessagePack.Formatters.GuidFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
