using Electron2D;

internal static class RandomNumberGeneratorTests
{
    internal static void Run()
    {
        using var rng = new Electron2D.RandomNumberGenerator { Seed = 123456789 };
        Check(rng.State == 8419318947914174787UL, "Seed must initialize the reference PCG32 state.");
        uint[] expected = [1712221112, 557107306, 1131688667, 13764607, 3091378429];
        foreach (var value in expected) Check(rng.Randi() == value, "The PCG32 sequence changed.");
        var saved = rng.State;
        var next = rng.Randi();
        rng.State = saved;
        Check(rng.Randi() == next && rng.Seed == 123456789, "Restoring state must resume without changing seed.");

        rng.Seed = 123456789;
        var unit = rng.Randf();
        Check(unit is >= 0 and <= 1 && rng.State == 14328811443233429133UL, "Float sampling consumes the expected two PCG values.");
        rng.Seed = 123456789;
        Check(rng.RandfRange(10, -10) is >= -10 and <= 10, "Reversed floating bounds must work.");
        rng.Seed = 123456789;
        Check(rng.RandiRange(7, 7) == 7 && rng.State == 8419318947914174787UL, "Equal bounds must not advance the stream.");
        Check(rng.RandiRange(int.MinValue, int.MaxValue) == unchecked((int)(-2147483648L + expected[0])), "The full signed range must use one raw sample.");
        for (var i = 0; i < 1000; i++)
        {
            var result = rng.RandiRange(5, -3);
            Check(result is >= -3 and <= 5, "Integer ranges must be inclusive and order independent.");
        }

        var before = rng.State;
        Check(rng.RandWeighted([]) == -1 && rng.RandWeighted([1, -1]) == -1 && rng.State == before, "Invalid weights must not advance the stream.");
        Check(rng.RandWeighted([0, 0, 4]) == 2 && rng.RandWeighted([0, 0]) == -1, "Zero weights and an all-zero selection must work.");
        Reject<ArgumentNullException>(() => rng.RandWeighted(null!));
        rng.Seed = 123456789;
        var zeroDeviation = rng.Randfn(4, 0);
        using (var comparison = new Electron2D.RandomNumberGenerator { Seed = 123456789 })
        {
            _ = comparison.Randf(); _ = comparison.Randf();
            Check(zeroDeviation == 4 && rng.State == comparison.State, "Normal sampling must consume two unit samples.");
        }
        rng.Seed = 123456789;
        var normalSum = 0d;
        var normalSquareSum = 0d;
        for (var i = 0; i < 20_000; i++)
        {
            var value = rng.Randfn(4, 2);
            Check(float.IsFinite(value), "Normal samples must stay finite for finite inputs.");
            normalSum += value;
            normalSquareSum += (double)value * value;
        }
        var normalMean = normalSum / 20_000;
        Check(Math.Abs(normalMean - 4) < .1 && Math.Abs(normalSquareSum / 20_000 - normalMean * normalMean - 4) < .2,
            "Normal sampling must have the requested mean and variance.");
        rng.Seed = 123456789;
        var weightedHits = 0;
        float[] weighted = [1, 3];
        for (var i = 0; i < 20_000; i++) if (rng.RandWeighted(weighted) == 1) weightedHits++;
        Check(weightedHits is > 14_000 and < 16_000, "Weighted sampling must reflect the weight ratio.");
        var stateProperty = rng.GetPropertyList().OfType<PropertyDescriptor<Electron2D.RandomNumberGenerator, ulong>>().Single(x => x.Name == nameof(rng.State));
        var restored = rng.State;
        stateProperty.SetValue(rng, saved);
        Check(rng.State == saved, "Typed state descriptor must restore the stream.");
        rng.State = restored;
        rng.Randomize();
        Check(rng.Seed != 123456789, "Randomize must replace the fixed seed.");
        rng.Dispose();
        Reject<ObjectDisposedException>(() => rng.Randi());
        Reject<ObjectDisposedException>(() => rng.State = saved);
        Console.WriteLine("Random-number sequence, ranges, weights, state and lifetime checks passed.");
    }

    private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
