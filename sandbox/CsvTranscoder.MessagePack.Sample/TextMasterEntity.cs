using CsvTranscoder.MessagePack.Sample.Enums;
using GameKernel;
using Localization;
using MessagePack;

namespace CsvTranscoder.MessagePack.Sample;

[MessagePackObject]
public class TextMasterEntity
{
    /// <summary>
    /// マスターID.
    /// </summary>
    [Key(0)]
    public required MasterId<TextCategory> Id { get; init; }

    /// <summary>
    /// グループ（テキストカテゴリー）.
    /// </summary>
    [IgnoreMember]
    public TextCategory Group => Id.Group;

    /// <summary>
    /// ローカライズフォーマット.
    /// </summary>
    [Key(1)]
    public required LocalizeFormat Format { get; init; }

    /// <summary>
    /// ローカライズテキスト.
    /// </summary>
    /// <remarks>
    /// フォーマットなしテキストはこちらから取得可能.
    /// </remarks>
    [IgnoreMember]
    public string Text => Format.ToString();
}