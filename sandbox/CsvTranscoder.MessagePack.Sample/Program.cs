using System.Reflection;
using AndanteTribe.Csv;
using AndanteTribe.Utils.MasterSample;
using CsvTranscoder.MessagePack.Sample;
using CsvTranscoder.MessagePack.Sample.Attributes;
using CsvTranscoder.MessagePack.Sample.Units;
using MessagePack;

// Compose a resolver that covers all types used by the master data:
//  1. MasterSampleCsvResolver  — generated formatter for [MessagePackObject] types in this project
//  2. GameKernelCsvResolver    — formatters for MasterId<T> and Obscured<T>
//  3. LocalizationCsvResolver  — formatter for LocalizeFormat
//  4. SampleCsvFormatterResolver — formatters for EnumMember enums and LocalizedMember strings
//  5. StandardResolver         — formatters for primitives, enums, string, etc.
var csvOptions = new CsvTranscodeOptions
{
    HasHeader = true,
    Separator = ',',
    NewLine = "\n",
    Resolver = CompositeResolver.Create(
        MasterSampleCsvResolver.Instance,
        AndanteTribe.Csv.GameKernelCsvResolver.Instance,
        AndanteTribe.Csv.LocalizationCsvResolver.Instance,
        CsvTranscoder.MessagePack.Sample.SampleCsvFormatterResolver.Instance,
        StandardResolver.Instance)
};

Console.WriteLine("CsvTranscoder + SourceGenerator sample");
Console.WriteLine();

// Show that the generated resolver has formatters for the project's types.
Console.WriteLine("--- Generated formatters ---");
Console.WriteLine($"BasicStatus            : {MasterSampleCsvResolver.Instance.GetFormatter<BasicStatus>()?.GetType().Name ?? "(null)"}");
Console.WriteLine($"CompatibilityGroup     : {MasterSampleCsvResolver.Instance.GetFormatter<CompatibilityGroup>()?.GetType().Name ?? "(null)"}");
Console.WriteLine($"EnemyMasterEntity      : {MasterSampleCsvResolver.Instance.GetFormatter<EnemyMasterEntity>()?.GetType().Name ?? "(null)"}");
Console.WriteLine($"GroundEnemyMasterEntity: {MasterSampleCsvResolver.Instance.GetFormatter<GroundEnemyMasterEntity>()?.GetType().Name ?? "(null)"}");
Console.WriteLine($"TextMasterEntity       : {MasterSampleCsvResolver.Instance.GetFormatter<TextMasterEntity>()?.GetType().Name ?? "(null)"}");
Console.WriteLine();

// Load CSV tables from the csv/ directory.
// [FileName] on each entity type maps it to its source CSV file.
Console.WriteLine("--- Loading CSV tables via generated formatters ---");
var csvDir = Path.Combine(AppContext.BaseDirectory, "csv");

await LoadAndPrintAsync<EnemyMasterEntity>(csvDir, csvOptions);
await LoadAndPrintAsync<GroundEnemyMasterEntity>(csvDir, csvOptions);
await LoadAndPrintAsync<TextMasterEntity>(csvDir, csvOptions);

// Locate the CSV file via [FileName], transcode it through the generated ICsvFormatter<T>,
// and display the resulting MessagePack payload as JSON.
static async Task LoadAndPrintAsync<T>(string csvDir, CsvTranscodeOptions options)
{
    var attr = typeof(T).GetCustomAttribute<FileNameAttribute>()
               ?? throw new InvalidOperationException(
                   $"{typeof(T).Name} must be decorated with [FileName].");

    var csvPath = Path.Combine(csvDir, $"{attr.Name}.csv");
    Console.WriteLine($"=== {typeof(T).Name}  ←  {attr.Name}.csv ===");

    try
    {
        using var inputStream = File.OpenRead(csvPath);
        using var outputStream = new MemoryStream();
        await AndanteTribe.Csv.CsvTranscoder.ToMessagePackAsync<T>(inputStream, outputStream, options);

        var json = MessagePackSerializer.ConvertToJson(outputStream.ToArray());
        const int maxJsonDisplayLength = 1000;
        Console.WriteLine(json.Length > maxJsonDisplayLength ? json[..maxJsonDisplayLength] + " …" : json);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] {ex.GetType().Name}: {ex.Message}");
    }

    Console.WriteLine();
}
