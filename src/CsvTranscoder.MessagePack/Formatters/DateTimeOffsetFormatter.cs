using System.Globalization;
using System.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeOffsetFormatter : ICsvFormatter<DateTimeOffset>
{
    public static readonly DateTimeOffsetFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var field = reader.ReadRaw();
        if (field.IsEmpty)
        {
            writer.WriteNil();
            return;
        }

        using var owner = new FieldSpanOwner(in field, stackalloc byte[64]);
        var span = owner.Span;
        Span<char> chars = stackalloc char[Encoding.UTF8.GetCharCount(span)];
        Encoding.UTF8.TryGetChars(span, chars, out _);
        var value = DateTimeOffset.Parse(chars, CultureInfo.InvariantCulture);
        MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
