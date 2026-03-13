using GameKernel;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see cref="Obscured{T}"/>.
/// Reads the inner <typeparamref name="T"/> value from CSV using the resolver's formatter
/// and writes a 1-element MessagePack array <c>[value]</c> matching <c>ObscuredFormatter&lt;T&gt;</c>.
/// </summary>
/// <typeparam name="T">The inner value type.</typeparam>
public sealed class ObscuredCsvFormatter<T> : ICsvFormatter<Obscured<T>>
    where T : unmanaged
{
    public static readonly ObscuredCsvFormatter<T> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        writer.WriteArrayHeader(1);
        options.Resolver.GetFormatterWithVerify<T>().Transcode(ref writer, ref reader, options);
    }
}
