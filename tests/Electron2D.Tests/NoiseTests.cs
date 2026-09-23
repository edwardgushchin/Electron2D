using Electron2D;

internal static class NoiseTests
{
    internal static void Run()
    {
        using var noise = new RampNoise();
        Check(noise.GetNoise1D(2) == 1 && noise.GetNoise2DV(new Vector2(2, 0)) == 1,
            "One-dimensional and vector sampling dispatch to the concrete generator.");
        using (var image = noise.GetImage(3, 1))
            Check(image.PixelFormat == Image.Format.L8 && image.GetData().SequenceEqual(new byte[] { 0, 127, 255 }),
                "Normalized image samples scale the finite minimum and maximum.");
        using (var image = noise.GetImage(3, 1, invert: true, normalize: false))
            Check(image.GetData().SequenceEqual(new byte[] { 255, 128, 0 }),
                "Raw [-1, 1] values truncate to bytes before inversion.");
        using (var image = noise.GetSeamlessImage(4, 4, skirt: 0.25f))
            Check(image.PixelFormat == Image.Format.L8 && image.Width == 4 && image.Height == 4 && image.GetData().Distinct().Count() > 1,
                "The overlap path produces a nonconstant image in the source format.");
        using (var image = noise.GetSeamlessImage(1, 1))
            Check(image.Width == 1 && image.Height == 1, "The smallest image supports quadrant wrapping.");

        using var constant = new ConstantNoise();
        using (var image = constant.GetImage(2, 2))
            Check(image.GetData().SequenceEqual(new byte[4]), "Constant normalized noise maps to black.");
        using (var image = constant.GetImage(2, 2, invert: true))
            Check(image.GetData().All(pixel => pixel == 255), "Inversion follows normalization.");
        using (var image = constant.GetSeamlessImage(4, 4, invert: true))
            Check(image.GetData().All(pixel => pixel == 255), "Seam blending preserves a constant image.");
        using var rgba = new RGBANoise();
        using (var image = rgba.GetSeamlessImage(3, 3))
            Check(image.PixelFormat == Image.Format.Rgba8 && image.GetData().Chunk(4).All(pixel => pixel.SequenceEqual(new byte[] { 30, 70, 200, 255 })),
                "The virtual source path preserves independent RGBA channels and ownership.");
        using var extreme = new ExtremeNoise();
        using (var image = extreme.GetImage(2, 1))
            Check(image.GetData().SequenceEqual(new byte[] { 0, 255 }), "Normalization widens finite extreme differences.");

        Reject<ArgumentOutOfRangeException>(() => noise.GetImage(0, 1));
        Reject<ArgumentOutOfRangeException>(() => noise.GetSeamlessImage(2, 2, skirt: float.NaN));
        Reject<ArgumentOutOfRangeException>(() => noise.GetSeamlessImage(2, 2, skirt: float.MaxValue));
        Reject<ArgumentOutOfRangeException>(() => noise.GetSeamlessImage(16_384, 2));
        using var invalid = new InvalidNoise();
        Reject<InvalidOperationException>(() => invalid.GetImage(2, 2));
        noise.Dispose();
        Reject<ObjectDisposedException>(() => noise.GetImage(1, 1));
        Reject<ObjectDisposedException>(() => noise.GetNoise2DV(Vector2.Zero));
        Console.WriteLine("Noise sampling, image generation, seamless blending and failures passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class RampNoise : Noise
    {
        public override float GetNoise1D(float x) => x - 1;
        public override float GetNoise2D(float x, float y) => x + y - 1;
    }

    private sealed class ConstantNoise : Noise
    {
        public override float GetNoise1D(float x) => 0;
        public override float GetNoise2D(float x, float y) => 0;
    }

    private sealed class InvalidNoise : Noise
    {
        public override float GetNoise1D(float x) => float.NaN;
        public override float GetNoise2D(float x, float y) => float.NaN;
    }

    private sealed class ExtremeNoise : Noise
    {
        public override float GetNoise1D(float x) => x == 0 ? -float.MaxValue : float.MaxValue;
        public override float GetNoise2D(float x, float y) => GetNoise1D(x);
    }

    private sealed class RGBANoise : Noise
    {
        public override float GetNoise1D(float x) => 0;
        public override float GetNoise2D(float x, float y) => 0;
        public override Image GetImage(int width, int height, bool invert = false, bool normalize = true) =>
            Image.CreateFromData(width, height, false, Image.Format.Rgba8,
                Enumerable.Range(0, width * height).SelectMany(_ => new byte[] { 30, 70, 200, 255 }).ToArray());
    }
}
