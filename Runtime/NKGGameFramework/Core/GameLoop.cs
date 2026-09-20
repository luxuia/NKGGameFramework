using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Core
{

    public interface IGameLoop
    {
        void Update(in GameFrameTime time);

        void Update(double deltaTime, double realDeltaTime)
        {
            var time = GameFrameTime.FromSeconds(deltaTime, realDeltaTime);
            Update(in time);
        }
    }
}
