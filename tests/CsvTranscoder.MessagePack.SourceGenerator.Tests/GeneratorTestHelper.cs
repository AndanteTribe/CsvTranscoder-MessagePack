using System.Collections.Immutable;
using AndanteTribe.Csv.SourceGenerator;
using MessagePack;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System.Text;

namespace CsvTranscoder.MessagePack.SourceGenerator.Tests;

/// <summary>Utility for running <see cref="CsvFormatterGenerator"/> in unit tests.</summary>
internal static class GeneratorTestHelper
{
    // Anchor on a known MessagePack annotation type to get the MessagePack.Annotations assembly.
    private static readonly string s_messagePackAnnotationsAssemblyPath =
        typeof(MessagePackObjectAttribute).Assembly.Location;

    // Anchor on MessagePackWriter to get the main MessagePack assembly (separate from Annotations).
    private static readonly string s_messagePackCoreAssemblyPath =
        typeof(global::MessagePack.MessagePackWriter).Assembly.Location;

    // Anchor on a known CsvTranscoder type to get the assembly location reliably.
    private static readonly string s_csvTranscoderAssemblyPath =
        typeof(AndanteTribe.Csv.ICsvFormatter<>).Assembly.Location;

    /// <summary>
    /// Runs the <see cref="CsvFormatterGenerator"/> against the supplied source code snippets
    /// and returns the resulting compilation (with generated sources included) and diagnostics.
    /// </summary>
    public static (Compilation Output, ImmutableArray<Diagnostic> Diagnostics) RunGenerator(
        params string[] sources)
    {
        // Build a CSharpCompilation from the provided source snippets.
        var syntaxTrees = sources
            .Select((src, i) => CSharpSyntaxTree.ParseText(
                SourceText.From(src, Encoding.UTF8),
                path: $"Input{i}.cs"))
            .ToArray();

        var seenPaths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        var references = new List<MetadataReference>();

        void TryAdd(string path)
        {
            if (!string.IsNullOrEmpty(path) && seenPaths.Add(path))
                references.Add(MetadataReference.CreateFromFile(path));
        }

        TryAdd(typeof(object).Assembly.Location);
        TryAdd(typeof(Console).Assembly.Location);
        TryAdd(s_messagePackAnnotationsAssemblyPath);
        TryAdd(s_messagePackCoreAssemblyPath);
        TryAdd(s_csvTranscoderAssemblyPath);

        // Add all currently-loaded assemblies so the compilation has a full BCL.
        foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
        {
            if (!asm.IsDynamic && !string.IsNullOrEmpty(asm.Location))
                TryAdd(asm.Location);
        }

        var compilation = CSharpCompilation.Create(
            "TestAssembly",
            syntaxTrees,
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = new CsvFormatterGenerator();
        var driver = CSharpGeneratorDriver
            .Create(generator)
            .RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var generatorDiagnostics);

        // Merge generator diagnostics with compilation diagnostics so tests catch both
        // code-generation failures and compile-time errors in the generated output.
        // Filter to errors and warnings only to avoid expensive enumeration of all hints.
        var allDiagnostics = generatorDiagnostics.AddRange(
            outputCompilation.GetDiagnostics().Where(
                d => d.Severity >= DiagnosticSeverity.Warning));

        return (outputCompilation, allDiagnostics);
    }
}
