using Electron2D;
using IOPath = System.IO.Path;

internal static partial class GraphChecks
{
    private static void CheckDomainBlits(string folder)
    {
        var files = Directory.GetFiles(folder, "blit-*.e2dres").Order(StringComparer.Ordinal).ToArray();
        using var source0 = new DrawableTexture(); using var source1 = new DrawableTexture(); using var source2 = new DrawableTexture(); using var source3 = new DrawableTexture();
        var sources = new[] { source0, source1, source2, source3 };
        for (var index = 0; index < 4; index++) sources[index].Setup(8, 8, DrawableTexture.DrawableFormat.RGBA8, new((index + 1) * 32f / 255, (index + 2) * 32f / 255, 192f / 255, 1));
        foreach (var file in files)
        {
            var name = IOPath.GetFileNameWithoutExtension(file);
            if (name.Length == 6 && char.IsAsciiDigit(name[^1])) continue;
            using var shader = ResourceLoader.Load<VisualShader>(file, ResourceLoader.CacheMode.IgnoreDeep); using var material = new ShaderMaterial { Shader = shader };
            if (name.StartsWith("blit-orientation-", StringComparison.Ordinal)) { CheckOrientationBlit(material); continue; }
            if (name.StartsWith("blit-random-range-", StringComparison.Ordinal)) { CheckRandomBlit(name, material); continue; }
            using var target = new DrawableTexture(); target.Setup(8, 8, DrawableTexture.DrawableFormat.RGBA8, Colors.Magenta);
            target.BlitRectMulti(new(0, 0, 8, 8), [source0, source1, source2, source3], [], material: material);
            using var image = target.GetImage()!; var actual = image.GetPixel(3, 3);
            var expected = new Color(.25f, .5f, .75f, 1);
            if (name == "blit-empty") expected = Colors.Black;
            else if (name.StartsWith("blit-source-", StringComparison.Ordinal)) { var index = name[^1] - '0'; expected = new((index + 1) * 32f / 255, (index + 2) * 32f / 255, 192f / 255, 1); }
            else if (name.StartsWith("blit-image-", StringComparison.Ordinal)) expected = new(64f / 255, 128f / 255, 192f / 255, 1);
            else if (name.StartsWith("blit-custom-", StringComparison.Ordinal)) expected = name[^1] == '0' ? new(.25f, .5f, .75f, 1) : name[^1] == '1' ? new(.125f, .375f, .625f, 1) : new(.5f, .5f, .5f, 1);
            else if (name.StartsWith("blit-parameter-", StringComparison.Ordinal))
            {
                var role = int.Parse(name.Split('-')[3]); expected = role switch
                { 0 => new(.25f, .25f, .25f, 1), 1 => new(7f / 16, 7f / 16, 7f / 16, 1), 2 => new(11f / 16, 11f / 16, 11f / 16, 1), 3 => new(.125f, .625f, .25f, 1), 4 => new(.125f, .5f, .875f, 1), 5 => new(.125f, .625f, .875f, 1), 6 => new(.75f, .75f, .75f, 1), _ => new(.2f, .3f, .4f, 1) };
            }
            Check(MathF.Abs(actual.R - expected.R) < .005f && MathF.Abs(actual.G - expected.G) < .005f && MathF.Abs(actual.B - expected.B) < .005f && MathF.Abs(actual.A - expected.A) < .005f, $"{name} pixel {actual}, expected {expected}.");
            if (name.StartsWith("blit-reroute", StringComparison.Ordinal))
            {
                for (var i = 0; i < 64; i++) target.BlitRectMulti(new(0, 0, 8, 8), [], [], material: material);
                var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 64; i++) target.BlitRectMulti(new(0, 0, 8, 8), [], [], material: material);
                var ownerDelta = GC.GetAllocatedBytesForCurrentThread() - owner; var allDelta = GC.GetTotalAllocatedBytes(true) - all;
                Check(ownerDelta == 0 && allDelta == 0, $"{name}: 64 prepared blits owner={ownerDelta}, all={allDelta} managed bytes.");
            }
        }
        Console.WriteLine($"{files.Length} writable-texture graphs: defaults, parameters, compiled providers, images/arrays, globals and source bindings passed.");
    }
}
