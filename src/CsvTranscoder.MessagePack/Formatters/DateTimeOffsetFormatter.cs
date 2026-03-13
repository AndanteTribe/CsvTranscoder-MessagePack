using System.Globalization;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeOffsetFormatter : ICsvFormatter<DateTimeOffset>
{
    public static readonly DateTimeOffsetFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        if (string.IsNullOrEmpty(str))
        {
            writer.WriteNil();
            return;
        }

        var value = DateTimeOffset.Parse(str, CultureInfo.InvariantCulture);
        MessagePack.Formatters.DateTimeOffsetFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
