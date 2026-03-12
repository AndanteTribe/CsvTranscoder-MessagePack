namespace CsvTranscoder.MessagePack.Sample.Attributes;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public class FileNameAttribute : Attribute
{
    public readonly string Name;

    public FileNameAttribute(string name)
    {
        Name = name;
    }
}