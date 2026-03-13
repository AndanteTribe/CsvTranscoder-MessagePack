using AndanteTribe.Csv.Formatters;
using Localization;

namespace AndanteTribe.Csv;

/// <summary>
/// An <see cref="ICsvFormatterResolver"/> that provides CSV formatters for
/// <see cref="Localization"/> types: <see cref="LocalizeFormat"/>.
/// </summary>
public sealed class LocalizationCsvResolver : ICsvFormatterResolver
{
    public static readonly LocalizationCsvResolver Instance = new();

    private LocalizationCsvResolver()
    {
        Cache<LocalizeFormat>.Value = LocalizeFormatCsvFormatter.Instance;
    }

    private static class Cache<T>
    {
        public static ICsvFormatter<T>? Value;
    }

    public ICsvFormatter<T>? GetFormatter<T>() => Cache<T>.Value;
}
