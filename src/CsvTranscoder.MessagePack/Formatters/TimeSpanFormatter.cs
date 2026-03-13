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
            throw new FormatException("Cannot parse empty field as TimeSpan.");

        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;

        // Use a fixed 64-char stack buffer; fall back to string allocation for unusually large fields.
        Span<char> charBuf = stackalloc char[64];
        TimeSpan value;
        if (Encoding.UTF8.GetCharCount(span) <= 64 && Encoding.UTF8.TryGetChars(span, charBuf, out var written))
        {
            value = TimeSpan.Parse(charBuf[..written], CultureInfo.InvariantCulture);
        }
        else
        {
            value = TimeSpan.Parse(Encoding.UTF8.GetString(span), CultureInfo.InvariantCulture);
        }

        MessagePack.Formatters.TimeSpanFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
