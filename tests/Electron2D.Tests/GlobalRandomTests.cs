using Electron2D;

internal static class GlobalRandomTests
{
    private const ulong FixedSeed = 123456789;

    internal static void Run()
    {
        ReplayMixedSequence();
        CheckSeedIsolation();
        CheckIntegerRanges();
        CheckConcurrentSampling();
        Console.WriteLine("Shared PCG32 vectors, seed isolation, integer bounds and concurrent sampling passed.");
    }

    private static void ReplayMixedSequence()
    {
        GlobalRandom.Seed(FixedSeed);
        var expected = DrawMixedSequence();
        Check(expected.Integer == 1712221112u && expected.Unit == 0.12971165776252747f,
            "Raw and unit draws must follow the pinned PCG32 vector.");
        Check(Math.Abs(expected.Range - 4.9679517752496345) < 1e-12 &&
            Math.Abs(expected.Normal - 4.144622869233236) < 1e-12,
            "Reversed floating range and normal draws must follow the pinned PCG32 vector.");
        Check(expected.Bounded == 3 && expected.Next == 3786296415u,
            "Mixed draws must consume the expected PCG32 words.");

        GlobalRandom.Seed(0);
        Check(GlobalRandom.Randi() == 881477183u, "Changing the seed must restart the stream.");
        GlobalRandom.Seed(FixedSeed);
        Check(DrawMixedSequence() == expected, "Resetting the seed must replay all five sampling operations.");

        GlobalRandom.Seed(FixedSeed);
        GlobalRandom.Randomize();
        Check(DrawMixedSequence() != expected, "Randomize must replace the fixed stream.");
        GlobalRandom.Seed(FixedSeed);
        Check(DrawMixedSequence() == expected, "Reseeding after Randomize must recover the fixed stream.");
    }

    private static (uint Integer, float Unit, double Range, double Normal, int Bounded, uint Next) DrawMixedSequence()
    {
        return (GlobalRandom.Randi(), GlobalRandom.Randf(), GlobalRandom.RandfRange(10, -10),
            GlobalRandom.Randfn(4, 2), GlobalRandom.RandiRange(5, -3), GlobalRandom.Randi());
    }

    private static void CheckSeedIsolation()
    {
        using var instance = new RandomNumberGenerator { Seed = FixedSeed };
        var initialState = instance.State;
        GlobalRandom.Seed(FixedSeed);
        Check(GlobalRandom.RandFromSeed(0) == (881477183u, 0UL), "Zero seed must round trip.");
        Check(GlobalRandom.RandFromSeed(ulong.MaxValue) == (3188254867u, ulong.MaxValue),
            "All 64 seed bits must round trip.");
        Check(GlobalRandom.Randi() == 1712221112u, "Seeded draws must leave the shared stream untouched.");
        Check(GlobalRandom.RandFromSeed(FixedSeed) == (1712221112u, FixedSeed),
            "Seeded draws must begin at the first word even when the shared stream has advanced.");
        Check(instance.State == initialState && instance.Randi() == 1712221112u,
            "Shared operations must leave instance state untouched.");
        instance.Seed = ulong.MaxValue;
        Check(instance.Randi() == 3188254867u && GlobalRandom.Randi() == 557107306u,
            "Instance draws and reseeding must leave the shared stream untouched.");
    }

    private static void CheckIntegerRanges()
    {
        GlobalRandom.Seed(5370688683869041894UL);
        Check(GlobalRandom.Randf() == 0f, "The unit interval must include zero.");
        GlobalRandom.Seed(3906655966136464614UL);
        Check(GlobalRandom.Randf() == 1f, "The unit interval must include one.");

        GlobalRandom.Seed(5370688683869041894UL);
        Check(GlobalRandom.RandiRange(int.MinValue, int.MaxValue) == int.MinValue &&
            GlobalRandom.Randi() == 1342897407u, "The full signed range must include its lower endpoint.");
        GlobalRandom.Seed(3906655966136464614UL);
        Check(GlobalRandom.RandiRange(int.MaxValue, int.MinValue) == int.MaxValue &&
            GlobalRandom.Randi() == 2107250955u, "Reversed full bounds must include the upper endpoint.");

        GlobalRandom.Seed(FixedSeed);
        Check(GlobalRandom.RandiRange(17, 17) == 17 && GlobalRandom.Randi() == 1712221112u,
            "Equal integer bounds must consume no words.");
        GlobalRandom.Seed(FixedSeed);
        Check(GlobalRandom.RandiRange(int.MinValue, 0) == -1203588868 && GlobalRandom.Randi() == 1421089443u,
            "Wide bounded draws must reject the expected raw words.");
        GlobalRandom.Seed(FixedSeed);
        Check(GlobalRandom.RandiRange(-3, 5) == -1 && GlobalRandom.Randi() == 557107306u,
            "An ordinary increasing range must agree with the pinned bounded draw.");

        GlobalRandom.Seed(FixedSeed);
        Check(GlobalRandom.RandfRange(2, 2) == 2 && GlobalRandom.Randi() == 13764607u,
            "Equal real bounds must still draw a double-precision sample.");
        GlobalRandom.Seed(5370688683869041894UL);
        Check(GlobalRandom.RandfRange(10, -10) == 10 && GlobalRandom.Randi() == 1342897407u,
            "A zero exponent must return the first bound and consume only one word.");
    }

    private static void CheckConcurrentSampling()
    {
        const int count = 8192;
        var serial = new double[count];
        GlobalRandom.Seed(FixedSeed);
        for (var i = 0; i < count; i++)
        {
            serial[i] = GlobalRandom.Randfn();
        }

        var next = GlobalRandom.Randi();
        var concurrent = new double[count];
        GlobalRandom.Seed(FixedSeed);
        Parallel.For(0, count, new ParallelOptions { MaxDegreeOfParallelism = 4 },
            i => concurrent[i] = GlobalRandom.Randfn(0, 1));

        Array.Sort(serial);
        Array.Sort(concurrent);
        Check(serial.SequenceEqual(concurrent) && GlobalRandom.Randi() == next,
            "Concurrent normal draws must preserve complete samples and the following stream position.");
    }

    private static void Check(bool valid, string message)
    {
        if (!valid)
        {
            throw new InvalidOperationException(message);
        }
    }
}
