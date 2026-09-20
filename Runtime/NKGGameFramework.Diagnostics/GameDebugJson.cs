using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace NKGGameFramework.Diagnostics
{
    /// <summary>
    /// JSON 序列化配置。原版基于 System.Text.Json JsonSerializerDefaults.Web
    /// （camelCase 属性名 + 大小写不敏感反序列化），此处用 Newtonsoft 等价配置对齐。
    /// </summary>
    public static class GameDebugJson
    {
        public static readonly JsonSerializerSettings Options = new()
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver(),
        };
    }
}
