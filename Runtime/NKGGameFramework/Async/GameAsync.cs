using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NKGGameFramework.Core;

namespace NKGGameFramework.Async
{

    public static class GameAsync
    {
        public static UniTask CompletedTask => UniTask.CompletedTask;

        public static UniTask<T> FromResult<T>(T value) => UniTask.FromResult(value);

        public static UniTask FromException(Exception exception)
        {
            NkgThrow.IfNull(exception);
            return UniTask.FromException(exception);
        }

        public static UniTask<T> FromException<T>(Exception exception)
        {
            NkgThrow.IfNull(exception);
            return UniTask.FromException<T>(exception);
        }

        public static UniTask FromCanceled(CancellationToken cancellationToken) => UniTask.FromCanceled(cancellationToken);

        public static UniTask<T> FromCanceled<T>(CancellationToken cancellationToken) => UniTask.FromCanceled<T>(cancellationToken);

        public static UniTask WhenAll(params UniTask[] tasks) => UniTask.WhenAll(tasks);

        public static UniTask<T[]> WhenAll<T>(params UniTask<T>[] tasks) => UniTask.WhenAll(tasks);

        public static UniTask<int> WhenAny(params UniTask[] tasks) => UniTask.WhenAny(tasks);

        public static UniTask Delay(IGameTimer timer, TimeSpan delay, CancellationToken cancellationToken = default)
        {
            NkgThrow.IfNull(timer);
            return timer.DelayAsync(delay, cancellationToken);
        }

        public static UniTask Delay(
            IGameTimer timer,
            TimeSpan delay,
            TimerTimeMode timeMode,
            CancellationToken cancellationToken = default)
        {
            NkgThrow.IfNull(timer);
            return timer.DelayAsync(delay, timeMode, cancellationToken);
        }

        public static UniTask NextFrame(IGameTimer timer, CancellationToken cancellationToken = default)
        {
            NkgThrow.IfNull(timer);
            return timer.NextFrameAsync(cancellationToken);
        }

        public static UniTask DelayFrame(IGameTimer timer, int frameCount, CancellationToken cancellationToken = default)
        {
            NkgThrow.IfNull(timer);
            return timer.DelayFrameAsync(frameCount, cancellationToken);
        }
    }
}
