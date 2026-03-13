using System.Runtime.CompilerServices;
using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class EnumFormatter<T> : ICsvFormatter<T>
    where T : struct, Enum
{
    public static readonly EnumFormatter<T> Instance = new();

    private static readonly TypeCode s_underlyingTypeCode = Type.GetTypeCode(Enum.GetUnderlyingType(typeof(T)));

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();
        T value;

        if (string.IsNullOrEmpty(str) || !Enum.TryParse(str, ignoreCase: true, out value))
        {
            value = default;
        }

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
