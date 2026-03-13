using System.Buffers;
using System.Buffers.Text;
using System.Globalization;
using System.Text;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeFormatter : ICsvFormatter<DateTime>
{
    public static readonly DateTimeFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var field = reader.ReadRaw();
        using var owner = new FieldSpanOwner(in field, stackalloc byte[64]);
        var span = owner.Span;

        if (Utf8Parser.TryParse(span, out DateTime value, out _, standardFormat: 'O'))
        {
            writer.Write(value);
            return;
        }

        if (Utf8Parser.TryParse(span, out value, out _, standardFormat: 'R'))
        {
            writer.Write(value);
            return;
        }

        // Fall back to char-based parsing for formats not handled by Utf8Parser (e.g. "yyyy/MM/dd HH:mm:ss").
        // Use a fixed 64-char stack buffer; rent from ArrayPool for unusually large fields.
        Span<char> text = stackalloc char[64];
        var charCount = Encoding.UTF8.GetCharCount(span);
        if (charCount <= 64 && Encoding.UTF8.TryGetChars(span, text, out var written))
        {
            if (DateTime.TryParse(text[..written], CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
            {
                writer.Write(value);
                return;
            }
        }
        else
        {
            var pooled = ArrayPool<char>.Shared.Rent(charCount);
            try
            {
                written = Encoding.UTF8.GetChars(span, pooled);
                if (DateTime.TryParse(pooled.AsSpan(0, written), CultureInfo.InvariantCulture, DateTimeStyles.None, out value))
                {
                    writer.Write(value);
                    return;
                }
            }
            finally
            {
                ArrayPool<char>.Shared.Return(pooled);
            }
        }

        throw new FormatException($"Cannot parse '{Encoding.UTF8.GetString(span)}' as DateTime.");
    }
}
