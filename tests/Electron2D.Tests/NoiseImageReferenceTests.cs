using Electron2D;
using System.Text.Json;

internal static class NoiseImageReferenceTests
{
    internal static void Run()
    {
        using var input = typeof(NoiseImageReferenceTests).Assembly.GetManifestResourceStream("TestNoise.image-reference.json")!; using var data = JsonDocument.Parse(input);
        Check(data.RootElement.GetProperty("commit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079", "Pinned noise image fixture");
        var failures = new List<string>(); var count = 0;
        foreach (var test in data.RootElement.GetProperty("cases").EnumerateArray())
        {
            using var noise = new FastNoiseLite { Seed = 19, Frequency = .2f, FractalType = FractalType.None };
            var width = test.GetProperty("width").GetInt32(); var height = test.GetProperty("height").GetInt32(); var kind = test.GetProperty("kind").GetString(); var invert = test.TryGetProperty("invert", out var inv) && inv.GetBoolean();
            var mode = test.TryGetProperty("mode", out var modeValue) ? modeValue.GetInt32() : 0; var skirt = test.TryGetProperty("skirt", out var value) ? value.GetSingle() : .1f;
            using var ramp = mode is not (1 or 3 or 4) ? null : new Gradient { Colors = [new(.1f, .7f, .2f, .3f), new(.9f, .2f, .8f, .9f)] };
            using var texture = kind != "texture" ? null : new NoiseTexture { Width = width, Height = height, Noise = noise, ColorRamp = ramp, GenerateMipmaps = mode is 3 or 4 or 5, Seamless = test.GetProperty("seamless").GetBoolean(), SeamlessBlendSkirt = skirt, AsNormalMap = mode is 2 or 3, BumpStrength = 2.5f };
            using var image = kind switch { "image" => noise.GetImage(width, height, invert, test.GetProperty("normalize").GetBoolean()), "seamless" => noise.GetSeamlessImage(width, height, invert, skirt), _ => texture!.GetImage()! };
            var expected = Convert.FromHexString(test.GetProperty("data").GetString()!); var actual = image.GetData();
            Check((int)image.PixelFormat == test.GetProperty("format").GetInt32() && image.HasMipmaps == test.GetProperty("mipmaps").GetBoolean(), "Image format/mip ownership");
            if (!actual.SequenceEqual(expected)) { var first = Enumerable.Range(0, Math.Min(actual.Length, expected.Length)).FirstOrDefault(i => actual[i] != expected[i]); failures.Add($"case {count} {kind} {width}x{height} skirt {skirt} mode {mode}: byte {first} actual {actual[first]} expected {expected[first]}"); }
            count++;
        }
        var rgbaCount = CompareRGBA(failures);
        Check(failures.Count == 0, string.Join("\n", failures)); Console.WriteLine($"Noise images match {count} independently executed pinned grayscale/seamless/gradient/normal/mipmap cases and {rgbaCount} RGBA kernel cases.");
    }
    private sealed class PatternNoise : Noise
    {
        public override float GetNoise1D(float x) => 0;
        public override float GetNoise2D(float x, float y) => 0;
        public override Image GetImage(int width, int height, bool invert = false, bool normalize = true)
        {
            var bytes = new byte[width * height * 4]; for (var y = 0; y < height; y++) for (var x = 0; x < width; x++) for (var c = 0; c < 4; c++) bytes[(y * width + x) * 4 + c] = (byte)(x * 37 + y * 53 + c * 17);
            return Image.CreateFromData(width, height, false, Image.Format.Rgba8, bytes);
        }
    }
    private static int CompareRGBA(List<string> failures)
    {
        using var input = typeof(NoiseImageReferenceTests).Assembly.GetManifestResourceStream("TestNoise.rgba-reference.json")!; using var data = JsonDocument.Parse(input); using var noise = new PatternNoise(); var count = 0;
        Check(data.RootElement.GetProperty("commit").GetString() == "ed1daf0bf001b61586d9930840f2f1394092c079", "Pinned RGBA kernel fixture");
        foreach (var test in data.RootElement.GetProperty("cases").EnumerateArray())
        {
            var width = test.GetProperty("width").GetInt32(); var height = test.GetProperty("height").GetInt32(); var skirt = test.GetProperty("skirt").GetSingle(); using var image = noise.GetSeamlessImage(width, height, skirt: skirt); var actual = image.GetData(); var expected = Convert.FromHexString(test.GetProperty("data").GetString()!);
            if (!actual.SequenceEqual(expected)) { var first = Enumerable.Range(0, actual.Length).First(i => actual[i] != expected[i]); failures.Add($"RGBA kernel {count} {width}x{height} skirt {skirt}: byte {first} actual {actual[first]} expected {expected[first]}"); }
            count++;
        }
        foreach (var skirt in new[] { .75f, 1f }) { using var image = noise.GetSeamlessImage(7, 9, skirt: skirt); Check(image.Width == 7 && image.Height == 9 && image.GetData().Length == 7 * 9 * 4, "Large-skirt writes stay inside the output image"); }
        return count;
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
