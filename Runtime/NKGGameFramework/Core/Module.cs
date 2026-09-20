using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Core
{

    public abstract class Module
    {
        public virtual int Priority => 0;

        public bool IsInitialized { get; private set; }

        public void Initialize(IRuntimeContext context)
        {
            NkgThrow.IfNull(context);

            if (IsInitialized)
            {
                return;
            }

            OnInitialize(context);
            IsInitialized = true;
        }

        public void Shutdown()
        {
            if (!IsInitialized)
            {
                return;
            }

            OnShutdown();
            IsInitialized = false;
        }

        protected virtual void OnInitialize(IRuntimeContext context)
        {
        }

        protected virtual void OnShutdown()
        {
        }
    }

    public interface IUpdateModule
    {
        void Update(in GameFrameTime time);

        void Update(double deltaTime, double realDeltaTime)
        {
            var time = GameFrameTime.FromSeconds(deltaTime, realDeltaTime);
            Update(in time);
        }
    }
}
