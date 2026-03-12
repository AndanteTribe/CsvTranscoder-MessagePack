using CsvTranscoder.MessagePack.Sample.Attributes;
using CsvTranscoder.MessagePack.Sample.Enums;
using CsvTranscoder.MessagePack.Sample.Units;
using GameKernel;
using Localization;
using MessagePack;

namespace CsvTranscoder.MessagePack.Sample;

[MessagePackObject]
[FileName("enem")]
public record EnemyMasterEntity
{
    private readonly Obscured<MasterId<BattleField>> _id;

    /// <summary>
    /// マスターID.
    /// </summary>
    [Key(0)]
    public required MasterId<BattleField> Id
    {
        get => _id;
        init => _id = value;
    }

    /// <summary>
    /// グループ（フィールド種別）.
    /// </summary>
    [IgnoreMember]
    public BattleField Group => Id.Group;

    /// <summary>
    /// 種族.
    /// </summary>
    [LocalizedMember, Key(1)]
    public required string Species { get; init; }

    /// <summary>
    /// 性質.
    /// </summary>
    [Key(2)]
    public required Nature Property { get; init; }

    private readonly Obscured<BasicStatus> _status;

    /// <summary>
    /// 基礎ステータス.
    /// </summary>
    [Key(3)]
    public required BasicStatus Status
    {
        get => _status;
        init => _status = value;
    }

    private readonly Obscured<CompatibilityGroup> _compatibilities;

    /// <summary>
    /// 相性情報.
    /// </summary>
    [Key(4)]
    public required CompatibilityGroup Compatibilities
    {
        get => _compatibilities;
        init => _compatibilities = value;
    }
}