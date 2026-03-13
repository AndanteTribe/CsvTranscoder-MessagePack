using System.Collections.Concurrent;

namespace AndanteTribe.Csv;

/// <summary>
/// Represents a collection of <see cref="ICsvFormatterResolver"/> instances acting as one.
/// </summary>
public static class CompositeResolver
{
    /// <summary>
    /// Creates a composite resolver that searches the given resolvers in order,
    /// returning the formatter found by the first resolver that supports the type.
    /// </summary>
    /// <param name="resolvers">
    /// A list of resolvers to search in the order given.
    /// If two resolvers support the same type, the first one is used.
    /// </param>
    /// <returns>A single <see cref="ICsvFormatterResolver"/> that delegates to all provided resolvers.</returns>
    public static ICsvFormatterResolver Create(params ICsvFormatterResolver[] resolvers)
    {
        if (resolvers is null) throw new ArgumentNullException(nameof(resolvers));
        return new CachingResolver(resolvers.ToArray());
    }

    private sealed class CachingResolver : ICsvFormatterResolver
    {
        private readonly ConcurrentDictionary<Type, object?> _formattersCache = new();
        private readonly ICsvFormatterResolver[] _resolvers;

        internal CachingResolver(ICsvFormatterResolver[] resolvers)
        {
            _resolvers = resolvers;
        }

        public ICsvFormatter<T>? GetFormatter<T>()
        {
            return (ICsvFormatter<T>?)_formattersCache.GetOrAdd(typeof(T), static (_, resolvers) =>
            {
                foreach (var resolver in resolvers)
                {
                    var f = resolver.GetFormatter<T>();
                    if (f is not null)
                        return f;
                }

                return null;
            }, _resolvers);
        }
    }
}
