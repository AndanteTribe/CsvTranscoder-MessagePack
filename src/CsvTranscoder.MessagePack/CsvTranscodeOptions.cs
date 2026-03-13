namespace AndanteTribe.Csv;

public record CsvTranscodeOptions
{
    public bool HasHeader { get; init; } = true;
    public bool AllowColumnComments { get; init; } = true;
    public bool AllowRowComments { get; init; } = true;
    public string NewLine { get; init; } = System.Environment.NewLine;
    public char Separator { get; init; } = ',';
    public Quote Quote { get; init; } = Quote.Minimal;
    public ICsvFormatterResolver Resolver { get; init; } = StandardResolver.Instance;
}

public enum Quote : byte
{
    None = 0,
    Minimal = 1,
    NoneNumeric = 2,
    All = 3
}