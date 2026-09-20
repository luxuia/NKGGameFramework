using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Diagnostics
{

    internal static class DebugSnapshotTypeNames
    {
        public static DebugTypeInfo Create(Type type)
        {
            return new DebugTypeInfo(
                type.Name,
                type.FullName ?? type.Name,
                type.Assembly.GetName().Name ?? string.Empty);
        }
    }
}
