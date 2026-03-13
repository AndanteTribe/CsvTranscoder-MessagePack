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
        public static ICsvFormatter<T>? Value;
    }

    public ICsvFormatter<T>? GetFormatter<T>()
    {
        if (Cache<T>.Value is not null)
        {
            return Cache<T>.Value;
        }

        if (typeof(T).IsGenericType)
        {
            var defHandle = typeof(T).GetGenericTypeDefinition().TypeHandle;
            var args = typeof(T).GetGenericArguments();

            if (defHandle.Equals(s_masterIdHandle))
            {
                Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(
                    typeof(MasterIdCsvFormatter<>).MakeGenericType(args))!;
                return Cache<T>.Value;
            }

            if (defHandle.Equals(s_obscuredHandle))
            {
                Cache<T>.Value = (ICsvFormatter<T>)Activator.CreateInstance(
                    typeof(ObscuredCsvFormatter<>).MakeGenericType(args))!;
                return Cache<T>.Value;
            }
        }

        return null;
    }
}
