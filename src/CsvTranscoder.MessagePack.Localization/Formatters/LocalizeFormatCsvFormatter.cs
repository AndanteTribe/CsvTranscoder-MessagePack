using Localization;
using MessagePack;
using MessagePack.Formatters;

namespace AndanteTribe.Csv.Formatters;

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see cref="LocalizeFormat"/>.
/// Reads a localization format string from CSV (e.g. <c>Hello, {0}!</c>) and
/// writes MessagePack output matching <c>LocalizeFormatFormatter</c>.
/// </summary>
public sealed class LocalizeFormatCsvFormatter : ICsvFormatter<LocalizeFormat>
{
    public static readonly LocalizeFormatCsvFormatter Instance = new();

    private static readonly IMessagePackFormatter<LocalizeFormat> s_mpFormatter =
        Localization.MessagePack.LocalizationResolver.Shared.GetFormatter<LocalizeFormat>()!;

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        var format = LocalizeFormat.Parse(str);
        s_mpFormatter.Serialize(ref writer, format, MessagePackSerializerOptions.Standard);
    }
}
