namespace System.Collections.Generic
{
    /// <summary>
    /// .NET 5+ IReadOnlySet&lt;T&gt; 的 netstandard2.1 backport（只读集合接口）。
    /// 注意：netstandard2.1 下 HashSet&lt;T&gt; 并不实现该接口，仅用于 typeof 形态检查等编译用途。
    /// </summary>
    public interface IReadOnlySet<T> : IReadOnlyCollection<T>
    {
        bool Contains(T item);

        bool IsProperSubsetOf(IEnumerable<T> other);

        bool IsProperSupersetOf(IEnumerable<T> other);

        bool IsSubsetOf(IEnumerable<T> other);

        bool IsSupersetOf(IEnumerable<T> other);

        bool Overlaps(IEnumerable<T> other);

        bool SetEquals(IEnumerable<T> other);
    }
}
