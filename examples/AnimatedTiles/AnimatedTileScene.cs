namespace Electron2D.Examples.AnimatedTiles;

internal sealed class AnimatedTileScene : Entity
{
    internal TileMapLayer Layer { get; }
    internal AnimatedTileScene(string file) { using var scene = ResourceLoader.Load<PackedScene>(file); Layer = (TileMapLayer)scene.Instantiate(); AddChild(Layer); }
}
