using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;

namespace NKGGameFramework.Runtime
{

    public interface IAudioHandle : IDisposable
    {
        string Location { get; }
    }

    public interface IAudioService
    {
        UniTask<IAudioHandle> PlayAsync(string location, AudioPlaybackOptions options = default, CancellationToken cancellationToken = default);
    }

    public readonly struct AudioPlaybackOptions : IEquatable<AudioPlaybackOptions>
    {
        public readonly float Volume;
        public readonly bool Loop;

        public AudioPlaybackOptions(float Volume = 1f, bool Loop = false)
        {
            this.Volume = Volume;
            this.Loop = Loop;
        }

        public bool Equals(AudioPlaybackOptions other) => EqualityComparer<float>.Default.Equals(Volume, other.Volume) && EqualityComparer<bool>.Default.Equals(Loop, other.Loop);

        public override int GetHashCode() => HashCode.Combine(Volume, Loop);

        public override bool Equals(object? obj) => obj is AudioPlaybackOptions other && Equals(other);

        public static bool operator ==(AudioPlaybackOptions left, AudioPlaybackOptions right) => left.Equals(right);
        public static bool operator !=(AudioPlaybackOptions left, AudioPlaybackOptions right) => !left.Equals(right);

        public void Deconstruct(out float volume, out bool loop)
        {
            volume = Volume; loop = Loop;
        }
    }
}
