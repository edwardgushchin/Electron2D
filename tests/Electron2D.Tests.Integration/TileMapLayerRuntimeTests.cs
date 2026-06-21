/*
    Electron2D
    MIT License
    Copyright (c) 2025-2026 Eduard Gushchin <eduardgushchin@yandex.ru>
    SPDX-License-Identifier: MIT

    Permission is hereby granted, free of charge, to any person obtaining a copy
    of this software and associated documentation files (the "Software"), to deal
    in the Software without restriction, including without limitation the rights
    to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
    copies of the Software, and to permit persons to whom the Software is
    furnished to do so, subject to the following conditions:

    The above copyright notice and this permission notice shall be included in
    all copies or substantial portions of the Software.

    THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
    IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
    FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
    AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
    LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
    OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
    SOFTWARE.
*/
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(PhysicsServer2DCollection.Name)]
public sealed class TileMapLayerRuntimeTests
{
    private const double FixedDelta = 1d / 60d;

    [Fact]
    public void TileSetAtlasSourceStoresTileDataAndTextureRegions()
    {
        var texture = new Electron2D.RuntimeTexture2D(64, 32, hasAlpha: false);
        var tileSet = new Electron2D.TileSet
        {
            TileSize = new Electron2D.Vector2I(16, 16)
        };
        var source = new Electron2D.TileSetAtlasSource
        {
            Texture = texture,
            TextureRegionSize = new Electron2D.Vector2I(16, 16)
        };

        source.CreateTile(new Electron2D.Vector2I(2, 1));
        var tileData = source.GetTileData(new Electron2D.Vector2I(2, 1));
        Assert.NotNull(tileData);

        tileData.SetCollisionPolygonsCount(0, 1);
        tileData.SetCollisionPolygonPoints(0, 0, CreateCellRectanglePoints(16f, 16f));
        tileData.SetCollisionPolygonOneWay(0, 0, true);
        tileData.SetCollisionPolygonOneWayMargin(0, 0, 4f);

        var sourceId = tileSet.AddSource(source, atlasSourceIdOverride: 7);

        Assert.Equal(7, sourceId);
        Assert.True(tileSet.HasSource(7));
        Assert.Same(source, tileSet.GetSource(7));
        Assert.Equal(1, tileSet.GetSourceCount());
        Assert.Equal(7, tileSet.GetSourceId(0));
        Assert.True(source.HasTile(new Electron2D.Vector2I(2, 1)));
        Assert.Equal(1, source.GetTilesCount());
        Assert.Equal(new Electron2D.Vector2I(2, 1), source.GetTileId(0));
        Assert.Equal(new Electron2D.Rect2(32f, 16f, 16f, 16f), source.GetTileTextureRegion(new Electron2D.Vector2I(2, 1)));
        Assert.Equal(CreateCellRectanglePoints(16f, 16f), tileData.GetCollisionPolygonPoints(0, 0));
        Assert.True(tileData.IsCollisionPolygonOneWay(0, 0));
        Assert.Equal(4f, tileData.GetCollisionPolygonOneWayMargin(0, 0));
    }

    [Fact]
    public void TileSetResourceSerializationRoundTripsAtlasTilesAndCollisionData()
    {
        var (tileSet, source) = CreateTileSet(new Electron2D.Vector2I(24, 12), new Electron2D.Vector2I(3, 2));
        source.TextureRegionSize = new Electron2D.Vector2I(12, 6);
        source.CreateTile(new Electron2D.Vector2I(4, 1), new Electron2D.Vector2I(2, 1));

        var tileData = source.GetTileData(new Electron2D.Vector2I(4, 1))!;
        tileData.Modulate = new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f);
        tileData.TextureOrigin = new Electron2D.Vector2(2f, -3f);
        tileData.ZIndex = 7;
        tileData.SetCollisionPolygonsCount(2, 1);
        tileData.SetCollisionPolygonPoints(2, 0, CreateCellRectanglePoints(24f, 12f));
        tileData.SetCollisionPolygonOneWay(2, 0, true);
        tileData.SetCollisionPolygonOneWayMargin(2, 0, 3.5f);

        var document = Electron2D.ResourceObjectSerializer.Capture(tileSet, "res://tiles/platformer.e2res");
        var serialized = Electron2D.SerializedResourceTextSerializer.Serialize(document);
        var restored = Assert.IsType<Electron2D.TileSet>(
            Electron2D.ResourceObjectSerializer.Instantiate(
                Electron2D.SerializedResourceTextSerializer.Deserialize(serialized)));

