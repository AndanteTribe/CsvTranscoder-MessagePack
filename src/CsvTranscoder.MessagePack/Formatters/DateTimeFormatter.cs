using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class DateTimeFormatter : ICsvFormatter<DateTime>
{
    public static readonly DateTimeFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadDateTime());
}
