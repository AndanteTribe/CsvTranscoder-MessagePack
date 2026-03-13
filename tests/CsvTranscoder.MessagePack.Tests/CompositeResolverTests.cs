using AndanteTribe.Csv;
using AndanteTribe.Csv.Formatters;

namespace CsvTranscoder.MessagePack.Tests;

public class CompositeResolverTests
{
    [Fact]
    public void Create2_ReturnsFormatterFromFirstMatchingResolver()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var resolver = CompositeResolver.Create(r1, StandardResolver.Instance);

        Assert.NotNull(resolver.GetFormatter<string>());
        Assert.NotNull(resolver.GetFormatter<int>());
    }

    [Fact]
    public void Create2_ReturnsNullWhenNoResolverSupportsType()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2);

        Assert.Null(resolver.GetFormatter<bool>());
    }

    [Fact]
    public void Create3_ReturnsFormatterFromFirstMatchingResolver()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, StandardResolver.Instance);

        Assert.NotNull(resolver.GetFormatter<string>());
        Assert.NotNull(resolver.GetFormatter<int>());
        Assert.NotNull(resolver.GetFormatter<bool>());
    }

    [Fact]
    public void Create4_ReturnsFormatterFromFirstMatchingResolver()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var r3 = new SingleFormatterResolver<bool>(BooleanFormatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, r3, StandardResolver.Instance);

        Assert.NotNull(resolver.GetFormatter<string>());
        Assert.NotNull(resolver.GetFormatter<int>());
        Assert.NotNull(resolver.GetFormatter<bool>());
        Assert.NotNull(resolver.GetFormatter<double>());
    }

    [Fact]
    public void Create5_ReturnsFormatterFromFirstMatchingResolver()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var r3 = new SingleFormatterResolver<bool>(BooleanFormatter.Instance);
        var r4 = new SingleFormatterResolver<double>(DoubleFormatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, r3, r4, StandardResolver.Instance);

        Assert.NotNull(resolver.GetFormatter<string>());
        Assert.NotNull(resolver.GetFormatter<int>());
        Assert.NotNull(resolver.GetFormatter<bool>());
        Assert.NotNull(resolver.GetFormatter<double>());
        Assert.NotNull(resolver.GetFormatter<long>());
    }

    [Fact]
    public void CreateN_WithSingleResolver_ReturnsFormatterFromThatResolver()
    {
        var resolver = CompositeResolver.Create(StandardResolver.Instance);

        Assert.NotNull(resolver.GetFormatter<int>());
    }

    [Fact]
    public void Create_FirstResolverTakesPriorityOverSecond()
    {
        var firstFormatter = new CustomStringFormatter("first");
        var secondFormatter = new CustomStringFormatter("second");
        var first = new SingleFormatterResolver<string>(firstFormatter);
        var second = new SingleFormatterResolver<string>(secondFormatter);

        var resolver = CompositeResolver.Create(first, second);

        Assert.Same(firstFormatter, resolver.GetFormatter<string>());
    }

    [Fact]
    public void Create_ReturnsNullWhenNoResolverSupportsType()
    {
        var resolver = CompositeResolver.Create(new SingleFormatterResolver<string>(StringFormatter.Instance));

        Assert.Null(resolver.GetFormatter<int>());
    }

    [Fact]
    public void CreateN_WithNoResolvers_ReturnsNull()
    {
        var resolver = CompositeResolver.Create();

        Assert.Null(resolver.GetFormatter<int>());
    }

    [Fact]
    public void CreateN_CachesResultOnSubsequentCalls()
    {
        var resolver = CompositeResolver.Create(StandardResolver.Instance);

        var first = resolver.GetFormatter<int>();
        var second = resolver.GetFormatter<int>();

        Assert.Same(first, second);
    }

    [Fact]
    public void CreateN_ThrowsForNullResolversArgument()
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
