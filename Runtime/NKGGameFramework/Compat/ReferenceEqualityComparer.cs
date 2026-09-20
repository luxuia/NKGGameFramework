using System.Runtime.CompilerServices;

namespace System.Collections.Generic
{
    /// <summary>
    /// .NET 5+ ReferenceEqualityComparer 的 netstandard2.1 backport（按引用相等比较）。
    /// 放在 System.Collections.Generic 命名空间以便使用方免 using 直呼类型名。
    /// </summary>
    public sealed class ReferenceEqualityComparer : IEqualityComparer<object?>
    {
        public static readonly ReferenceEqualityComparer Instance = new();

        private ReferenceEqualityComparer()
        {
        }

        public bool Equals(object? x, object? y) => ReferenceEquals(x, y);

        public int GetHashCode(object? obj) => RuntimeHelpers.GetHashCode(obj);
    }
}
