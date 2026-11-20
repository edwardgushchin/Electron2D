using System.Diagnostics;
using System.Numerics;

namespace Electron2D;

// PCG32 core adapted from M.E. O'Neill's 2014 Apache-2.0 implementation.
// See licence/PCG32-LICENSE.txt for its notice and license.
internal struct Pcg32
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong DefaultIncrement = 1442695040888963407UL;
    private const ulong DefaultSeed = 12047754176567800795UL;

    private ulong _state;
    private ulong _seed;

    internal Pcg32(ulong seed) => SetSeed(seed);

    internal ulong Seed => _seed;
    internal ulong State { readonly get => _state; set => _state = value; }

    internal static Pcg32 CreateRandomized()
    {
        var stream = new Pcg32(DefaultSeed);
        stream.Randomize();
        return stream;
    }

    internal void Randomize()
    {
        var seconds = unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
        var microseconds = unchecked((ulong)((double)Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency));
        SetSeed(unchecked((seconds + microseconds) * _state + DefaultIncrement));
    }

    internal int NextIntRange(int from, int to)
    {
        if (from == to)
        {
            return from;
        }

        var min = Math.Min((long)from, to);
        var max = Math.Max((long)from, to);
        var difference = (uint)(max - min);
        return (int)(min + (difference == uint.MaxValue ? Next() : Bounded(difference + 1)));
    }

    internal float NextFloatNormal(float mean, float deviation)
    {
        var sample = NextFloat();
        if (sample < 0.00001f)
        {
            sample += 0.00001f;
        }

        return mean + deviation * (Mathf.Cos(Mathf.Tau * NextFloat()) * Mathf.Sqrt(-2f * Mathf.Log(sample)));
    }

    internal void SetSeed(ulong value)
    {
        _seed = value;
        _state = 0;
        _ = Next();
        _state = unchecked(_state + value);
        _ = Next();
    }

    internal uint Next()
    {
        var old = _state;
        _state = unchecked(old * Multiplier + ((DefaultIncrement << 1) | 1));
        var shifted = (uint)(((old >> 18) ^ old) >> 27);
        return BitOperations.RotateRight(shifted, (int)(old >> 59));
    }

    private uint Bounded(uint bound)
    {
        var threshold = unchecked(0u - bound) % bound;
        while (true)
        {
            var value = Next();
            if (value >= threshold)
            {
                return value % bound;
            }
        }
    }

    internal float NextFloat()
    {
        var exponent = Next();
        if (exponent == 0)
        {
            return 0;
        }

        return System.MathF.ScaleB((float)(Next() | 0x80000001u), -32 - BitOperations.LeadingZeroCount(exponent));
    }

    internal double NextDouble()
    {
        var exponent = Next();
        if (exponent == 0)
        {
            return 0;
        }

        var high = (ulong)Next() << 32;
        var significand = high | Next() | 0x8000000000000001UL;
        return Math.ScaleB((double)significand, -64 - BitOperations.LeadingZeroCount(exponent));
    }
}
