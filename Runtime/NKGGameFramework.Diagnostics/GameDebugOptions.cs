using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Diagnostics
{

    public sealed class GameDebugOptions
    {
        public bool EnableMutations { get; set; }

        public string DumpDirectory { get; set; } = Path.Combine(
            Path.GetTempPath(),
            "NKGGameFramework",
            "debug-dumps");
    }
}
