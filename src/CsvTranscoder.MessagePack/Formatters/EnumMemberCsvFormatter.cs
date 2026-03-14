using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see langword="enum"/> types that supports
/// values specified via <see cref="EnumMemberAttribute.Value"/>.
/// For <see cref="FlagsAttribute"/> enums, multiple values may be combined using
/// <c>_</c> as a separator (e.g., <c>"混沌_中庸"</c>).
/// Falls back to <see cref="Enum.TryParse{TEnum}(string, bool, out TEnum)"/> when no
/// <see cref="EnumMemberAttribute"/> match is found.
/// </summary>
/// <typeparam name="T">The enum type.</typeparam>
public sealed class EnumMemberCsvFormatter<T> : ICsvFormatter<T>
    where T : struct, Enum
{
    public static readonly EnumMemberCsvFormatter<T> Instance = new();

    private static readonly TypeCode s_underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));
    private static readonly bool s_isFlags = typeof(T).IsDefined(typeof(FlagsAttribute), inherit: false);
    private static readonly Dictionary<string, T> s_memberLookup = BuildMemberLookup();

    private static Dictionary<string, T> BuildMemberLookup()
    {
        var dict = new Dictionary<string, T>(StringComparer.Ordinal);
        foreach (var field in typeof(T).GetFields(BindingFlags.Public | BindingFlags.Static))
        {
            var attr = field.GetCustomAttribute<EnumMemberAttribute>();
            if (attr?.Value is not null)
            {
                dict[attr.Value] = (T)field.GetValue(null)!;
            }
        }

        return dict;
    }

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        var str = reader.ReadString();
        T value;

        if (string.IsNullOrEmpty(str))
        {
            value = default;
        }
        else if (s_isFlags && str.IndexOf('_') >= 0)
        {
            value = ParseFlags(str);
        }
        else if (!s_memberLookup.TryGetValue(str, out value) &&
                 !Enum.TryParse(str, ignoreCase: true, out value))
        {
            value = default;
        }

        WriteValue(ref writer, value);
    }

    private static T ParseFlags(string str)
    {
        ulong combined = 0;
        var span = str.AsSpan();

        while (!span.IsEmpty)
        {
            int sep = span.IndexOf('_');
            var part = sep >= 0 ? span[..sep] : span;
            span = sep >= 0 ? span[(sep + 1)..] : ReadOnlySpan<char>.Empty;

            if (part.IsEmpty) continue;

            var partStr = part.ToString();
            if (s_memberLookup.TryGetValue(partStr, out var memberValue))
            {
                combined |= Convert.ToUInt64(memberValue);
            }
            else if (Enum.TryParse<T>(partStr, ignoreCase: true, out var parsed))
            {
                combined |= Convert.ToUInt64(parsed);
            }
        }

        return (T)Enum.ToObject(typeof(T), combined);
    }

    private static void WriteValue(ref MessagePackWriter writer, T value)
    {
        switch (s_underlyingTypeCode)
        {
            case TypeCode.SByte:
                writer.Write(Unsafe.As<T, sbyte>(ref value));
                break;
            case TypeCode.Byte:
                writer.Write(Unsafe.As<T, byte>(ref value));
                break;
            case TypeCode.Int16:
                writer.Write(Unsafe.As<T, short>(ref value));
                break;
            case TypeCode.UInt16:
                writer.Write(Unsafe.As<T, ushort>(ref value));
                break;
            case TypeCode.Int32:
                writer.Write(Unsafe.As<T, int>(ref value));
                break;
            case TypeCode.UInt32:
                writer.Write(Unsafe.As<T, uint>(ref value));
                break;
            case TypeCode.Int64:
                writer.Write(Unsafe.As<T, long>(ref value));
                break;
            case TypeCode.UInt64:
                writer.Write(Unsafe.As<T, ulong>(ref value));
                break;
            default:
                writer.Write(Unsafe.As<T, int>(ref value));
                break;
        }
    }
}
