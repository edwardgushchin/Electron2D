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

public sealed class CanvasNodeSubmissionTests
{
    [Fact]
    public void SubmissionSortsSpritesByLayerZIndexAndTreeOrder()
    {
        var root = new Electron2D.Node();
        var backLayer = new Electron2D.CanvasLayer { Layer = -1 };
        var frontLayer = new Electron2D.CanvasLayer { Layer = 1 };
        var backSprite = CreateSprite("back", zIndex: 100);
        var frontLow = CreateSprite("front-low", zIndex: -10);
        var frontHigh = CreateSprite("front-high", zIndex: 5);

        root.AddChild(frontLayer);
        root.AddChild(backLayer);
        backLayer.AddChild(backSprite);
        frontLayer.AddChild(frontHigh);
        frontLayer.AddChild(frontLow);

        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(root);

        Assert.Equal(
            new[] { "back", "front-low", "front-high" },
            plan.Commands.Select(command => command.DebugName).ToArray());
        Assert.Equal(new[] { -1, 1, 1 }, plan.Commands.Select(command => command.Layer).ToArray());
    }

    [Fact]
    public void SubmissionCombinesInheritedModulateWithSelfModulateOnlyForCurrentSprite()
    {
        var parent = new Electron2D.Node2D
        {
            Modulate = new Electron2D.Color(0.5f, 1f, 1f, 1f),
            SelfModulate = new Electron2D.Color(0.1f, 0.1f, 0.1f, 1f)
        };
        var sprite = CreateSprite("sprite", zIndex: 0);
        sprite.Modulate = new Electron2D.Color(1f, 0.5f, 1f, 1f);
        sprite.SelfModulate = new Electron2D.Color(1f, 1f, 0.25f, 0.5f);
        parent.AddChild(sprite);

        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(parent);

        var command = Assert.Single(plan.Commands);
        Assert.Equal(new Electron2D.Color(0.5f, 0.5f, 1f, 1f), command.Modulate);
        Assert.Equal(new Electron2D.Color(1f, 1f, 0.25f, 0.5f), command.SelfModulate);
        Assert.Equal(new Electron2D.Color(0.5f, 0.5f, 0.25f, 0.5f), command.EffectiveModulate);
    }

    [Fact]
    public void SubmissionFiltersHiddenCanvasItemsAndHiddenCanvasLayers()
    {
        var root = new Electron2D.Node();
        var hiddenLayer = new Electron2D.CanvasLayer { Layer = 1, Visible = false };
        var visibleLayer = new Electron2D.CanvasLayer { Layer = 2 };
        var hiddenParent = new Electron2D.Node2D { Visible = false };
        var hiddenByLayer = CreateSprite("hidden-by-layer", zIndex: 0);
        var hiddenByParent = CreateSprite("hidden-by-parent", zIndex: 0);
        var visible = CreateSprite("visible", zIndex: 0);

        root.AddChild(hiddenLayer);
        root.AddChild(visibleLayer);
        hiddenLayer.AddChild(hiddenByLayer);
        visibleLayer.AddChild(hiddenParent);
        hiddenParent.AddChild(hiddenByParent);
        visibleLayer.AddChild(visible);

        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(root);

        var command = Assert.Single(plan.Commands);
        Assert.Equal("visible", command.DebugName);
        Assert.Equal(2, command.Layer);
    }

    [Fact]
    public void SubmissionWritesTransformRectsFlipFlagsAndTextureBatchKey()
    {
        var texture = new Electron2D.RuntimeTexture2D(8, 4, hasAlpha: false);
        var parent = new Electron2D.Node2D { Position = new Electron2D.Vector2(10f, 20f) };
        var first = new Electron2D.Sprite2D
        {
            Name = "first",
            Texture = texture,
            Centered = false,
            Offset = new Electron2D.Vector2(1f, 2f),
            Position = new Electron2D.Vector2(3f, 4f),
            FlipH = true,
            RegionEnabled = true,
            RegionRect = new Electron2D.Rect2(2f, 1f, 4f, 3f)
        };
        var second = new Electron2D.Sprite2D
        {
            Name = "second",
            Texture = texture,
            Centered = false
        };

        parent.AddChild(first);
        parent.AddChild(second);

        var plan = new Electron2D.CanvasSubmissionContext().BuildPlan(parent);

        Assert.Equal(2, plan.Commands.Count);
        Assert.Equal(1, plan.DrawCallCount);
        Assert.Equal(plan.Commands[0].BatchKey.Texture, plan.Commands[1].BatchKey.Texture);

        var command = plan.Commands.Single(command => command.DebugName == "first");
        Assert.Equal(new Electron2D.Rect2(2f, 1f, 4f, 3f), command.SourceRect);
        Assert.Equal(new Electron2D.Rect2(1f, 2f, 4f, 3f), command.DestinationRect);
        Assert.True(command.Transform.Origin.IsEqualApprox(new Electron2D.Vector2(13f, 24f)));
        Assert.True(command.FlipH);
        Assert.False(command.FlipV);
    }

    private static Electron2D.Sprite2D CreateSprite(string name, int zIndex)
    {
        return new Electron2D.Sprite2D
        {
            Name = name,
            Texture = new Electron2D.RuntimeTexture2D(4, 4, hasAlpha: false),
            Centered = false,
            ZIndex = zIndex
        };
    }
}
