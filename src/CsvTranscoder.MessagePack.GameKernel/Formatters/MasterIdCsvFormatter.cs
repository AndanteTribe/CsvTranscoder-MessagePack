using GameKernel;
using MessagePack;
using MessagePack.Formatters;

namespace AndanteTribe.Csv.Formatters;

/// <summary>
/// An <see cref="ICsvFormatter{T}"/> for <see cref="MasterId{TGroup}"/>.
/// Reads a field in <c>GroupName.NNNN</c> format (e.g. <c>Air.0001</c>) and writes
/// a 2-element MessagePack array <c>[group, id]</c> matching <c>MasterIdFormatter&lt;TGroup&gt;</c>.
/// </summary>
/// <typeparam name="TGroup">The enum type that represents the master-data group.</typeparam>
public sealed class MasterIdCsvFormatter<TGroup> : ICsvFormatter<MasterId<TGroup>>
    where TGroup : unmanaged, Enum
{
    public static readonly MasterIdCsvFormatter<TGroup> Instance = new();

    private static readonly IMessagePackFormatter<MasterId<TGroup>> s_mpFormatter =
        GameKernel.MessagePack.GameKernelResolver.Shared.GetFormatter<MasterId<TGroup>>()!;

    public void Transcode(ref MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
    {
        var str = reader.ReadString();

        var dotIndex = str.AsSpan().IndexOf('.');
        if (dotIndex < 0)
        {
            throw new FormatException($"Cannot parse '{str}' as MasterId<{typeof(TGroup).Name}>: missing '.' separator.");
        }

        var groupSpan = str.AsSpan(0, dotIndex);
        var idSpan = str.AsSpan(dotIndex + 1);

        if (!Enum.TryParse<TGroup>(groupSpan, ignoreCase: true, out var group))
        {
            throw new FormatException($"Cannot parse '{groupSpan}' as {typeof(TGroup).Name}.");
        }

        if (!uint.TryParse(idSpan, out var id))
        {
            throw new FormatException($"Cannot parse '{idSpan}' as uint.");
        }

        var masterId = new MasterId<TGroup>(group, id);
        s_mpFormatter.Serialize(ref writer, masterId, MessagePackSerializerOptions.Standard);
    }
}
