using Microsoft.CodeAnalysis;

namespace CsvTranscoder.MessagePack.SourceGenerator.Tests;

/// <summary>
/// Unit tests for <see cref="AndanteTribe.Csv.SourceGenerator.CsvFormatterGenerator"/>.
/// Each test creates an in-memory compilation, runs the generator, and inspects either
/// the generated source code or the output compilation for correctness.
/// Each formatter is emitted in its own <c>.g.cs</c> partial-class file (e.g.
/// <c>MyNs.MyResolver.FooCsvFormatter.g.cs</c>) nested inside the resolver class.
/// The resolver body (Instance, GetFormatter, FormatterLookup) lives in <c>MyNs.MyResolver.g.cs</c>.
/// File names are PascalCase with dots, matching the convention of MessagePack-CSharp's generator.
/// </summary>
public class CsvFormatterGeneratorTests
{
    // -----------------------------------------------------------------------
    //  Formatter generation (each formatter in its own .g.cs file)
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesFormatter_ForSimpleMessagePackObject()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Foo
            {
                [Key(0)] public int Id { get; set; }
                [Key(1)] public string Name { get; set; } = "";
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Resolver body file: hint name uses PascalCase dots.
        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // Formatter class definition is in the formatter-specific file.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.FooCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("internal sealed class FooCsvFormatter", fmtSrc);
        Assert.Contains("ICsvFormatter<global::MyNs.Foo>", fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(2)", fmtSrc);
        Assert.Contains("GetFormatterWithVerify<int>", fmtSrc);
        Assert.Contains("GetFormatterWithVerify<string>", fmtSrc);
    }

    [Fact]
    public void GeneratesFormatter_WithSparseKeys_WritesNilForMissingKeys()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Sparse
            {
                [Key(0)] public int A { get; set; }
                // Key(1) intentionally missing
                [Key(2)] public int B { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // Resolver body references SparseCsvFormatter in FormatterLookup.
        Assert.Contains("SparseCsvFormatter", resolverSrc);
        // Formatter-specific file has the array header and nil writes.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.SparseCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        // Array header must be maxKey+1 = 3
        Assert.Contains("writer.WriteArrayHeader(3)", fmtSrc);
        // Sparse slot (key 1) writes nil
        Assert.Contains("writer.WriteNil();", fmtSrc);
    }

    [Fact]
    public void IgnoresMember_WithIgnoreMemberAttribute()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Bar
            {
                [Key(0)] public int Id { get; set; }
                [IgnoreMember] public string Ignored { get; set; } = "";
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // Resolver body references BarCsvFormatter in FormatterLookup.
        Assert.Contains("BarCsvFormatter", resolverSrc);
        // Formatter-specific file has the array header.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.BarCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
    }

    [Fact]
    public void SkipsType_WithNoKeyMembers()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class NoKeys
            {
                [IgnoreMember] public int X { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // No formatter should be generated for a type with no [Key] members.
        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.DoesNotContain("NoKeysCsvFormatter", resolverSrc);
        Assert.Null(GetGeneratedSource(compilation, "MyNs.MyResolver.NoKeysCsvFormatter.g.cs"));
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

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
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

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // Resolver body (FormatterLookup) references both types.
        Assert.Contains("global::MyNs.TypeA", resolverSrc);
        Assert.Contains("global::MyNs.TypeB", resolverSrc);
        // Both formatter class names appear in the lookup table.
        Assert.Contains("TypeACsvFormatter", resolverSrc);
        Assert.Contains("TypeBCsvFormatter", resolverSrc);
        // Each formatter is in its own file.
        Assert.NotNull(GetGeneratedSource(compilation, "MyNs.MyResolver.TypeACsvFormatter.g.cs"));
        Assert.NotNull(GetGeneratedSource(compilation, "MyNs.MyResolver.TypeBCsvFormatter.g.cs"));
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

        // Resolver body is in a PascalCase file with no namespace prefix (global namespace).
        var resolverSrc = GetGeneratedSource(compilation, "GlobalResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("partial class GlobalResolver", resolverSrc);
        // Formatter reference appears in the FormatterLookup table.
        Assert.Contains("TopLevelTypeCsvFormatter", resolverSrc);
        // Formatter is in its own file.
        Assert.NotNull(GetGeneratedSource(compilation, "GlobalResolver.TopLevelTypeCsvFormatter.g.cs"));
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

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("#if !DISABLE_CSVTRANSCODER_MESSAGEPACK", resolverSrc);
        Assert.Contains("#endif", resolverSrc);
        // Resolver body references FooCsvFormatter in FormatterLookup.
        Assert.Contains("FooCsvFormatter", resolverSrc);
        // Formatter file is also guarded.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.FooCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("#if !DISABLE_CSVTRANSCODER_MESSAGEPACK", fmtSrc);
        Assert.Contains("#endif", fmtSrc);
    }

    // -----------------------------------------------------------------------
    //  Member filtering
    // -----------------------------------------------------------------------

    [Fact]
    public void SkipsStaticMembers()
    {
        // Static members should be ignored even when decorated with [Key].
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class WithStatic
            {
                [Key(0)] public int Id { get; set; }
                [Key(1)] public static int StaticProp { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Only the non-static Id (key 0) should appear; array size should be 1.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.WithStaticCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
    }

    [Fact]
    public void SkipsNonPublicMembers()
    {
        // Private/internal members must be excluded.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class WithPrivate
            {
                [Key(0)] public int Id { get; set; }
                [Key(1)] private string Secret { get; set; } = "";
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Private member is excluded; only the public Id (key 0) should be in the formatter.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.WithPrivateCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
    }

    [Fact]
    public void SkipsStringKeyMembers()
    {
        // String-keyed members are map-mode and must be skipped; if ALL keys are
        // strings the type should produce no formatter at all.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class StringKeyed
            {
                [Key("id")] public int Id { get; set; }
                [Key("name")] public string Name { get; set; } = "";
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // All members have string keys → no valid int-key members → no formatter generated.
        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.DoesNotContain("StringKeyedCsvFormatter", resolverSrc);
        Assert.Null(GetGeneratedSource(compilation, "MyNs.MyResolver.StringKeyedCsvFormatter.g.cs"));
    }

    [Fact]
    public void SkipsStringKeyMembers_MixedWithIntKeys()
    {
        // Only int-keyed members contribute to the formatter; string-keyed members are skipped.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class MixedKeys
            {
                [Key(0)] public int Id { get; set; }
                [Key("name")] public string Name { get; set; } = "";
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // FormatterLookup references MixedKeysCsvFormatter.
        Assert.Contains("MixedKeysCsvFormatter", resolverSrc);
        // Only the int-keyed Id member should be in the formatter.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.MixedKeysCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
        // The string-keyed "Name" member must not cause a formatter call.
        Assert.Contains("GetFormatterWithVerify<int>", fmtSrc);
        Assert.DoesNotContain("GetFormatterWithVerify<string>", fmtSrc);
    }

    [Fact]
    public void GeneratesFormatter_ForPublicField()
    {
        // Public fields (not just properties) should be picked up by the generator.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class WithField
            {
                [Key(0)] public int Value;
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("WithFieldCsvFormatter", resolverSrc);
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.WithFieldCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
        Assert.Contains("GetFormatterWithVerify<int>", fmtSrc);
    }

    [Fact]
    public void GeneratedResolver_WithNoFormattableTypes()
    {
        // A [GeneratedCsvFormatterResolver] class with no valid [MessagePackObject]
        // types in the compilation should still produce a resolver (with an empty lookup).
        const string source = """
            using AndanteTribe.Csv;
            namespace MyNs;
            [GeneratedCsvFormatterResolver]
            public partial class EmptyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.EmptyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("partial class EmptyResolver", resolverSrc);
        Assert.Contains("return null;", resolverSrc);
    }

    // -----------------------------------------------------------------------
    //  Struct support
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesFormatter_ForReadonlyRecordStruct()
    {
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public readonly record struct MyStruct(
                [property: Key(0)] int X,
                [property: Key(1)] int Y);
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("MyStructCsvFormatter", resolverSrc);
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.MyStructCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("ICsvFormatter<global::MyNs.MyStruct>", fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(2)", fmtSrc);
    }

    // -----------------------------------------------------------------------
    //  Naming convention: MessagePack-style nested partial class hierarchy
    // -----------------------------------------------------------------------

    [Fact]
    public void GeneratesFormatter_NestedInsideResolverWithNamespaceHierarchy()
    {
        // Formatters must be nested inside the resolver using internal partial classes
        // that mirror the type's namespace — the same pattern as MessagePack-CSharp's generator.
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

        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.FooCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        // The namespace 'MyNs' should appear as an internal partial class wrapper inside resolver
        Assert.Contains("internal partial class MyNs", fmtSrc);
        // The formatter class is internal sealed
        Assert.Contains("internal sealed class FooCsvFormatter", fmtSrc);
        // Uses simple type name (not flat underscore-separated name)
        Assert.DoesNotContain("MyNs_FooCsvFormatter", fmtSrc);
    }

    [Fact]
    public void GeneratesFormatter_FormatterLookup_IsPrivateInsideResolver()
    {
        // The FormatterLookup class must be private inside the resolver — no global
        // AndanteTribe.Csv.Generated namespace collision possible.
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

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("private static class FormatterLookup", resolverSrc);
        Assert.DoesNotContain("namespace AndanteTribe.Csv.Generated", resolverSrc);
    }

    // -----------------------------------------------------------------------
    //  Code-review fixes (accessibility, duplicate keys, negative keys, etc.)
    // -----------------------------------------------------------------------

    [Fact]
    public void SkipsNonPublicMessagePackObjectType()
    {
        // Private nested types should produce no formatter.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            public class Outer
            {
                [MessagePackObject]
                private class PrivateInner
                {
                    [Key(0)] public int Id { get; set; }
                }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.DoesNotContain("PrivateInnerCsvFormatter", resolverSrc);
        Assert.Null(GetGeneratedSource(compilation, "MyNs.MyResolver.PrivateInnerCsvFormatter.g.cs"));
    }

    [Fact]
    public void GeneratesFormatter_ForInternalType()
    {
        // Internal [MessagePackObject] types should produce a formatter — always internal sealed.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            internal class InternalFoo
            {
                [Key(0)] public int Id { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // Resolver body references InternalFooCsvFormatter in FormatterLookup.
        Assert.Contains("InternalFooCsvFormatter", resolverSrc);
        // Formatter class definition is in the formatter-specific file.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.InternalFooCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("internal sealed class InternalFooCsvFormatter", fmtSrc);
    }

    [Fact]
    public void SkipsNegativeKeyMembers()
    {
        // Members decorated with a negative [Key] value are skipped.
        // MessagePack's own analyzer already warns about negative keys.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class WithNegativeKey
            {
                [Key(0)] public int Valid { get; set; }
                [Key(-1)] public int Invalid { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        // Only key 0 (Valid) should appear; array size should be 1.
        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.WithNegativeKeyCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
    }

    [Fact]
    public void HandlesDuplicateKeys_DoesNotThrow()
    {
        // When two members share the same integer key, the generator must not throw.
        // The first member in declaration order wins; the duplicate is silently dropped.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class WithDuplicateKey
            {
                [Key(0)] public int First { get; set; }
                [Key(0)] public int Second { get; set; }
            }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        // Must not produce generator errors (no exception propagated as diagnostics).
        Assert.DoesNotContain(diagnostics, d => d.Severity == DiagnosticSeverity.Error
            && d.GetMessage().Contains("Exception"));

        var fmtSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.WithDuplicateKeyCsvFormatter.g.cs");
        Assert.NotNull(fmtSrc);
        Assert.Contains("writer.WriteArrayHeader(1)", fmtSrc);
    }

    [Fact]
    public void SkipsGenericResolverClass()
    {
        // A generic [GeneratedCsvFormatterResolver] class must be ignored:
        // the generator cannot emit a non-generic partial matching a generic declaration.
        // Since no valid user-defined resolver exists, a default GeneratedCsvFormatterResolver
        // is emitted in the AndanteTribe.Csv namespace instead.
        const string source = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace MyNs;
            [MessagePackObject]
            public class Foo { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class GenericResolver<T> { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // No resolver should be generated for the generic class.
        Assert.Null(GetGeneratedSource(compilation, "MyNs.GenericResolver.g.cs"));
        // A default resolver is generated in AndanteTribe.Csv instead.
        Assert.NotNull(GetGeneratedSource(compilation, "AndanteTribe.Csv.GeneratedCsvFormatterResolver.g.cs"));
    }

    [Fact]
    public void GeneratesDefaultResolver_WhenNoUserDefinedResolverExists()
    {
        // When no [GeneratedCsvFormatterResolver] class is present, the generator should
        // automatically emit a default GeneratedCsvFormatterResolver in the AndanteTribe.Csv
        // namespace — mirroring MessagePack's GeneratedMessagePackResolver fallback behavior.
        const string source = """
            using MessagePack;
            namespace MyNs;
            [MessagePackObject]
            public class Foo { [Key(0)] public int Id { get; set; } }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(source);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));
        // Default resolver is emitted in the AndanteTribe.Csv namespace.
        var resolverSrc = GetGeneratedSource(compilation, "AndanteTribe.Csv.GeneratedCsvFormatterResolver.g.cs");
        Assert.NotNull(resolverSrc);
        Assert.Contains("public partial class GeneratedCsvFormatterResolver", resolverSrc);
        Assert.Contains("ICsvFormatterResolver", resolverSrc);
        Assert.Contains("FooCsvFormatter", resolverSrc);
        // FooCsvFormatter must be registered in the FormatterLookup table.
        Assert.Contains("typeof(global::MyNs.Foo)", resolverSrc);
        // Formatter is in its own file, nested inside the default resolver.
        Assert.NotNull(GetGeneratedSource(compilation, "AndanteTribe.Csv.GeneratedCsvFormatterResolver.FooCsvFormatter.g.cs"));
    }

    [Fact]
    public void GeneratedResolver_IncludesAccessibilityModifier()
    {
        // The generated partial class must include the same accessibility as the user declaration.
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

        var resolverSrc = GetGeneratedSource(compilation, "MyNs.MyResolver.g.cs");
        Assert.NotNull(resolverSrc);
        // The generated partial declaration must carry the accessibility modifier.
        Assert.Contains("public partial class MyResolver", resolverSrc);
    }

    [Fact]
    public void GeneratedResolver_EachHasPrivateFormatterLookup_NoCollision()
    {
        // Two resolvers with the same class name in different namespaces must each produce
        // a self-contained file. Since FormatterLookup is now private inside each resolver,
        // there is no naming collision in a shared namespace.
        const string sourceA = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace NsA;
            [MessagePackObject]
            public class Foo { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        const string sourceB = """
            using MessagePack;
            using AndanteTribe.Csv;
            namespace NsB;
            [MessagePackObject]
            public class Bar { [Key(0)] public int Id { get; set; } }
            [GeneratedCsvFormatterResolver]
            public partial class MyResolver { }
            """;

        var (compilation, diagnostics) = GeneratorTestHelper.RunGenerator(sourceA, sourceB);

        Assert.Empty(diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var resolverA = GetGeneratedSource(compilation, "NsA.MyResolver.g.cs");
        var resolverB = GetGeneratedSource(compilation, "NsB.MyResolver.g.cs");
        Assert.NotNull(resolverA);
        Assert.NotNull(resolverB);
        // Each resolver's FormatterLookup is private and independent — no collision.
        Assert.Contains("private static class FormatterLookup", resolverA);
        Assert.Contains("private static class FormatterLookup", resolverB);
        // Each resolver references only its own type.
        Assert.Contains("global::NsA.Foo", resolverA);
        Assert.Contains("global::NsB.Bar", resolverB);
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
