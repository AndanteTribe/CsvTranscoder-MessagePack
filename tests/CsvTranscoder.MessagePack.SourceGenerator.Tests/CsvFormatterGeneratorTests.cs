using Microsoft.CodeAnalysis;

namespace CsvTranscoder.MessagePack.SourceGenerator.Tests;

/// <summary>
/// Unit tests for <see cref="AndanteTribe.Csv.SourceGenerator.CsvFormatterGenerator"/>.
/// Each test creates an in-memory compilation, runs the generator, and inspects either
/// the generated source code or the output compilation for correctness.
/// </summary>
public class CsvFormatterGeneratorTests
{
    // -----------------------------------------------------------------------
    //  Formatter generation
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesFormatter_ForSimpleMessagePackObject()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Foo
            {
                [Key(0)] public int Id { get; set; }
                [Key(1)] public string Name { get; set; } = "";
            }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var formatterSource = GetGeneratedSource(compilation, "Formatter_MyNs_Foo");
        Assert.NotNull(formatterSource);
        Assert.Contains("ICsvFormatter<global::MyNs.Foo>", formatterSource);
        Assert.Contains("writer.WriteArrayHeader(2)", formatterSource);
        Assert.Contains("GetFormatterWithVerify<int>", formatterSource);
        Assert.Contains("GetFormatterWithVerify<string>", formatterSource);
    }

    [Fact]
    public void GeneratesFormatter_WithSparseKeys_WritesNilForMissingKeys()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Sparse
            {
                [Key(0)] public int A { get; set; }
                // Key(1) intentionally missing
                [Key(2)] public int B { get; set; }
            }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var src = GetGeneratedSource(compilation, "Formatter_MyNs_Sparse");
        Assert.NotNull(src);
        // Array header must be maxKey+1 = 3
        Assert.Contains("writer.WriteArrayHeader(3)", src);
        // Sparse slot (key 1) writes nil
        Assert.Contains("writer.WriteNil();", src);
    }

    [Fact]
    public void IgnoresMember_WithIgnoreMemberAttribute()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Bar
            {
                [Key(0)] public int Id { get; set; }
                [IgnoreMember] public string Ignored { get; set; } = "";
            }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var src = GetGeneratedSource(compilation, "Formatter_MyNs_Bar");
        Assert.NotNull(src);
        Assert.Contains("writer.WriteArrayHeader(1)", src);
    }

    [Fact]
    public void SkipsType_WithNoKeyMembers()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class NoKeys
            {
                [IgnoreMember] public int X { get; set; }
            }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // No formatter should be generated for a type with no [Key] members.
        var src = GetGeneratedSource(compilation, "Formatter_MyNs_NoKeys");
        Assert.Null(src);
    }

    [Fact]
    public void SkipsOpenGenericTypes()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Generic<T>
            {
                [Key(0)] public T Value { get; set; } = default!;
            }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // Open generic types should not generate a formatter.
        Assert.DoesNotContain(
            GetAllGeneratedSources(compilation),
            s => s.Contains("GenericCsvFormatter"));
    }

    // -----------------------------------------------------------------------
    //  Resolver generation
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesResolver_ForGeneratedCsvFormatterResolverClass()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Foo { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "Resolver_MyNs_MyResolver");
        Assert.NotNull(resolverSrc);
        Assert.Contains("partial class MyResolver", resolverSrc);
        Assert.Contains("ICsvFormatterResolver", resolverSrc);
        Assert.Contains("public static readonly MyResolver Instance", resolverSrc);
        Assert.Contains("GetFormatter<T>()", resolverSrc);
    }

    [Fact]
    public void GeneratedResolver_RegistersAllMessagePackObjectTypes()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class TypeA { [Key(0)] public int X { get; set; } }
            [MessagePackObject]
            public class TypeB { [Key(0)] public string Y { get; set; } = ""; }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "Resolver_MyNs_MyResolver");
        Assert.NotNull(resolverSrc);
        Assert.Contains("global::MyNs.TypeA", resolverSrc);
        Assert.Contains("global::MyNs.TypeB", resolverSrc);
    }

    [Fact]
    public void GeneratedResolver_InGlobalNamespace()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            [MessagePackObject]
            public class TopLevelType { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class GlobalResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "Resolver_GlobalResolver");
        Assert.NotNull(resolverSrc);
        Assert.Contains("partial class GlobalResolver", resolverSrc);
    }

    // -----------------------------------------------------------------------
    //  Attribute injection and #if guard
    // -----------------------------------------------------------------------

    [Fact]
    public void InjectsGeneratedCsvFormatterResolverAttribute_WithDisableGuard()
    {
        var (compilation, _) = GeneratorTestHelper.RunGenerator();

        var attrSrc = GetGeneratedSource(compilation, "GeneratedCsvFormatterResolverAttribute");
        Assert.NotNull(attrSrc);
        Assert.Contains("GeneratedCsvFormatterResolverAttribute", attrSrc);
        Assert.Contains("AttributeUsage", attrSrc);
        Assert.Contains("#if !DISABLE_CSVTRANSCODER_MESSAGEPACK", attrSrc);
        Assert.Contains("#endif", attrSrc);
    }

    [Fact]
    public void GeneratedFormatter_IsWrappedWithDisableGuard()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Guarded { [Key(0)] public int X { get; set; } }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var src = GetGeneratedSource(compilation, "Formatter_MyNs_Guarded");
        Assert.NotNull(src);
        Assert.Contains("#if !DISABLE_CSVTRANSCODER_MESSAGEPACK", src);
        Assert.Contains("#endif", src);
    }

    [Fact]
    public void GeneratedResolver_IsWrappedWithDisableGuard()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Foo { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "Resolver_MyNs_MyResolver");
        Assert.NotNull(resolverSrc);
        Assert.Contains("#if !DISABLE_CSVTRANSCODER_MESSAGEPACK", resolverSrc);
        Assert.Contains("#endif", resolverSrc);
    }

    // -----------------------------------------------------------------------
    //  Struct support
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesFormatter_ForReadonlyRecordStruct()
    {
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public readonly record struct MyStruct(
                [property: Key(0)] int X,
                [property: Key(1)] int Y);
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var src = GetGeneratedSource(compilation, "Formatter_MyNs_MyStruct");
        Assert.NotNull(src);
        Assert.Contains("ICsvFormatter<global::MyNs.MyStruct>", src);
        Assert.Contains("writer.WriteArrayHeader(2)", src);
    }

    // -----------------------------------------------------------------------
    //  Helpers
    // -----------------------------------------------------------------------

    private static string? GetGeneratedSource(Compilation compilation, string hintNameContains)
    {
        foreach (var tree in compilation.SyntaxTrees)
        {
            if (tree.FilePath.Contains(hintNameContains))
                return tree.GetText().ToString();
        }

        return null;
    }

    private static IEnumerable<string> GetAllGeneratedSources(Compilation compilation)
        => compilation.SyntaxTrees.Select(t => t.GetText().ToString());
}
