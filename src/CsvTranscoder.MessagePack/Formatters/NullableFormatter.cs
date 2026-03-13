using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class NullableFormatter<T> : ICsvFormatter<T?>
    where T : struct
{
    public static readonly NullableFormatter<T> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        if (reader.IsNextFieldEmpty())
        {
            writer.WriteNil();
            reader.SkipField();
            return;
        }

        options.Resolver.GetFormatterWithVerify<T>().Transcode(ref writer, ref reader, options);
    }
}
