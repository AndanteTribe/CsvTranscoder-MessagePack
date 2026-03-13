using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class BooleanFormatter : ICsvFormatter<bool>
{
    public static readonly BooleanFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadBoolean());
}

public sealed class ByteFormatter : ICsvFormatter<byte>
{
    public static readonly ByteFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadByte());
}

public sealed class SByteFormatter : ICsvFormatter<sbyte>
{
    public static readonly SByteFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadSByte());
}

public sealed class Int16Formatter : ICsvFormatter<short>
{
    public static readonly Int16Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadInt16());
}

public sealed class UInt16Formatter : ICsvFormatter<ushort>
{
    public static readonly UInt16Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadUInt16());
}

public sealed class Int32Formatter : ICsvFormatter<int>
{
    public static readonly Int32Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadInt32());
}

public sealed class UInt32Formatter : ICsvFormatter<uint>
{
    public static readonly UInt32Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadUInt32());
}

public sealed class Int64Formatter : ICsvFormatter<long>
{
    public static readonly Int64Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadInt64());
}

public sealed class UInt64Formatter : ICsvFormatter<ulong>
{
    public static readonly UInt64Formatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadUInt64());
}

public sealed class SingleFormatter : ICsvFormatter<float>
{
    public static readonly SingleFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadSingle());
}

public sealed class DoubleFormatter : ICsvFormatter<double>
{
    public static readonly DoubleFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadDouble());
}

public sealed class DecimalFormatter : ICsvFormatter<decimal>
{
    public static readonly DecimalFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => MessagePack.Formatters.DecimalFormatter.Instance.Serialize(ref writer, reader.ReadDecimal(), MessagePackSerializerOptions.Standard);
}

public sealed class CharFormatter : ICsvFormatter<char>
{
    public static readonly CharFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadChar());
}

public sealed class StringFormatter : ICsvFormatter<string>
{
    public static readonly StringFormatter Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
        => writer.Write(reader.ReadString());
}
