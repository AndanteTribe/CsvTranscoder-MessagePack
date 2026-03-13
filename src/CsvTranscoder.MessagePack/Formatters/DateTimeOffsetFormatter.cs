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
            throw new FormatException("Cannot parse empty field as DateTimeOffset.");

        using var owner = new FieldSpanOwner(in field, stackalloc byte[64]);
        var span = owner.Span;

        // Use a fixed 64-char stack buffer; fall back to string allocation for unusually large fields.
        Span<char> charBuf = stackalloc char[64];
        DateTimeOffset value;
        if (Encoding.UTF8.GetCharCount(span) <= 64 && Encoding.UTF8.TryGetChars(span, charBuf, out var written))
        {
            value = DateTimeOffset.Parse(charBuf[..written], CultureInfo.InvariantCulture);
        }
        else
        {
            value = DateTimeOffset.Parse(Encoding.UTF8.GetString(span), CultureInfo.InvariantCulture);
        }

        MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
