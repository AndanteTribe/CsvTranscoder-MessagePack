using System.Runtime.CompilerServices;

namespace AndanteTribe.Csv;

public interface ICsvFormatterResolver
{
    ICsvFormatter<T>? GetFormatter<T>();
}

public static class FormatterResolverExtensions
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ICsvFormatter<T> GetFormatterWithVerify<T>(this ICsvFormatterResolver resolver)
    {
        var formatter = resolver.GetFormatter<T>();
        return formatter ?? throw new InvalidOperationException($"Formatter for type {typeof(T)} is not found in resolver {resolver.GetType()}.");
    }
}