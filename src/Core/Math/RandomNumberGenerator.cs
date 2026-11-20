namespace Electron2D;

/// <summary>Generates independent pseudo-random number streams with restorable state.</summary>
/// <remarks>The stream uses PCG32. Set <see cref="Seed"/> for a reproducible sequence, or save
/// <see cref="State"/> and restore it after setting the seed to resume a sequence.</remarks>
public class RandomNumberGenerator : ElectronObject
{
    private static readonly IReadOnlyList<PropertyDescriptor> RandomProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<RandomNumberGenerator, ulong>(nameof(Seed), rng => rng.Seed, (rng, value) => rng.Seed = value, _ => 0UL, stored: true),
        new PropertyDescriptor<RandomNumberGenerator, ulong>(nameof(State), rng => rng.State, (rng, value) => rng.State = value, _ => 0UL, stored: true)
    ]);
    private readonly object _gate = new();
    private Pcg32 _stream;

    /// <summary>Creates a generator with a time-dependent seed.</summary>
    public RandomNumberGenerator()
    {
        _stream = Pcg32.CreateRandomized();
    }

    /// <summary>Gets or sets the seed used to initialize this stream.</summary>
    /// <value>A random value initially; assigning it restarts the sequence and replaces <see cref="State"/>.</value>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public ulong Seed
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _stream.Seed;
            }
        }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                _stream.SetSeed(value);
            }
        }
    }

    /// <summary>Gets or restores the current stream state without changing the seed.</summary>
    /// <value>The current 64-bit PCG state. Restore only a previously captured state.</value>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public ulong State
    {
        get
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                return _stream.State;
            }
        }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                _stream.State = value;
            }
        }
    }

    /// <summary>Restarts the stream using the current wall clock and monotonic clock.</summary>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public void Randomize()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            _stream.Randomize();
        }
    }

    /// <summary>Returns a uniformly distributed unsigned 32-bit integer.</summary>
    /// <returns>A value from zero through <see cref="uint.MaxValue"/>.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public uint Randi()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _stream.Next();
        }
    }

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
            return _stream.NextIntRange(from, to);
        }
    }

    /// <summary>Returns a pseudo-random floating-point number between zero and one, inclusive.</summary>
    /// <returns>A value in the unit interval.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public float Randf()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _stream.NextFloat();
        }
    }

    /// <summary>Samples a floating-point range, preserving the order of the supplied bounds.</summary>
    /// <param name="from">The value returned for a unit sample of zero.</param>
    /// <param name="to">The value returned for a unit sample of one.</param>
    /// <returns>A sample between the bounds for finite, representable inputs.</returns>
    /// <exception cref="ObjectDisposedException">The generator has been disposed.</exception>
    public float RandfRange(float from, float to)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _stream.NextFloat() * (to - from) + from;
        }
    }

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
            return _stream.NextFloatNormal(mean, deviation);
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

            var distance = _stream.NextFloat() * sum;
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

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(RandomProperties);
}
