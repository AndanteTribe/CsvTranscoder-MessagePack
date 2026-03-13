using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;

namespace CsvTranscoder.MessagePack.Tests;

public class CompositeResolverTests
{
    [Fact]
    public void Create_WithSingleResolver_ReturnsFormatterFromThatResolver()
    {
        var resolver = CompositeResolver.Create(StandardResolver.Instance);

        var formatter = resolver.GetFormatter<int>();

        Assert.NotNull(formatter);
    }

    [Fact]
    public void Create_WithMultipleResolvers_ReturnsFormatterFromFirstMatchingResolver()
    {
        // Arrange: a resolver that only handles string, and the standard resolver
        var stringOnlyResolver = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var resolver = CompositeResolver.Create(stringOnlyResolver, StandardResolver.Instance);

        // string comes from the first resolver, int from the second
        var stringFormatter = resolver.GetFormatter<string>();
        var intFormatter = resolver.GetFormatter<int>();

        Assert.NotNull(stringFormatter);
        Assert.NotNull(intFormatter);
    }

    [Fact]
    public void Create_FirstResolverTakesPriorityOverSecond()
    {
        // Arrange: two resolvers both supporting string, the first should win
        var firstFormatter = new CustomStringFormatter("first");
        var secondFormatter = new CustomStringFormatter("second");
        var first = new SingleFormatterResolver<string>(firstFormatter);
        var second = new SingleFormatterResolver<string>(secondFormatter);

        var resolver = CompositeResolver.Create(first, second);
        var formatter = resolver.GetFormatter<string>();

        Assert.Same(firstFormatter, formatter);
    }

    [Fact]
    public void Create_ReturnsNullWhenNoResolverSupportsType()
    {
        var resolver = CompositeResolver.Create(new SingleFormatterResolver<string>(StringFormatter.Instance));

        var formatter = resolver.GetFormatter<int>();

        Assert.Null(formatter);
    }

    [Fact]
    public void Create_WithNoResolvers_ReturnsNull()
    {
        var resolver = CompositeResolver.Create();

        var formatter = resolver.GetFormatter<int>();

        Assert.Null(formatter);
    }

    [Fact]
    public void Create_CachesResultOnSubsequentCalls()
    {
        var resolver = CompositeResolver.Create(StandardResolver.Instance);

        var first = resolver.GetFormatter<int>();
        var second = resolver.GetFormatter<int>();

        Assert.Same(first, second);
    }

    [Fact]
    public void Create_ThrowsForNullResolversArgument()
    {
        Assert.Throws<ArgumentNullException>(() => CompositeResolver.Create(null!));
    }

    // ─── helper types ───────────────────────────────────────────────────────────

    private sealed class SingleFormatterResolver<TType>(ICsvFormatter<TType> formatter) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
            => typeof(T) == typeof(TType) ? (ICsvFormatter<T>)(object)formatter : null;
    }

    private sealed class CustomStringFormatter(string tag) : ICsvFormatter<string>
    {
        public string Tag => tag;

        public void Transcode(ref global::MessagePack.MessagePackWriter writer, ref CsvReader reader, CsvTranscodeOptions options)
            => StringFormatter.Instance.Transcode(ref writer, ref reader, options);
    }
}
