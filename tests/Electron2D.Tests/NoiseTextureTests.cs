using Electron2D;

internal static class NoiseTextureTests
{
    internal static void Run()
    {
        using var texture = new NoiseTexture { Width = 2, Height = 1, GenerateMipmaps = false };
        Check(texture.GetWidth() == 2 && texture.GetHeight() == 1 && texture.GetImage() is null,
            "A texture without noise retains its configured size but has no pixels.");
        using var noise = new MutableNoise();
        texture.Noise = noise;
        using var first = texture.GetImage()!;
        Check(first.PixelFormat == Image.Format.L8 && first.GetData().SequenceEqual(new byte[] { 0, 255 }),
            "The noise source generates grayscale pixels.");
        var cached = texture.CapturePixels();
        Check(ReferenceEquals(cached, texture.CapturePixels()), "Unchanged reads reuse one pixel snapshot.");

        noise.Constant = true;
        using (var changed = texture.GetImage())
            Check(changed is not null && changed.GetData().SequenceEqual(new byte[] { 0, 0 }) &&
                first.GetData().SequenceEqual(new byte[] { 0, 255 }) && !ReferenceEquals(cached, texture.CapturePixels()),
                "A source change rebakes without mutating a caller-owned image.");
        noise.Constant = false;
        texture.Invert = true;
        using (var inverted = texture.GetImage())
            Check(inverted is not null && inverted.GetData().SequenceEqual(new byte[] { 255, 0 }),
                "Inversion is applied before other image processing.");
        texture.Invert = false;

        using var ramp = new Gradient { Colors = [Colors.Red, Colors.Blue] };
        texture.ColorRamp = ramp;
        using (var colored = texture.GetImage())
            Check(colored is not null && colored.PixelFormat == Image.Format.Rgba8 &&
                colored.GetData().SequenceEqual(new byte[] { 255, 0, 0, 255, 0, 0, 255, 255 }) && !texture.HasAlpha,
                "The color ramp maps luminance to RGBA pixels.");
        ramp.Colors = [Colors.Green, Colors.White];
        using (var recolored = texture.GetImage())
            Check(recolored is not null && recolored.GetPixel(0, 0).G == 1,
                "A color-ramp edit invalidates the generated texture.");

        texture.AsNormalMap = true;
        texture.BumpStrength = 2;
        texture.GenerateMipmaps = true;
        using (var normal = texture.GetImage())
            Check(normal is not null && normal.PixelFormat == Image.Format.Rgba8 && normal.HasMipmaps &&
                normal.GetPixel(0, 0).A == 1 && texture.MipmapCount == 1,
                "Normal conversion and mipmap generation feed the texture contract.");
        texture.ColorRamp = null;
        texture.AsNormalMap = false;
        texture.Seamless = true;
        texture.SeamlessBlendSkirt = 0.25f;
        using (var seamless = texture.GetImage())
            Check(seamless is not null && seamless.Width == 2 && seamless.Height == 1,
                "Seamless generation uses the noise resource's overlap path.");

        using var shallow = (NoiseTexture)texture.Duplicate();
        using var deep = (NoiseTexture)texture.Duplicate(true);
        Check(ReferenceEquals(shallow.Noise, noise) && !ReferenceEquals(deep.Noise, noise) &&
            deep.Noise is MutableNoise && deep.Width == 2 && deep.Seamless,
            "Resource duplication preserves settings and follows the requested source-copy policy.");
        using var deepNoise = (MutableNoise)deep.Noise!;
        using var previous = new MutableNoise();
        using var replacement = new NoiseTexture { Noise = previous };
        replacement.CopyFromResource(texture);
        var replacementChanges = 0;
        replacement.Changed += _ => replacementChanges++;
        previous.Constant = true;
        Check(replacementChanges == 0 && ReferenceEquals(replacement.Noise, noise),
            "CopyFromResource detaches the former source subscription.");
        noise.Constant = true;
        Check(replacementChanges == 1, "CopyFromResource subscribes to the replacement source once.");
        noise.Constant = false;

        Reject<ArgumentOutOfRangeException>(() => texture.Width = 0);
        Reject<ArgumentOutOfRangeException>(() => texture.SeamlessBlendSkirt = float.NaN);
        Reject<ArgumentOutOfRangeException>(() => texture.SeamlessBlendSkirt = 2);
        Reject<ArgumentOutOfRangeException>(() => texture.BumpStrength = float.PositiveInfinity);
        using var invalid = new InvalidNoise();
        texture.Noise = invalid;
        Reject<InvalidOperationException>(() => texture.GetImage());
        texture.Noise = noise;
        using (var recovered = texture.GetImage()) Check(recovered is not null, "Failed baking leaves the texture recoverable.");
        using var recursive = new RecursiveNoise(texture);
        texture.Noise = recursive;
        Reject<InvalidOperationException>(() => texture.GetImage());
        texture.Noise = noise;
        texture.Dispose();
        Check(!noise.IsDisposed && !ramp.IsDisposed, "Texture disposal does not own its sources.");
        Reject<ObjectDisposedException>(() => texture.GetImage());
        Console.WriteLine("Noise texture sampling, invalidation, color, normals, mipmaps, copying and failures passed.");
    }

    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }

    private sealed class MutableNoise : Noise
    {
        private bool _constant;
        internal bool Constant
        {
            get => _constant;
            set { _constant = value; EmitChanged(); }
        }
        public override float GetNoise1D(float x) => _constant ? 0 : x * 2 - 1;
        public override float GetNoise2D(float x, float y) => GetNoise1D(x);
        protected override Resource CreateDuplicateInstance() => new MutableNoise();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
            Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource) =>
            ((MutableNoise)target)._constant = _constant;
    }

    private sealed class InvalidNoise : Noise
    {
        public override float GetNoise1D(float x) => float.NaN;
        public override float GetNoise2D(float x, float y) => float.NaN;
    }

    private sealed class RecursiveNoise(NoiseTexture texture) : Noise
    {
        public override float GetNoise1D(float x) => 0;
        public override float GetNoise2D(float x, float y)
        {
            using var image = texture.GetImage();
            return 0;
        }
    }
}