        Assert.Equal(new Electron2D.Vector2I(24, 12), restored.TileSize);
        Assert.True(restored.HasSource(5));
        var restoredSource = Assert.IsType<Electron2D.TileSetAtlasSource>(restored.GetSource(5));

        Assert.Equal(new Electron2D.Vector2I(12, 6), restoredSource.TextureRegionSize);
        Assert.Equal(2, restoredSource.GetTilesCount());
        Assert.Equal(new Electron2D.Rect2(48f, 6f, 24f, 6f), restoredSource.GetTileTextureRegion(new Electron2D.Vector2I(4, 1)));

        var restoredTileData = restoredSource.GetTileData(new Electron2D.Vector2I(4, 1))!;
        Assert.Equal(new Electron2D.Color(0.25f, 0.5f, 0.75f, 1f), restoredTileData.Modulate);
        Assert.Equal(new Electron2D.Vector2(2f, -3f), restoredTileData.TextureOrigin);
        Assert.Equal(7, restoredTileData.ZIndex);
        Assert.Equal(CreateCellRectanglePoints(24f, 12f), restoredTileData.GetCollisionPolygonPoints(2, 0));
        Assert.True(restoredTileData.IsCollisionPolygonOneWay(2, 0));
        Assert.Equal(3.5f, restoredTileData.GetCollisionPolygonOneWayMargin(2, 0));
    }

    [Fact]
    public void TileMapLayerStoresCellsAndMapsCoordinates()
    {
        var (tileSet, _) = CreateTileSet(new Electron2D.Vector2I(16, 16), new Electron2D.Vector2I(1, 0));
        var layer = new Electron2D.TileMapLayer
        {
            TileSet = tileSet
        };

        layer.SetCell(new Electron2D.Vector2I(2, 3), sourceId: 5, atlasCoords: new Electron2D.Vector2I(1, 0));

        Assert.Equal(5, layer.GetCellSourceId(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(new Electron2D.Vector2I(1, 0), layer.GetCellAtlasCoords(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(0, layer.GetCellAlternativeTile(new Electron2D.Vector2I(2, 3)));
        Assert.NotNull(layer.GetCellTileData(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(new[] { new Electron2D.Vector2I(2, 3) }, layer.GetUsedCells());
        Assert.Equal(new[] { new Electron2D.Vector2I(2, 3) }, layer.GetUsedCellsById(sourceId: 5));
        Assert.Equal(new Electron2D.Rect2I(2, 3, 1, 1), layer.GetUsedRect());
        Assert.Equal(new Electron2D.Vector2(40f, 56f), layer.MapToLocal(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(new Electron2D.Vector2I(2, 3), layer.LocalToMap(new Electron2D.Vector2(47.9f, 63.9f)));

        layer.EraseCell(new Electron2D.Vector2I(2, 3));

        Assert.Equal(-1, layer.GetCellSourceId(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(new Electron2D.Vector2I(-1, -1), layer.GetCellAtlasCoords(new Electron2D.Vector2I(2, 3)));
        Assert.Equal(-1, layer.GetCellAlternativeTile(new Electron2D.Vector2I(2, 3)));
        Assert.Empty(layer.GetUsedCells());
    }

    [Fact]
    public void TileMapLayerSubmitsCellsToCanvasRenderPlan()
    {
        var texture = new Electron2D.RuntimeTexture2D(64, 32, hasAlpha: false);
        var (tileSet, _) = CreateTileSet(new Electron2D.Vector2I(16, 16), new Electron2D.Vector2I(1, 0), texture);
        var root = new Electron2D.Node();
        var layer = new Electron2D.TileMapLayer
        {
            Name = "Ground",
            TileSet = tileSet,
            Position = new Electron2D.Vector2(10f, 20f)
        };
        layer.SetCell(new Electron2D.Vector2I(2, 0), sourceId: 5, atlasCoords: new Electron2D.Vector2I(1, 0));
        root.AddChild(layer);

        var command = Assert.Single(new Electron2D.CanvasSubmissionContext().BuildPlan(root).Commands);

        Assert.Same(texture, command.Texture);
        Assert.Equal(new Electron2D.Rect2(16f, 0f, 16f, 16f), command.SourceRect);
        Assert.Equal(new Electron2D.Rect2(32f, 0f, 16f, 16f), command.DestinationRect);
        Assert.True(command.Transform.Origin.IsEqualApprox(new Electron2D.Vector2(10f, 20f)));
        Assert.Equal("Ground", command.DebugName);
    }

    [Fact]
    public void TileMapLayerCollisionSupportsOneWayPlatformMovement()
    {
        var fallingTree = new Electron2D.SceneTree();
        var platform = CreateOneWayPlatformLayer();
        var falling = CreateCharacter("Falling", Electron2D.Vector2.Zero, new Electron2D.Vector2(10f, 10f));
        falling.Velocity = new Electron2D.Vector2(0f, 6000f);
        fallingTree.Root.AddChild(platform);
        fallingTree.Root.AddChild(falling);

        Assert.True(falling.MoveAndSlide());
        Assert.True(falling.IsOnFloor());
        Assert.Same(platform, falling.GetLastSlideCollision()!.GetCollider());
        AssertVectorEqual(new Electron2D.Vector2(0f, 44.92f), falling.Position);
        AssertVectorEqual(Electron2D.Vector2.Zero, falling.Velocity);

        var risingTree = new Electron2D.SceneTree();
        var oneWayPlatform = CreateOneWayPlatformLayer();
        var rising = CreateCharacter("Rising", new Electron2D.Vector2(0f, 70f), new Electron2D.Vector2(10f, 10f));
        rising.Velocity = new Electron2D.Vector2(0f, -6000f);
        risingTree.Root.AddChild(oneWayPlatform);
        risingTree.Root.AddChild(rising);

        Assert.False(rising.MoveAndSlide());
        Assert.False(rising.IsOnCeiling());
        AssertVectorEqual(new Electron2D.Vector2(0f, -30f), rising.Position);
        AssertVectorEqual(new Electron2D.Vector2(0f, -6000f), rising.Velocity);
    }

    private static (Electron2D.TileSet TileSet, Electron2D.TileSetAtlasSource Source) CreateTileSet(
        Electron2D.Vector2I tileSize,
        Electron2D.Vector2I atlasCoords,
        Electron2D.Texture2D? texture = null)
    {
        var tileSet = new Electron2D.TileSet
        {
            TileSize = tileSize
        };
        var source = new Electron2D.TileSetAtlasSource
        {
            Texture = texture ?? new Electron2D.RuntimeTexture2D(64, 32, hasAlpha: false),
            TextureRegionSize = tileSize
        };
        source.CreateTile(atlasCoords);
        tileSet.AddSource(source, atlasSourceIdOverride: 5);
        return (tileSet, source);
    }

    private static Electron2D.TileMapLayer CreateOneWayPlatformLayer()
    {
        var (tileSet, source) = CreateTileSet(new Electron2D.Vector2I(120, 10), Electron2D.Vector2I.Zero);
        var tileData = source.GetTileData(Electron2D.Vector2I.Zero)!;
        tileData.SetCollisionPolygonsCount(0, 1);
        tileData.SetCollisionPolygonPoints(0, 0, CreateCellRectanglePoints(120f, 10f));
        tileData.SetCollisionPolygonOneWay(0, 0, true);
        tileData.SetCollisionPolygonOneWayMargin(0, 0, 1f);

        var layer = new Electron2D.TileMapLayer
        {
            Name = "TilePlatform",
            TileSet = tileSet
        };
        layer.SetCell(new Electron2D.Vector2I(0, 5), sourceId: 5, atlasCoords: Electron2D.Vector2I.Zero);
        return layer;
    }

    private static Electron2D.CharacterBody2D CreateCharacter(
        string name,
        Electron2D.Vector2 position,
        Electron2D.Vector2 size)
    {
        var body = new Electron2D.CharacterBody2D
        {
            Name = name,
            Position = position
        };
        body.AddChild(new Electron2D.CollisionShape2D
        {
            Shape = new Electron2D.RectangleShape2D { Size = size }
        });
        return body;
    }

    private static Electron2D.Vector2[] CreateCellRectanglePoints(float width, float height)
    {
        return
        [
            new Electron2D.Vector2(0f, 0f),
            new Electron2D.Vector2(width, 0f),
            new Electron2D.Vector2(width, height),
            new Electron2D.Vector2(0f, height)
        ];
    }

    private static void AssertVectorEqual(Electron2D.Vector2 expected, Electron2D.Vector2 actual)
    {
        Assert.True(actual.IsEqualApprox(expected), $"Expected {expected}, actual {actual}.");
    }
}
