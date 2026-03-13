using System.Buffers;
using System.Globalization;
using System.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeOffsetFormatter : ICsvFormatter<DateTimeOffset>
{
    public static readonly DateTimeOffsetFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        var field = reader.ReadRaw();
        if (field.IsEmpty)
        {
            throw new FormatException("Cannot parse empty field as DateTimeOffset.");
        }

        using var owner = new FieldSpanOwner(in field, stackalloc byte[64]);
        var span = owner.Span;

        // Use a fixed 64-char stack buffer; rent from ArrayPool for unusually large fields.
        DateTimeOffset value;
        var charCount = Encoding.UTF8.GetCharCount(span);
        if (charCount <= 64)
        {
            var charBuf = (Span<char>)stackalloc char[64];
            var written = Encoding.UTF8.GetChars(span, charBuf);
            value = DateTimeOffset.Parse(charBuf[..written], CultureInfo.InvariantCulture);
        }
        else
        {
            var pooled = ArrayPool<char>.Shared.Rent(charCount);
            try
            {
                var written = Encoding.UTF8.GetChars(span, pooled);
                value = DateTimeOffset.Parse(pooled.AsSpan(0, written), CultureInfo.InvariantCulture);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pooled);
            }
        }

        MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
