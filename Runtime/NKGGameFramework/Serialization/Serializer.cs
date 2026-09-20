using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Serialization
{

    public interface IGameSerializer
    {
        string Serialize<T>(T value);

        T? Deserialize<T>(string payload);
    }

    public interface IBinaryGameSerializer
    {
        byte[] SerializeToBytes<T>(T value);

        T? DeserializeFromBytes<T>(byte[] payload);
    }

    public interface IJsonGameSerializer
    {
        string SerializeToJson<T>(T value);

        T? DeserializeFromJson<T>(string payload);
    }
}
