using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Runtime
{

    public interface ILocalizationService
    {
        string CurrentCulture { get; }

        string GetText(string key);
    }

}
