using System.Buffers;
using System.Globalization;
using System.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class TimeSpanFormatter : ICsvFormatter<TimeSpan>
{
    public static readonly TimeSpanFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        var field = reader.ReadRaw();
        if (field.IsEmpty)
            throw new FormatException("Cannot parse empty field as TimeSpan.");

        using var owner = new FieldSpanOwner(in field, stackalloc byte[32]);
        var span = owner.Span;

        // Use a fixed 64-char stack buffer; rent from ArrayPool for unusually large fields.
        Span<char> charBuf = stackalloc char[64];
        TimeSpan value;
        var charCount = Encoding.UTF8.GetCharCount(span);
        if (charCount <= 64 && Encoding.UTF8.TryGetChars(span, charBuf, out var written))
        {
            value = TimeSpan.Parse(charBuf[..written], CultureInfo.InvariantCulture);
        }
        else
        {
            var pooled = ArrayPool<char>.Shared.Rent(charCount);
            try
            {
                written = Encoding.UTF8.GetChars(span, pooled);
                value = TimeSpan.Parse(pooled.AsSpan(0, written), CultureInfo.InvariantCulture);
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pooled);
            }
        }

        MessagePack.Formatters.TimeSpanFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
