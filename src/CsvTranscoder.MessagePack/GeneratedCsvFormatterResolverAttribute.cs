using System;
using System.Diagnostics;

namespace AndanteTribe.Csv;

/// <summary>
/// An attribute to apply to a <see langword="partial" /> <see langword="class" /> that will serve as the
/// source-generated <see cref="ICsvFormatterResolver"/> for CSV transcoding.
/// The generator produces <see cref="ICsvFormatter{T}"/> implementations for every
/// <see cref="MessagePack.MessagePackObjectAttribute"/>-annotated type found in the current compilation
/// and registers them in the resolver.
/// </summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
[Conditional("NEVERDEFINED")]
public sealed class GeneratedCsvFormatterResolverAttribute : Attribute
{
}
