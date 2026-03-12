using MessagePack;

namespace AndanteTribe.Csv;

public interface ICsvFormatter<T>
{
    void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options);
}