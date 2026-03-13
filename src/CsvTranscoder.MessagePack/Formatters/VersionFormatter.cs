using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class VersionFormatter : ICsvFormatter<Version?>
{
    public static readonly VersionFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        if (string.IsNullOrEmpty(str))
        {
            writer.WriteNil();
            return;
        }

        writer.Write(str);
    }
}
