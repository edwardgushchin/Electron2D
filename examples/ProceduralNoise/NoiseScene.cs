namespace Electron2D.Examples.ProceduralNoise;

internal sealed class NoiseScene : Entity
{
    internal NoiseTexture First { get; }
    internal NoiseTexture Second { get; }
    internal int Publications { get; private set; }
    internal bool InitiallyEmpty { get; private set; }
    internal NoiseScene(string directory)
    {
        using var scene = ResourceLoader.Load<PackedScene>(System.IO.Path.Combine(directory, "pattern.e2dscene"));
        Entity? first = null, second = null;
        try
        {
            first = (Entity)scene.Instantiate(); second = (Entity)scene.Instantiate(); second.Name = "SecondPattern"; second.Position = new(32, 0); AddChild(first); AddChild(second);
            First = (NoiseTexture)first.GetNode<Sprite>("Picture").Texture!; Second = (NoiseTexture)second.GetNode<Sprite>("Picture").Texture!;
            if (ReferenceEquals(First, Second) || ReferenceEquals(First.Noise, Second.Noise) || !ReferenceEquals(First, first.GetNode<Sprite>("Alias").Texture)) throw new InvalidOperationException("Scene-local generation must preserve aliases within each instance.");
        }
        catch { first?.Dispose(); second?.Dispose(); Dispose(); throw; }

    }
    protected override void OnReady() { InitiallyEmpty = First.GetImage() is null && Second.GetImage() is null; First.Changed += _ => Publications++; }
    internal void Stage() => ((FastNoiseLite)First.Noise!).Seed = 42;
}
