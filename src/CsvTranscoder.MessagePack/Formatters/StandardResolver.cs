using AndanteTribe.Csv.Formatters;

namespace AndanteTribe.Csv;

/// <summary>
/// A built-in <see cref="ICsvFormatterResolver"/> that provides formatters for standard .NET types.
/// Supports all primitive types, <see cref="DateTime"/>, <see cref="DateTimeOffset"/>,
/// <see cref="TimeSpan"/>, <see cref="Guid"/>, <see cref="Uri"/>, <see cref="Version"/>,
/// <see cref="decimal"/>, <see cref="char"/>, <see cref="string"/>, enum types,
/// <see cref="Nullable{T}"/> wrappers, and <see cref="ValueTuple"/> types with up to 7 elements.
/// </summary>
public sealed class StandardResolver : ICsvFormatterResolver
{
    public static readonly StandardResolver Instance = new();

    private static readonly RuntimeTypeHandle s_valueTuple1Handle = typeof(ValueTuple<>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple2Handle = typeof(ValueTuple<,>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple3Handle = typeof(ValueTuple<,,>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple4Handle = typeof(ValueTuple<,,,>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple5Handle = typeof(ValueTuple<,,,,>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple6Handle = typeof(ValueTuple<,,,,,>).TypeHandle;
    private static readonly RuntimeTypeHandle s_valueTuple7Handle = typeof(ValueTuple<,,,,,,>).TypeHandle;

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
        Cache<DateTimeOffset>.Value = DateTimeOffsetFormatter.Instance;
        Cache<TimeSpan>.Value = TimeSpanFormatter.Instance;
        Cache<Guid>.Value = GuidFormatter.Instance;
        Cache<Uri?>.Value = UriFormatter.Instance;
        Cache<Version?>.Value = VersionFormatter.Instance;
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
            Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(typeof(EnumMemberCsvFormatter<>).MakeGenericType(typeof(T)))!;
            return Cache<T>.Value;
        }

        if (typeof(T).IsGenericType)
        {
            var def = typeof(T).GetGenericTypeDefinition();

            if (def == typeof(Nullable<>))
            {
                Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(typeof(NullableFormatter<>).MakeGenericType(Nullable.GetUnderlyingType(typeof(T))!))!;
                return Cache<T>.Value;
            }

            var defHandle = def.TypeHandle;
            Type? formatterType = null;
            if (defHandle.Equals(s_valueTuple1Handle))
            {
                formatterType = typeof(ValueTupleFormatter<>);
            }
            else if (defHandle.Equals(s_valueTuple2Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,>);
            }
            else if (defHandle.Equals(s_valueTuple3Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,,>);
            }
            else if (defHandle.Equals(s_valueTuple4Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,,,>);
            }
            else if (defHandle.Equals(s_valueTuple5Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,,,,>);
            }
            else if (defHandle.Equals(s_valueTuple6Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,,,,,>);
            }
            else if (defHandle.Equals(s_valueTuple7Handle))
            {
                formatterType = typeof(ValueTupleFormatter<,,,,,,>);
            }

            if (formatterType is not null)
            {
                Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(formatterType.MakeGenericType(typeof(T).GetGenericArguments()))!;
                return Cache<T>.Value;
            }
        }

        return null;
    }
}
