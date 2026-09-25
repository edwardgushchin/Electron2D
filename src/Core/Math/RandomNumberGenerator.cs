using System.Diagnostics;
using System.Numerics;

namespace Electron2D;

// PCG32 core adapted from M.E. O'Neill's 2014 Apache-2.0 implementation.
// See licence/PCG32-LICENSE.txt for its notice and license.
/// <summary>Generates independent pseudo-random number streams with restorable state.</summary>
/// <remarks>The stream uses PCG32. Set <see cref="Seed"/> for a reproducible sequence, or save
/// <see cref="State"/> and restore it after setting the seed to resume a sequence.</remarks>
public class RandomNumberGenerator : ElectronObject
{
    private const ulong Multiplier = 6364136223846793005UL;
    private const ulong DefaultIncrement = 1442695040888963407UL;
    private const ulong DefaultSeed = 12047754176567800795UL;
    private static readonly IReadOnlyList<PropertyDescriptor> RandomProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<RandomNumberGenerator, ulong>(nameof(Seed), rng => rng.Seed, (rng, value) => rng.Seed = value, _ => 0UL, stored: true),
        new PropertyDescriptor<RandomNumberGenerator, ulong>(nameof(State), rng => rng.State, (rng, value) => rng.State = value, _ => 0UL, stored: true)
    ]);
    private readonly object _gate = new();
    private ulong _state;
    private ulong _seed;

    /// <summary>Creates a generator with a time-dependent seed.</summary>
    public RandomNumberGenerator()
    {
        SetSeed(DefaultSeed);
        Randomize();
    }

    /// <summary>Gets or sets the seed used to initialize this stream.</summary>
    /// <value>A random value initially; assigning it restarts the sequence and replaces <see cref="State"/>.</value>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public ulong Seed
    {
        get { lock (_gate) { ThrowIfDisposed(); return _seed; } }
        set { lock (_gate) { ThrowIfDisposed(); SetSeed(value); } }
    }

    /// <summary>Gets or restores the current stream state without changing the seed.</summary>
    /// <value>The current 64-bit PCG state. Restore only a previously captured state.</value>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public ulong State
    {
        get { lock (_gate) { ThrowIfDisposed(); return _state; } }
        set { lock (_gate) { ThrowIfDisposed(); _state = value; } }
    }

    /// <summary>Restarts the stream using the current wall clock and monotonic clock.</summary>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public void Randomize()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var seconds = unchecked((ulong)DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var microseconds = unchecked((ulong)((double)Stopwatch.GetTimestamp() * 1_000_000 / Stopwatch.Frequency));
            SetSeed(unchecked((seconds + microseconds) * _state + DefaultIncrement));
        }
    }

    /// <summary>Returns a uniformly distributed unsigned 32-bit integer.</summary>
    /// <returns>A value from zero through <see cref="uint.MaxValue"/>.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public uint Randi() { lock (_gate) { ThrowIfDisposed(); return Next(); } }

    /// <summary>Returns an inclusive, uniformly distributed signed integer from either ordering of the bounds.</summary>
    /// <param name="from">One inclusive bound.</param>
    /// <param name="to">The other inclusive bound.</param>
    /// <returns>An integer within the two bounds.</returns>
    /// <remarks>Equal bounds do not advance the stream.</remarks>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public int RandiRange(int from, int to)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (from == to) return from;
            var min = Math.Min((long)from, to);
            var max = Math.Max((long)from, to);
            var difference = (uint)(max - min);
            return (int)(min + (difference == uint.MaxValue ? Next() : Bounded(difference + 1)));
        }
    }

    /// <summary>Returns a pseudo-random floating-point number between zero and one, inclusive.</summary>
    /// <returns>A value in the unit interval.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public float Randf() { lock (_gate) { ThrowIfDisposed(); return NextFloat(); } }

    /// <summary>Samples a floating-point range, preserving the order of the supplied bounds.</summary>
    /// <param name="from">The value returned for a unit sample of zero.</param>
    /// <param name="to">The value returned for a unit sample of one.</param>
    /// <returns>A sample between the bounds for finite, representable inputs.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public float RandfRange(float from, float to) { lock (_gate) { ThrowIfDisposed(); return NextFloat() * (to - from) + from; } }

    /// <summary>Samples a normal distribution using the Box-Muller transform.</summary>
    /// <param name="mean">The distribution mean.</param>
    /// <param name="deviation">The standard deviation.</param>
    /// <returns>A normally distributed floating-point sample.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public float Randfn(float mean = 0, float deviation = 1)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            var sample = NextFloat();
            if (sample < 0.00001f) sample += 0.00001f;
            return mean + deviation * (Mathf.Cos(Mathf.Tau * NextFloat()) * Mathf.Sqrt(-2f * Mathf.Log(sample)));
        }
    }

    /// <summary>Chooses an index with probability proportional to each nonnegative weight.</summary>
    /// <param name="weights">Weights in index order.</param>
    /// <returns>The chosen index, or -1 for an empty array, a negative weight, or no positive weight.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="weights"/> is null.</exception>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public int RandWeighted(float[] weights)
    {
        ArgumentNullException.ThrowIfNull(weights);
        lock (_gate)
        {
            ThrowIfDisposed();
            if (weights.Length == 0) return -1;
            float sum = 0;
            foreach (var weight in weights)
            {
                if (weight < 0) return -1;
                sum += weight;
            }

            var distance = NextFloat() * sum;
            for (var i = 0; i < weights.Length; i++)
            {
                distance -= weights[i];
                if (distance < 0) return i;
            }

            for (var i = weights.Length - 1; i >= 0; i--)
                if (weights[i] > 0) return i;
            return -1;
        }
    }

    private void SetSeed(ulong value)
    {
        _seed = value;
        _state = 0;
        _ = Next();
        _state = unchecked(_state + value);
        _ = Next();
    }

    private uint Next()
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
            if (value >= threshold) return value % bound;
        }
    }

    private float NextFloat()
    {
        var exponent = Next();
        if (exponent == 0) return 0;
        return System.MathF.ScaleB((float)(Next() | 0x80000001u), -32 - BitOperations.LeadingZeroCount(exponent));
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(RandomProperties);
}
