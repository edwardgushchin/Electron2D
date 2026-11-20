namespace Electron2D;

/// <summary>Provides a shared PCG32 stream for gameplay and procedural generation.</summary>
/// <remarks>
/// Each shared operation is atomic. Replay requires the same seed and call order, which concurrent
/// callers must coordinate. Normal sampling uses runtime math. Use an independent
/// <see cref="RandomNumberGenerator"/> to save or restore the position of a stream.
/// This generator is intended for gameplay, where cryptographic security is unnecessary.
/// </remarks>
public static class GlobalRandom
{
    private static readonly object Gate = new();
    private static Pcg32 _stream = Pcg32.CreateRandomized();

    /// <summary>Restarts the shared stream from a fixed seed.</summary>
    /// <param name="seed">The full 64-bit initialization seed.</param>
    public static void Seed(ulong seed)
    {
        lock (Gate)
        {
            _stream.SetSeed(seed);
        }
    }

    /// <summary>Restarts the shared stream with a time-dependent seed.</summary>
    public static void Randomize()
    {
        lock (Gate)
        {
            _stream.Randomize();
        }
    }

    /// <summary>Returns the first integer draw for a seed independently of the shared stream.</summary>
    /// <param name="seed">The full 64-bit initialization seed.</param>
    /// <returns>The unsigned draw and the unchanged input seed.</returns>
    public static (uint Value, ulong Seed) RandFromSeed(ulong seed)
    {
        var stream = new Pcg32(seed);
        return (stream.Next(), seed);
    }

    /// <summary>Draws a uniformly distributed unsigned 32-bit integer.</summary>
    /// <returns>A value from zero through <see cref="uint.MaxValue"/>.</returns>
    public static uint Randi()
    {
        lock (Gate)
        {
            return _stream.Next();
        }
    }

    /// <summary>Draws a uniformly distributed integer from an inclusive range.</summary>
    /// <param name="from">One inclusive bound.</param>
    /// <param name="to">The other inclusive bound.</param>
    /// <returns>A value between the bounds, in either bound order.</returns>
    /// <remarks>Equal bounds return that value without drawing from the stream.</remarks>
    public static int RandiRange(int from, int to)
    {
        lock (Gate)
        {
            return _stream.NextIntRange(from, to);
        }
    }

    /// <summary>Draws a unit sample by scaling one unsigned integer to single precision.</summary>
    /// <returns>A value in the inclusive interval [0, 1].</returns>
    public static float Randf()
    {
        lock (Gate)
        {
            return (float)_stream.Next() / uint.MaxValue;
        }
    }

    /// <summary>Interpolates between the bounds using a double-precision unit sample.</summary>
    /// <param name="from">The first interpolation bound.</param>
    /// <param name="to">The second interpolation bound.</param>
    /// <returns>The result of multiplying the unit sample by <c>to - from</c> and adding <paramref name="from"/>.</returns>
    /// <remarks>Double-precision rounding can place the result outside the bounds even when both bounds
    /// and their difference are finite. Overflow and nonfinite inputs follow normal floating-point arithmetic.</remarks>
    public static double RandfRange(double from, double to)
    {
        lock (Gate)
        {
            return _stream.NextDouble() * (to - from) + from;
        }
    }

    /// <summary>Draws a normal sample using the Box-Muller transform in double precision.</summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="deviation">The standard deviation.</param>
    /// <returns>A normally distributed sample.</returns>
    public static double Randfn(double mean = 0, double deviation = 1)
    {
        lock (Gate)
        {
            const double minimumSample = 0.00001;
            var sample = _stream.NextDouble();
            if (sample < minimumSample)
            {
                sample += minimumSample;
            }

            return mean + deviation * (Mathf.Cos(Math.Tau * _stream.NextDouble()) * Mathf.Sqrt(-2 * Mathf.Log(sample)));
        }
    }
}
