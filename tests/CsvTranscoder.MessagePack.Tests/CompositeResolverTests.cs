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
    public void Create3_ReturnsNullWhenNoResolverSupportsType()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var r3 = new SingleFormatterResolver<bool>(BooleanFormatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, r3);

        Assert.Null(resolver.GetFormatter<double>());
    }

    [Fact]
    public void Create4_ReturnsNullWhenNoResolverSupportsType()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var r3 = new SingleFormatterResolver<bool>(BooleanFormatter.Instance);
        var r4 = new SingleFormatterResolver<double>(DoubleFormatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, r3, r4);

        Assert.Null(resolver.GetFormatter<long>());
    }

    [Fact]
    public void Create5_ReturnsNullWhenNoResolverSupportsType()
    {
        var r1 = new SingleFormatterResolver<string>(StringFormatter.Instance);
        var r2 = new SingleFormatterResolver<int>(Int32Formatter.Instance);
        var r3 = new SingleFormatterResolver<bool>(BooleanFormatter.Instance);
        var r4 = new SingleFormatterResolver<double>(DoubleFormatter.Instance);
        var r5 = new SingleFormatterResolver<long>(Int64Formatter.Instance);
        var resolver = CompositeResolver.Create(r1, r2, r3, r4, r5);

        Assert.Null(resolver.GetFormatter<float>());
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
    public void CreateN_ThrowsForNullResolversArgument()
    {
        Assert.Throws<ArgumentNullException>(() => CompositeResolver.Create(null!));
    }

    [Fact]
    public void CreateN_ThrowsForNullElementInArray()
    {
        // 6 args forces the params overload (fixed overloads only go up to 5),
        // so the per-element null validation loop inside Create(params...) is exercised.
        var r = StandardResolver.Instance;
        Assert.Throws<ArgumentNullException>(() =>
            CompositeResolver.Create(r, r, r, r, r, null!));
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public void Create2_ThrowsForNullArgument(int nullPosition)
    {
        var r = StandardResolver.Instance;
        Assert.Throws<ArgumentNullException>(() => nullPosition switch
        {
            1 => CompositeResolver.Create(null!, r),
            _ => CompositeResolver.Create(r, null!),
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    public void Create3_ThrowsForNullArgument(int nullPosition)
    {
        var r = StandardResolver.Instance;
        Assert.Throws<ArgumentNullException>(() => nullPosition switch
        {
            1 => CompositeResolver.Create(null!, r, r),
            2 => CompositeResolver.Create(r, null!, r),
            _ => CompositeResolver.Create(r, r, null!),
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    public void Create4_ThrowsForNullArgument(int nullPosition)
    {
        var r = StandardResolver.Instance;
        Assert.Throws<ArgumentNullException>(() => nullPosition switch
        {
            1 => CompositeResolver.Create(null!, r, r, r),
            2 => CompositeResolver.Create(r, null!, r, r),
            3 => CompositeResolver.Create(r, r, null!, r),
            _ => CompositeResolver.Create(r, r, r, null!),
        });
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    public void Create5_ThrowsForNullArgument(int nullPosition)
    {
        var r = StandardResolver.Instance;
        Assert.Throws<ArgumentNullException>(() => nullPosition switch
        {
            1 => CompositeResolver.Create(null!, r, r, r, r),
            2 => CompositeResolver.Create(r, null!, r, r, r),
            3 => CompositeResolver.Create(r, r, null!, r, r),
            4 => CompositeResolver.Create(r, r, r, null!, r),
            _ => CompositeResolver.Create(r, r, r, r, null!),
        });
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

        public void Transcode(ref global::MessagePack.MessagePackWriter writer, ref CsvReader reader)
            => StringFormatter.Instance.Transcode(ref writer, ref reader);
    }
}
