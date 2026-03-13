using AndanteTribe.Csv.Formatters;

namespace AndanteTribe.Csv;

/// <summary>
/// A built-in <see cref="ICsvFormatterResolver"/> that provides formatters for standard .NET types.
/// Supports all primitive types, <see cref="DateTime"/>, <see cref="TimeSpan"/>, <see cref="Guid"/>,
/// <see cref="decimal"/>, <see cref="char"/>, <see cref="string"/>, enum types, and <see cref="Nullable{T}"/> wrappers.
/// </summary>
public sealed class StandardResolver : ICsvFormatterResolver
{
    public static readonly StandardResolver Instance = new();

    private StandardResolver()
    {
        Cache<bool>.Value = BooleanFormatter.Instance;
        Cache<byte>.Value = ByteFormatter.Instance;
        Cache<sbyte>.Value = SByteFormatter.Instance;
        Cache<short>.Value = Int16Formatter.Instance;
        Cache<ushort>.Value = UInt16Formatter.Instance;
        Cache<int>.Value = Int32Formatter.Instance;
        Cache<uint>.Value = UInt32Formatter.Instance;
        Cache<long>.Value = Int64Formatter.Instance;
        Cache<ulong>.Value = UInt64Formatter.Instance;
        Cache<float>.Value = SingleFormatter.Instance;
        Cache<double>.Value = DoubleFormatter.Instance;
        Cache<decimal>.Value = DecimalFormatter.Instance;
        Cache<char>.Value = CharFormatter.Instance;
        Cache<string>.Value = StringFormatter.Instance;
        Cache<DateTime>.Value = DateTimeFormatter.Instance;
        Cache<TimeSpan>.Value = TimeSpanFormatter.Instance;
        Cache<Guid>.Value = GuidFormatter.Instance;
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
            Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(typeof(EnumFormatter<>).MakeGenericType(typeof(T)))!;
            return Cache<T>.Value;
        }

        if (typeof(T).IsGenericType && typeof(T).GetGenericTypeDefinition() == typeof(Nullable<>))
        {
            Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(typeof(NullableFormatter<>).MakeGenericType(Nullable.GetUnderlyingType(typeof(T))!))!;
            return Cache<T>.Value;
        }

        return null;
    }
}
