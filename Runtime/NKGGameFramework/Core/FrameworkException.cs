using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Core
{

    public sealed class FrameworkException : Exception
    {
        public FrameworkException(string message)
            : base(message)
        {
        }

        public FrameworkException(string message, Exception innerException)
            : base(message, innerException)
        {
        }
    }

}
