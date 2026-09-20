using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Reflection;
using OdinSerializer;

namespace NKGGameFramework.Diagnostics
{

    internal static class GameDebugOdinSerializationPolicy
    {
        public static ISerializationPolicy Instance => GameDebugOdinSerialization.Policy;

        internal static bool ShouldSerializeMember(MemberInfo member)
        {
            return GameDebugOdinSerialization.ShouldSerializeMember(member);
        }

        internal static bool TryGetAutoProperty(FieldInfo field, out PropertyInfo? property)
        {
            return GameDebugOdinSerialization.TryGetAutoProperty(field, out property);
        }
    }
}
