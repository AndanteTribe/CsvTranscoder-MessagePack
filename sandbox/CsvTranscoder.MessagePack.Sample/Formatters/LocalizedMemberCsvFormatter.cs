using AndanteTribe.Csv;
using MessagePack;

namespace CsvTranscoder.MessagePack.Sample.Formatters;

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see cref="string"/> members where two consecutive CSV
/// columns hold the Japanese and English values respectively.
/// This formatter reads the <b>Japanese</b> (first) column and skips the English (second) column.
/// </summary>
public sealed class LocalizedMemberJapaneseCsvFormatter : ICsvFormatter<string>
{
    public static readonly LocalizedMemberJapaneseCsvFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        var value = reader.ReadString();
        reader.SkipField(); // skip the English column
        writer.Write(value);
    }
}

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see cref="string"/> members where two consecutive CSV
/// columns hold the Japanese and English values respectively.
/// This formatter skips the Japanese (first) column and reads the <b>English</b> (second) column.
/// </summary>
public sealed class LocalizedMemberEnglishCsvFormatter : ICsvFormatter<string>
{
    public static readonly LocalizedMemberEnglishCsvFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        reader.SkipField(); // skip the Japanese column
        var value = reader.ReadString();
        writer.Write(value);
    }
}
