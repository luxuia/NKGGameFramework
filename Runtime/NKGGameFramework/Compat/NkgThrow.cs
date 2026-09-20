using System;
using System.Runtime.CompilerServices;

namespace NKGGameFramework
{
    /// <summary>
    /// .NET 6+ ArgumentNullException.ThrowIfNull / ArgumentException.ThrowIfNullOrWhiteSpace
    /// 在 netstandard2.1 下的等价垫片。放在父命名空间 NKGGameFramework，
    /// 所有 NKGGameFramework.* 代码无需额外 using。
    /// </summary>
    public static class NkgThrow
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull(object? argument)
        {
            if (argument is null)
            {
                throw new ArgumentNullException();
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNull(object? argument, string paramName)
        {
            if (argument is null)
            {
                throw new ArgumentNullException(paramName);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNullOrWhiteSpace(string? argument)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("The argument cannot be null, empty, or consist only of white-space characters.");
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static void IfNullOrWhiteSpace(string? argument, string paramName)
        {
            if (string.IsNullOrWhiteSpace(argument))
            {
                throw new ArgumentException("The argument cannot be null, empty, or consist only of white-space characters.", paramName);
            }
        }
    }
}
