using System;
using OpenPerch.App.Core;

namespace OpenPerch.App.Tests.Shared
{
    /// <summary>Replays a fixed sequence of [0, 1) values, cycling when it runs out.</summary>
    public sealed class FakeRandom : IRandom
    {
        private readonly float[] _sequence;
        private int _next;

        public FakeRandom(params float[] sequence)
        {
            if (sequence == null || sequence.Length == 0) throw new ArgumentException("At least one value is required.", nameof(sequence));
            foreach (var v in sequence)
            {
                if (!(v >= 0f && v < 1f)) throw new ArgumentOutOfRangeException(nameof(sequence), v, "Values must be in [0, 1).");
            }
            _sequence = sequence;
        }

        public int Calls { get; private set; }

        public float Next01()
        {
            var v = _sequence[_next];
            _next = (_next + 1) % _sequence.Length;
            Calls++;
            return v;
        }

        public int Range(int minInclusive, int maxExclusive)
        {
            if (maxExclusive <= minInclusive) throw new ArgumentException("maxExclusive must be greater than minInclusive.");
            return minInclusive + (int)(Next01() * (maxExclusive - minInclusive));
        }
    }
}
