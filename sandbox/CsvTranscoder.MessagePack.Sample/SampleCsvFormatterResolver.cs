using AndanteTribe.Csv;
using CsvTranscoder.MessagePack.Sample.Formatters;

namespace CsvTranscoder.MessagePack.Sample;

/// <summary>
/// A sample-specific <see cref="ICsvFormatterResolver"/> that provides formatters for the
/// special CSV conventions used in this sandbox:
/// <list type="bullet">
///   <item>
///     <description>
///       <see cref="string"/> → <see cref="LocalizedMemberJapaneseCsvFormatter"/>:
///       reads the Japanese value from a paired ja/en column and skips the English column.
///     </description>
///   </item>
///   <item>
///     <description>
///       Any <see langword="enum"/> type → <see cref="EnumMemberCsvFormatter{T}"/>:
///       parses <see cref="System.Runtime.Serialization.EnumMemberAttribute"/> values and
///       handles <c>_</c>-separated flags.
///     </description>
///   </item>
/// </list>
/// Place this resolver before <see cref="StandardResolver"/> in a <see cref="CompositeResolver"/>
/// so that it takes priority over the built-in enum and string formatters.
/// </summary>
public sealed class SampleCsvFormatterResolver : ICsvFormatterResolver
{
    public static readonly SampleCsvFormatterResolver Instance = new();

    private SampleCsvFormatterResolver()
    {
        Cache<string>.Value = LocalizedMemberJapaneseCsvFormatter.Instance;
    }

    private static class Cache<T>
    {
        public static ICsvFormatter<T>? Value;
    }

    public ICsvFormatter<T>? GetFormatter<T>()
    {
        if (Cache<T>.Value is not null)
        {
            return Cache<T>.Value;
        }

        if (typeof(T).IsEnum)
        {
            Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(
                typeof(EnumMemberCsvFormatter<>).MakeGenericType(typeof(T)))!;
            return Cache<T>.Value;
        }

        return null;
    }
}
