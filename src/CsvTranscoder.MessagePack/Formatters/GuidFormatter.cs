using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class GuidFormatter : ICsvFormatter<Guid>
{
    public static readonly GuidFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        if (string.IsNullOrEmpty(str))
        {
            writer.WriteNil();
            return;
        }

        MessagePack.Formatters.GuidFormatter.Instance.Serialize(ref writer, Guid.Parse(str), MessagePackSerializerOptions.Standard);
    }
}
