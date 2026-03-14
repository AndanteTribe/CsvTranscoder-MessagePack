namespace AndanteTribe.Csv;

/// <summary>
/// Represents a collection of <see cref="ICsvFormatterResolver"/> instances acting as one.
/// </summary>
public static class CompositeResolver
{
    /// <inheritdoc cref="Create(ICsvFormatterResolver[])"/>
    public static ICsvFormatterResolver Create(
        ICsvFormatterResolver resolver1,
        ICsvFormatterResolver resolver2)
    {
        if (resolver1 is null)
        {
            throw new ArgumentNullException(nameof(resolver1));
        }

        if (resolver2 is null)
        {
            throw new ArgumentNullException(nameof(resolver2));
        }

        return new Resolver2(resolver1, resolver2);
    }

    /// <inheritdoc cref="Create(ICsvFormatterResolver[])"/>
    public static ICsvFormatterResolver Create(
        ICsvFormatterResolver resolver1,
        ICsvFormatterResolver resolver2,
        ICsvFormatterResolver resolver3)
    {
        if (resolver1 is null)
        {
            throw new ArgumentNullException(nameof(resolver1));
        }

        if (resolver2 is null)
        {
            throw new ArgumentNullException(nameof(resolver2));
        }

        if (resolver3 is null)
        {
            throw new ArgumentNullException(nameof(resolver3));
        }

        return new Resolver3(resolver1, resolver2, resolver3);
    }

    /// <inheritdoc cref="Create(ICsvFormatterResolver[])"/>
    public static ICsvFormatterResolver Create(
        ICsvFormatterResolver resolver1,
        ICsvFormatterResolver resolver2,
        ICsvFormatterResolver resolver3,
        ICsvFormatterResolver resolver4)
    {
        if (resolver1 is null)
        {
            throw new ArgumentNullException(nameof(resolver1));
        }

        if (resolver2 is null)
        {
            throw new ArgumentNullException(nameof(resolver2));
        }

        if (resolver3 is null)
        {
            throw new ArgumentNullException(nameof(resolver3));
        }

        if (resolver4 is null)
        {
            throw new ArgumentNullException(nameof(resolver4));
        }

        return new Resolver4(resolver1, resolver2, resolver3, resolver4);
    }

    /// <inheritdoc cref="Create(ICsvFormatterResolver[])"/>
    public static ICsvFormatterResolver Create(
        ICsvFormatterResolver resolver1,
        ICsvFormatterResolver resolver2,
        ICsvFormatterResolver resolver3,
        ICsvFormatterResolver resolver4,
        ICsvFormatterResolver resolver5)
    {
        if (resolver1 is null)
        {
            throw new ArgumentNullException(nameof(resolver1));
        }

        if (resolver2 is null)
        {
            throw new ArgumentNullException(nameof(resolver2));
        }

        if (resolver3 is null)
        {
            throw new ArgumentNullException(nameof(resolver3));
        }

        if (resolver4 is null)
        {
            throw new ArgumentNullException(nameof(resolver4));
        }

        if (resolver5 is null)
        {
            throw new ArgumentNullException(nameof(resolver5));
        }

        return new Resolver5(resolver1, resolver2, resolver3, resolver4, resolver5);
    }

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
        if (resolvers is null)
        {
            throw new ArgumentNullException(nameof(resolvers));
        }

        for (var i = 0; i < resolvers.Length; i++)
        {
            if (resolvers[i] is null)
            {
                throw new ArgumentNullException($"{nameof(resolvers)}[{i}]");
            }
        }

        return new ResolverN(resolvers);
    }

    private sealed class Resolver2(
        ICsvFormatterResolver r1,
        ICsvFormatterResolver r2) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
            => r1.GetFormatter<T>() ?? r2.GetFormatter<T>();
    }

    private sealed class Resolver3(
        ICsvFormatterResolver r1,
        ICsvFormatterResolver r2,
        ICsvFormatterResolver r3) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
            => r1.GetFormatter<T>() ?? r2.GetFormatter<T>() ?? r3.GetFormatter<T>();
    }

    private sealed class Resolver4(
        ICsvFormatterResolver r1,
        ICsvFormatterResolver r2,
        ICsvFormatterResolver r3,
        ICsvFormatterResolver r4) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
            => r1.GetFormatter<T>() ?? r2.GetFormatter<T>() ?? r3.GetFormatter<T>() ?? r4.GetFormatter<T>();
    }

    private sealed class Resolver5(
        ICsvFormatterResolver r1,
        ICsvFormatterResolver r2,
        ICsvFormatterResolver r3,
        ICsvFormatterResolver r4,
        ICsvFormatterResolver r5) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
            => r1.GetFormatter<T>() ?? r2.GetFormatter<T>() ?? r3.GetFormatter<T>() ?? r4.GetFormatter<T>() ?? r5.GetFormatter<T>();
    }

    private sealed class ResolverN(ICsvFormatterResolver[] resolvers) : ICsvFormatterResolver
    {
        public ICsvFormatter<T>? GetFormatter<T>()
        {
            foreach (var resolver in resolvers)
            {
                var f = resolver.GetFormatter<T>();
                if (f is not null)
                {
                    return f;
                }
            }

            return null;
        }
    }
}
