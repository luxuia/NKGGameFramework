using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace NKGGameFramework.Runtime
{

    public interface IEntityView
    {
        long ViewId { get; }
    }

    public interface IPresentationService
    {
        UniTask<IEntityView> BindAsync(object logicalEntity, string viewLocation, CancellationToken cancellationToken = default);
    }
}
