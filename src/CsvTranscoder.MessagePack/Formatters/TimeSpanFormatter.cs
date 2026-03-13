using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class TimeSpanFormatter : ICsvFormatter<TimeSpan>
{
    public static readonly TimeSpanFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        if (string.IsNullOrEmpty(str))
        {
            writer.WriteNil();
            return;
        }

        var value = TimeSpan.Parse(str, System.Globalization.CultureInfo.InvariantCulture);
        MessagePack.Formatters.TimeSpanFormatter.Instance.Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
    }
}
