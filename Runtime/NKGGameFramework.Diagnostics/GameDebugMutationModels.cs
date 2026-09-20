using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Diagnostics
{

    public sealed record GameDebugMutationRequest(
        string WorldName,
        string SceneName,
        int EntityId,
        int? EntityVersion,
        string ComponentTypeFullName,
        string ComponentAssemblyName,
        ComponentValueDebugSnapshot Value);

    public sealed record GameDebugMutationResult(
        bool Succeeded,
        string Message);
}
