namespace AndanteTribe.Csv;

public record CsvTranscodeOptions
{
    public bool HasHeader { get; init; } = true;
    public bool AllowColumnComments { get; init; } = true;
    public bool AllowRowComments { get; init; } = true;
    public string NewLine { get; init; } = System.Environment.NewLine;
    public char Separator { get; init; } = ',';
    public ICsvFormatterResolver Resolver { get; init; }
}