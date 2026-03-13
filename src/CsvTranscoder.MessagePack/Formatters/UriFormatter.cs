using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class UriFormatter : ICsvFormatter<Uri?>
{
    public static readonly UriFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
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
