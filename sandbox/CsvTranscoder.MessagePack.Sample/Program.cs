using System.Buffers;
using AndanteTribe.Csv;
using AndanteTribe.Utils.MasterSample;
using CsvTranscoder.MessagePack.Sample;
using CsvTranscoder.MessagePack.Sample.Units;
using MessagePack;

// Compose a resolver that covers all types used by the master data:
//  1. MasterSampleCsvResolver  — generated formatter for [MessagePackObject] types in this project
//  2. GameKernelCsvResolver    — formatters for MasterId<T> and Obscured<T>
//  3. LocalizationCsvResolver  — formatter for LocalizeFormat
//  4. StandardResolver         — formatters for primitives, enums, string, etc.
var csvOptions = new CsvTranscodeOptions
{
    HasHeader = true,
    Separator = ',',
    NewLine = "\n",
    Resolver = CompositeResolver.Create(
        MasterSampleCsvResolver.Instance,
        AndanteTribe.Csv.GameKernelCsvResolver.Instance,
        AndanteTribe.Csv.LocalizationCsvResolver.Instance,
        StandardResolver.Instance)
};

Console.WriteLine("CsvTranscoder + SourceGenerator sample");
Console.WriteLine();

// Verify that the generated resolver can find the generated formatters.
var basicStatusFormatter = MasterSampleCsvResolver.Instance.GetFormatter<BasicStatus>();
Console.WriteLine($"BasicStatus formatter      : {basicStatusFormatter?.GetType().Name ?? "(null)"}");

var compatFormatter = MasterSampleCsvResolver.Instance.GetFormatter<CompatibilityGroup>();
Console.WriteLine($"CompatibilityGroup formatter: {compatFormatter?.GetType().Name ?? "(null)"}");

var enemyFormatter = MasterSampleCsvResolver.Instance.GetFormatter<EnemyMasterEntity>();
Console.WriteLine($"EnemyMasterEntity formatter : {enemyFormatter?.GetType().Name ?? "(null)"}");

var textFormatter = MasterSampleCsvResolver.Instance.GetFormatter<TextMasterEntity>();
Console.WriteLine($"TextMasterEntity formatter  : {textFormatter?.GetType().Name ?? "(null)"}");
