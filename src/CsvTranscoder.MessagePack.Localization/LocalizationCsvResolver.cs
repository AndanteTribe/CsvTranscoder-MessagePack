using AndanteTribe.Csv.Formatters;
using Localization;

namespace AndanteTribe.Csv;

/// <summary>
/// An <see cref="ICsvFormatterResolver"/> that provides CSV formatters for
/// <see cref="Localization"/> types: <see cref="LocalizeFormat"/> and
/// <see cref="string"/> members decorated with <see cref="LocalizedMemberAttribute"/>.
/// </summary>
/// <remarks>
/// When this resolver is included in a <see cref="CompositeResolver"/> before
/// <see cref="StandardResolver"/>, every <see langword="string"/> CSV column is
/// handled by <see cref="LocalizedMemberJapaneseCsvFormatter"/>, which reads the
/// Japanese value and skips the paired English column.  Only include this resolver
/// in composites for entities whose string properties are all localized pairs.
/// </remarks>
public sealed class LocalizationCsvResolver : ICsvFormatterResolver
{
    public static readonly LocalizationCsvResolver Instance = new();

    private LocalizationCsvResolver()
    {
        Cache<LocalizeFormat>.Value = LocalizeFormatCsvFormatter.Instance;
        Cache<string>.Value = LocalizedMemberJapaneseCsvFormatter.Instance;
    }

    private static class Cache<T>
    {
        public static ICsvFormatter<T>? Value;
    }

    public ICsvFormatter<T>? GetFormatter<T>() => Cache<T>.Value;
}
