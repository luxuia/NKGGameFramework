using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace NKGGameFramework.Runtime
{

    public interface IConfigService
    {
        UniTask<TConfig> LoadAsync<TConfig>(string key, CancellationToken cancellationToken = default)
            where TConfig : class;
    }
}
