using AndanteTribe.Csv.Formatters;
using GameKernel;

namespace AndanteTribe.Csv;

/// <summary>
/// An <see cref="ICsvFormatterResolver"/> that provides CSV formatters for
/// <see cref="GameKernel"/> types: <see cref="MasterId{TGroup}"/> and <see cref="Obscured{T}"/>.
/// </summary>
public sealed class GameKernelCsvResolver : ICsvFormatterResolver
{
    public static readonly GameKernelCsvResolver Instance = new();

    private static readonly RuntimeTypeHandle s_masterIdHandle = typeof(MasterId<>).TypeHandle;
    private static readonly RuntimeTypeHandle s_obscuredHandle = typeof(Obscured<>).TypeHandle;

    private GameKernelCsvResolver() { }

    private static class Cache<T>
    {
        public static readonly ICsvFormatter<T>? Value = GetFormatter(typeof(T)) as ICsvFormatter<T>;
    }

    public ICsvFormatter<T>? GetFormatter<T>() => Cache<T>.Value;

    private static object? GetFormatter(Type t)
    {
        if (!t.IsGenericType)
        {
            return null;
        }

        var defHandle = t.GetGenericTypeDefinition().TypeHandle;
        var args = t.GetGenericArguments();

        if (defHandle.Equals(s_masterIdHandle))
        {
            return Activator.CreateInstance(typeof(MasterIdCsvFormatter<>).MakeGenericType(args));
        }

        if (defHandle.Equals(s_obscuredHandle))
        {
            return Activator.CreateInstance(typeof(ObscuredCsvFormatter<>).MakeGenericType(args));
        }

        return null;
    }
}
