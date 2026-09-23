using Electron2D;

internal static class FastNoiseLiteTests
{
    internal static void Run()
    {
        using var noise = new FastNoiseLite();
        Check(noise.NoiseType == FastNoiseLite.NoiseTypeEnum.SimplexSmooth && noise.Seed == 0 && noise.Frequency == 0.01f &&
              noise.FractalType == FastNoiseLite.FractalTypeEnum.FBM && noise.FractalOctaves == 5 &&
              noise.CellularDistanceFunction == FastNoiseLite.CellularDistanceFunctionEnum.Euclidean &&
              noise.DomainWarpFractalType == FastNoiseLite.DomainWarpFractalTypeEnum.Progressive,
            "The default wrapper configures both backend generators.");
        Near(noise.GetNoise2D(12.5f, -7.25f), -0.236460894f, "Default OpenSimplex2S/FBM sample matches the pinned native algorithm.");
        var changes = 0; var lists = 0;
        noise.Changed += _ => changes++; noise.PropertyListChanged += _ => lists++;
        noise.Seed = 0; Check(changes == 1 && lists == 0, "Equal seed assignment still emits Changed.");
        noise.FractalType = FastNoiseLite.FractalTypeEnum.None;
        var expected = new[] { -0.292454004f, -0.191824034f, -0.575321615f, -0.0862276554f, 0.0118474429f, 0.00951066613f };
        for (var i = 0; i < expected.Length; i++)
        {
            noise.NoiseType = (FastNoiseLite.NoiseTypeEnum)i;
            Near(noise.GetNoise2D(12.5f, -7.25f), expected[i], $"Noise type {i} matches the pinned native algorithm.");
        }
        Check(lists == 7, "Fractal and noise type changes notify the property list.");
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.FBM;
        noise.DomainWarpEnabled = true;
        Near(noise.GetNoise2D(12.5f, -7.25f), -0.0896649063f, "Progressive domain warp matches the pinned native algorithm.");
        var before = changes; noise.DomainWarpEnabled = true;
        Check(changes == before, "Repeated warp enablement is silent.");
        noise.DomainWarpEnabled = false;
        var fractalSamples = new[] { 0.645866394f, 0.273189276f, -0.0137654115f, 0.425243765f };
        for (var i = 0; i < fractalSamples.Length; i++)
        {
            noise.FractalType = (FastNoiseLite.FractalTypeEnum)i;
            Near(noise.GetNoise2D(31.75f, 45.125f), fractalSamples[i], $"Fractal mode {i} matches the pinned native algorithm.");
        }
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.Cellular;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.None;
        noise.CellularDistanceFunction = FastNoiseLite.CellularDistanceFunctionEnum.Hybrid;
        noise.CellularReturnType = FastNoiseLite.CellularReturnTypeEnum.Distance2Div;
        noise.CellularJitter = 0.75f;
        Near(noise.GetNoise2D(31.75f, 45.125f), -0.0654041171f, "Cellular distance, return and jitter match the pinned native algorithm.");
        noise.NoiseType = FastNoiseLite.NoiseTypeEnum.SimplexSmooth;
        noise.FractalType = FastNoiseLite.FractalTypeEnum.FBM;
        noise.DomainWarpEnabled = true;
        var warpSamples = new[]
        {
            0.270160258f, 0.219851837f, 0.27002123f,
            0.247627646f, 0.238803804f, 0.356469899f,
            0.447229117f, 0.417616874f, 0.397535145f,
        };
        for (var type = 0; type < 3; type++)
            for (var fractal = 0; fractal < 3; fractal++)
            {
                noise.DomainWarpType = (FastNoiseLite.DomainWarpTypeEnum)type;
                noise.DomainWarpFractalType = (FastNoiseLite.DomainWarpFractalTypeEnum)fractal;
                Near(noise.GetNoise2D(31.75f, 45.125f), warpSamples[type * 3 + fractal],
                    $"Warp mode {type}/{fractal} matches the pinned native algorithm.");
            }
        noise.Offset = new Vector2(2, 3);
        Check(noise.GetNoise2D(12.5f, -7.25f) != noise.GetNoise2D(12.5f, -7.25f - 3), "Offset affects samples.");
        noise.DomainWarpEnabled = false;
        Near(noise.GetNoise1D(7.5f), noise.GetNoise2D(9.5f, 0f), "One-dimensional delegation applies the X offset twice.");
        noise.DomainWarpEnabled = true;
        using var copy = (FastNoiseLite)noise.Duplicate();
        Near(copy.GetNoise2D(12.5f, -7.25f), noise.GetNoise2D(12.5f, -7.25f), "Duplication copies both generators and offset.");
        copy.Seed = 123;
        Check(copy.GetNoise2D(12.5f, -7.25f) != noise.GetNoise2D(12.5f, -7.25f), "Copied generators are independent.");
        copy.CopyFromResource(noise);
        Near(copy.GetNoise2D(12.5f, -7.25f), noise.GetNoise2D(12.5f, -7.25f), "Resource copying restores the exact noise configuration.");
        using var texture = new NoiseTexture { Noise = noise, Width = 8, Height = 8, GenerateMipmaps = false };
        using var image = texture.GetImage();
        Check(image is not null && image.GetData().Distinct().Count() > 1, "The built-in generator feeds a concrete noise texture.");
        noise.Seed = 42;
        using var changed = texture.GetImage();
        Check(changed is not null && !changed.GetData().SequenceEqual(image!.GetData()), "Generator changes invalidate the borrowed texture.");
        Reject<ArgumentOutOfRangeException>(() => noise.Frequency = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => noise.FractalOctaves = 0);
        Reject<ArgumentOutOfRangeException>(() => noise.NoiseType = (FastNoiseLite.NoiseTypeEnum)99);
        noise.Dispose();
        Reject<ObjectDisposedException>(() => noise.GetNoise2D(1, 2));
        Console.WriteLine("FastNoiseLite vectors, state, copying and texture integration passed.");
    }

    private static void Near(float actual, float expected, string message)
    {
        if (!float.IsFinite(actual) || MathF.Abs(actual - expected) > 2e-5f)
            throw new InvalidOperationException($"{message} Expected {expected}, got {actual}.");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
