using System.Globalization;
using System.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class TimeSpanFormatter : ICsvFormatter<TimeSpan>
{
    public static readonly TimeSpanFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var field = reader.ReadRaw();
        if (field.IsEmpty)
        {
            writer.WriteNil();
            return;
        }

        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;
        Span<char> chars = stackalloc char[Encoding.UTF8.GetCharCount(span)];
        Encoding.UTF8.TryGetChars(span, chars, out _);
        var value = TimeSpan.Parse(chars, CultureInfo.InvariantCulture);
        MessagePack.Formatters.TimeSpanFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
