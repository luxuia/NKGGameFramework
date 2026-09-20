namespace System.Runtime.CompilerServices
{
    // Unity 2022.3 / netstandard2.1 不自带 IsExternalInit；
    // init/record 编译需要它。每个含 init 语法的程序集各放一份（internal）。
    internal sealed class IsExternalInit
    {
    }
}
