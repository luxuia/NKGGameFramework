using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Diagnostics
{

    public interface IGameDebugComponentValueSerializer
    {
        ComponentValueDebugSnapshot Serialize(
            object value,
            GameDebugComponentValueSerializationOptions? options = null);

        object Deserialize(ComponentValueDebugSnapshot value, Type expectedType);
    }
}
