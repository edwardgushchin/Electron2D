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

namespace Electron2D.Tests.Unit;

public sealed class CanvasNodePublicApiTests
{
    [Fact]
    public void CanvasItemVisibilityAndModulationDefaultsFollowElectron2DBaseline()
    {
        var item = new Electron2D.CanvasItem();

        Assert.True(item.Visible);
        Assert.Equal(Electron2D.Color.White, item.Modulate);
        Assert.Equal(Electron2D.Color.White, item.SelfModulate);
        Assert.Equal(0, item.ZIndex);
        Assert.False(item.YSortEnabled);
        Assert.True(item.IsVisibleInTree());

        item.Hide();
        Assert.False(item.Visible);
        Assert.False(item.IsVisibleInTree());

        item.Show();
        Assert.True(item.Visible);
        Assert.True(item.IsVisibleInTree());
    }

    [Fact]
    public void CanvasItemVisibilityInheritanceStopsAtNonCanvasNode()
    {
        var root = new Electron2D.CanvasItem();
        var directChild = new Electron2D.CanvasItem();
        var bridge = new Electron2D.Node();
        var independentChild = new Electron2D.CanvasItem();

        root.AddChild(directChild);
        root.AddChild(bridge);
        bridge.AddChild(independentChild);

        root.Hide();

        Assert.False(directChild.IsVisibleInTree());
        Assert.True(independentChild.IsVisibleInTree());
    }

    [Fact]
    public void Node2DLocalAndGlobalTransformsUsePositionRotationAndScale()
    {
        var node = new Electron2D.Node2D
        {
            Position = new Electron2D.Vector2(3f, 4f),
            Rotation = Electron2D.Mathf.Pi * 0.5f,
            Scale = new Electron2D.Vector2(2f, 3f)
        };

        Assert.Equal(90f, node.RotationDegrees, precision: 4);
        Assert.True(node.Transform.Xform(Electron2D.Vector2.Right).IsEqualApprox(new Electron2D.Vector2(3f, 6f)));
        Assert.True(node.ToGlobal(Electron2D.Vector2.Right).IsEqualApprox(new Electron2D.Vector2(3f, 6f)));
        Assert.True(node.ToLocal(new Electron2D.Vector2(3f, 6f)).IsEqualApprox(Electron2D.Vector2.Right));

        node.RotationDegrees = 180f;
        Assert.True(Electron2D.Mathf.IsEqualApprox(Electron2D.Mathf.Pi, node.Rotation));

        node.ApplyScale(new Electron2D.Vector2(2f, 0.5f));
        Assert.Equal(new Electron2D.Vector2(4f, 1.5f), node.Scale);
    }

    [Fact]
    public void Node2DGlobalTransformUsesDirectNode2DParent()
    {
        var parent = new Electron2D.Node2D { Position = new Electron2D.Vector2(10f, 20f) };
        var child = new Electron2D.Node2D { Position = new Electron2D.Vector2(2f, 3f) };
        parent.AddChild(child);

        Assert.Equal(new Electron2D.Vector2(12f, 23f), child.GlobalPosition);

        child.GlobalPosition = new Electron2D.Vector2(30f, 40f);

        Assert.Equal(new Electron2D.Vector2(20f, 20f), child.Position);
        Assert.Equal(new Electron2D.Vector2(30f, 40f), child.GlobalPosition);
    }

    [Fact]
    public void Node2DReparentCanKeepGlobalTransform()
    {
        var oldParent = new Electron2D.Node2D { Position = new Electron2D.Vector2(10f, 0f) };
        var newParent = new Electron2D.Node2D { Position = new Electron2D.Vector2(100f, 0f) };
        var child = new Electron2D.Node2D { Position = new Electron2D.Vector2(5f, 0f) };

        oldParent.AddChild(child);

        child.Reparent(newParent, keepGlobalTransform: true);

        Assert.Same(newParent, child.GetParent());
        Assert.Equal(new Electron2D.Vector2(15f, 0f), child.GlobalPosition);
        Assert.Equal(new Electron2D.Vector2(-85f, 0f), child.Position);
    }

    [Fact]
    public void Sprite2DRectRegionFlipAndPixelOpacityFollowTextureSpace()
    {
        var texture = new TestTexture(10, 20, hasAlpha: true, (x, y) => x == 2 && y == 3);
        var sprite = new Electron2D.Sprite2D
        {
            Texture = texture,
            Centered = false,
            Offset = new Electron2D.Vector2(1f, 1f)
        };

        Assert.Equal(new Electron2D.Rect2(1f, 1f, 10f, 20f), sprite.GetRect());
        Assert.True(sprite.IsPixelOpaque(new Electron2D.Vector2(3f, 4f)));
        Assert.False(sprite.IsPixelOpaque(new Electron2D.Vector2(4f, 4f)));

        sprite.FlipH = true;
        sprite.FlipV = true;
        Assert.True(sprite.FlipH);
        Assert.True(sprite.FlipV);

        sprite.RegionEnabled = true;
        sprite.RegionRect = new Electron2D.Rect2(4f, 5f, 6f, 7f);
        Assert.Equal(new Electron2D.Rect2(1f, 1f, 6f, 7f), sprite.GetRect());
    }

    [Fact]
    public void CanvasLayerDefaultsToElectron2DLayerAndVisibility()
    {
        var layer = new Electron2D.CanvasLayer();

        Assert.Equal(1, layer.Layer);
        Assert.True(layer.Visible);
        Assert.Equal(Electron2D.Vector2.Zero, layer.Offset);
        Assert.Equal(Electron2D.Vector2.One, layer.Scale);
        Assert.Equal(Electron2D.Transform2D.Identity, layer.Transform);

        layer.Hide();
        Assert.False(layer.Visible);

        layer.Show();
        Assert.True(layer.Visible);
    }

    private sealed class TestTexture : Electron2D.Texture2D
    {
        private readonly Func<int, int, bool> isOpaque;

        public TestTexture(int width, int height, bool hasAlpha, Func<int, int, bool> isOpaque)
        {
            Width = width;
            Height = height;
            HasAlphaValue = hasAlpha;
            this.isOpaque = isOpaque;
        }

        public int Width { get; }

        public int Height { get; }

        public bool HasAlphaValue { get; }

        public override int GetWidth()
        {
            return Width;
        }

        public override int GetHeight()
        {
            return Height;
        }

        public override bool HasAlpha()
        {
            return HasAlphaValue;
        }

        public override bool IsPixelOpaque(int x, int y)
        {
            return x >= 0 && y >= 0 && x < Width && y < Height && isOpaque(x, y);
        }
    }
}
