using AndanteTribe.Utils.MasterSample;
using CsvTranscoder.MessagePack.Sample.Enums;
using GameKernel;
using MessagePack;

namespace CsvTranscoder.MessagePack.Sample;

public record GroundEnemyMasterEntity
{
    private readonly Obscured<MasterId<GroundEnemyCategory>> _id;

    /// <summary>
    /// マスターID.
    /// </summary>
    [Key(0)]
    public required MasterId<GroundEnemyCategory> Id
    {
        get => _id;
        init => _id = value;
    }

    /// <summary>
    /// グループ（地上敵種別）.
    /// </summary>
    [IgnoreMember]
    public GroundEnemyCategory Group => Id.Group;

    private readonly Obscured<MasterId<BattleField>> _enemyId;

    /// <summary>
    /// <see cref="EnemyMasterEntity"/>のマスターID.
    /// </summary>
    [Key(1)]
    public required MasterId<BattleField> EnemyId
    {
        get => _enemyId;
        init => _enemyId = value;
    }

    private readonly Obscured<uint> _idleChaseDistance;

    /// <summary>
    /// 非戦闘時の敵とプレイヤーの距離.
    /// </summary>
    [Key(2)]
    public required uint IdleChaseDistance
    {
        get => _idleChaseDistance;
        init => _idleChaseDistance = value;
    }

    private readonly Obscured<uint> _battleChaseDistance;

    /// <summary>
    /// 戦闘時の敵とプレイヤーの距離.
    /// </summary>
    [Key(3)]
    public required uint BattleChaseDistance
    {
        get => _battleChaseDistance;
        init => _battleChaseDistance = value;
    }
}