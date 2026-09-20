using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Diagnostics
{

    public interface IGameDebugSnapshotProvider
    {
        GameDebugSnapshot Capture(GameDebugSnapshotCaptureOptions? options = null);
    }
}
