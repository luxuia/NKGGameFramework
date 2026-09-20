using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
namespace NKGGameFramework.Mathematics
{

    // Deterministic seeded RNG. The same seed yields the same stream on any
    // platform and .NET runtime because it uses only ulong arithmetic
    // (xorshift64*), never System.Random's unspecified algorithm.
    public sealed class DeterministicRandom
    {
        private const ulong DefaultSeed = 0x9E3779B97F4A7C15UL;
        private const ulong Multiplier = 0x2545F4914F6CDD1DUL;
        private const double Inverse53Bits = 1.0 / 9007199254740992.0; // 2^53

        private ulong _state;

        public DeterministicRandom(ulong seed = DefaultSeed)
        {
            _state = seed == 0 ? DefaultSeed : seed;
        }

        public ulong NextUInt64()
        {
            _state ^= _state >> 12;
            _state ^= _state << 25;
            _state ^= _state >> 27;
            return _state * Multiplier;
        }

        public long NextInt64() => (long)NextUInt64();

        public int NextInt(int maxExclusive)
        {
            if (maxExclusive <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be greater than zero.");
            }

            return (int)(NextUInt64() % (ulong)maxExclusive);
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be greater than lower bound.");
            }

            var span = (long)maxExclusive - minInclusive;
            return (int)(minInclusive + (long)(NextUInt64() % (ulong)span));
        }

        public double NextDouble()
        {
            // Top 53 bits of the word give a uniform value in [0, 1).
            return (NextUInt64() >> 11) * Inverse53Bits;
        }

        public double Range(double minInclusive, double maxExclusive)
        {
            if (maxExclusive <= minInclusive)
            {
                throw new ArgumentOutOfRangeException(nameof(maxExclusive), "Upper bound must be greater than lower bound.");
            }

            return minInclusive + NextDouble() * (maxExclusive - minInclusive);
        }

        public bool NextBool() => (NextUInt64() & 1) == 1;

        // Derives an independent stream, e.g. one per loot roll or proc source.
        public DeterministicRandom Fork(ulong salt = 0) => new(NextUInt64() ^ salt);
    }
}
