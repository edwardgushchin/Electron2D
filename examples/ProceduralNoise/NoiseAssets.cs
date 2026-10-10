using System.Globalization;

namespace Electron2D.Examples.ProceduralNoise;

internal static class NoiseAssets
{
    internal static void Write(string directory)
    {
        Directory.CreateDirectory(directory);
        using var noise = new FastNoiseLite { ResourceLocalToScene = true, Seed = 1337, NoiseType = NoiseType.ValueCubic, Frequency = .06f, Offset = new(12, 7), FractalType = FractalType.PingPong, FractalOctaves = 3, FractalLacunarity = 1.7f, FractalGain = .3f, FractalWeightedStrength = .2f, FractalPingPongStrength = 2.3f, CellularDistanceFunction = CellularDistanceFunction.Manhattan, CellularReturnType = CellularReturnType.Distance2Mul, CellularJitter = .3f, DomainWarpEnabled = true, DomainWarpType = DomainWarpType.BasicGrid, DomainWarpAmplitude = 3, DomainWarpFrequency = .2f, DomainWarpFractalType = DomainWarpFractalType.Independent, DomainWarpFractalOctaves = 2, DomainWarpFractalLacunarity = 1.9f, DomainWarpFractalGain = .4f };
        using var ramp = new Gradient { ResourceLocalToScene = true, Colors = [Colors.Red, Colors.Blue] };
        using var texture = new NoiseTexture { ResourceLocalToScene = true, Width = 32, Height = 32, Noise = noise, ColorRamp = ramp, Seamless = true, SeamlessBlendSkirt = .2f };
        var texturePath = System.IO.Path.Combine(directory, "noise.e2dres"); ResourceSaver.Save(texture, texturePath); texture.SetPathCache(texturePath);
        using var root = new Entity { Name = "Pattern" }; var picture = new Sprite { Name = "Picture", Texture = texture, Centered = false }; var alias = new Sprite { Name = "Alias", Texture = texture, Centered = false, Visible = false }; root.AddChild(picture); picture.Owner = root; root.AddChild(alias); alias.Owner = root;
        using var scene = new PackedScene(); scene.Pack(root); ResourceSaver.Save(scene, System.IO.Path.Combine(directory, "pattern.e2dscene"));
        File.WriteAllText(System.IO.Path.Combine(directory, "settings.txt"), Settings(noise));
        using var initial = texture.GetImage()!; File.WriteAllBytes(System.IO.Path.Combine(directory, "initial.rgba"), initial.GetData());
        noise.Seed = 42; using var updated = texture.GetImage()!; File.WriteAllBytes(System.IO.Path.Combine(directory, "updated.rgba"), updated.GetData());
    }
    internal static string Settings(FastNoiseLite n) => string.Join("|", new object[] { n.NoiseType, n.Seed, n.Frequency, n.Offset, n.FractalType, n.FractalOctaves, n.FractalLacunarity, n.FractalGain, n.FractalWeightedStrength, n.FractalPingPongStrength, n.CellularDistanceFunction, n.CellularReturnType, n.CellularJitter, n.DomainWarpEnabled, n.DomainWarpType, n.DomainWarpAmplitude, n.DomainWarpFrequency, n.DomainWarpFractalType, n.DomainWarpFractalOctaves, n.DomainWarpFractalLacunarity, n.DomainWarpFractalGain }.Select(v => Convert.ToString(v, CultureInfo.InvariantCulture)));
}
