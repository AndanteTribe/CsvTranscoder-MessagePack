using MessagePack;

namespace AndanteTribe.Csv.Formatters;

public sealed class ValueTupleFormatter<T1> : ICsvFormatter<ValueTuple<T1>>
{
    public static readonly ValueTupleFormatter<T1> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(1);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2> : ICsvFormatter<ValueTuple<T1, T2>>
{
    public static readonly ValueTupleFormatter<T1, T2> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(2);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2, T3> : ICsvFormatter<ValueTuple<T1, T2, T3>>
{
    public static readonly ValueTupleFormatter<T1, T2, T3> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(3);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T3>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2, T3, T4> : ICsvFormatter<ValueTuple<T1, T2, T3, T4>>
{
    public static readonly ValueTupleFormatter<T1, T2, T3, T4> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(4);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T3>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T4>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5> : ICsvFormatter<ValueTuple<T1, T2, T3, T4, T5>>
{
    public static readonly ValueTupleFormatter<T1, T2, T3, T4, T5> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(5);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T3>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T4>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T5>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6> : ICsvFormatter<ValueTuple<T1, T2, T3, T4, T5, T6>>
{
    public static readonly ValueTupleFormatter<T1, T2, T3, T4, T5, T6> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(6);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T3>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T4>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T5>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T6>().Transcode(ref writer, ref reader);
    }
}

public sealed class ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7> : ICsvFormatter<ValueTuple<T1, T2, T3, T4, T5, T6, T7>>
{
    public static readonly ValueTupleFormatter<T1, T2, T3, T4, T5, T6, T7> Instance = new();

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader)
    {
        writer.WriteArrayHeader(7);
        reader.Options.Resolver.GetFormatterWithVerify<T1>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T2>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T3>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T4>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T5>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T6>().Transcode(ref writer, ref reader);
        reader.Options.Resolver.GetFormatterWithVerify<T7>().Transcode(ref writer, ref reader);
    }
}
