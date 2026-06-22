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

public sealed class ContainerLayoutTests
{
    [Fact]
    public void HBoxContainerUsesMinimumSizeSeparationAndExpandFillFlags()
    {
        var tree = new Electron2D.SceneTree();
        var container = new Electron2D.HBoxContainer
        {
            Size = new Electron2D.Vector2(120f, 30f)
        };
        var fixedChild = new MinimumControl(new Electron2D.Vector2(20f, 10f))
        {
            SizeFlagsVertical = Electron2D.SizeFlags.ShrinkCenter
        };
        var expandingChild = new MinimumControl(new Electron2D.Vector2(30f, 12f))
        {
            SizeFlagsHorizontal = Electron2D.SizeFlags.ExpandFill,
            SizeFlagsVertical = Electron2D.SizeFlags.Fill,
            SizeFlagsStretchRatio = 2f
        };
        var trailingChild = new MinimumControl(new Electron2D.Vector2(10f, 8f))
        {
            SizeFlagsHorizontal = Electron2D.SizeFlags.ShrinkEnd,
            SizeFlagsVertical = Electron2D.SizeFlags.ShrinkEnd
        };

        container.AddThemeConstantOverride("separation", 5);
        container.AddChild(fixedChild);
        container.AddChild(expandingChild);
        container.AddChild(trailingChild);
        tree.Root.AddChild(container);

        tree.ProcessFrame(0d);

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(0f, 10f), new Electron2D.Vector2(20f, 10f)), fixedChild.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(25f, 0f), new Electron2D.Vector2(80f, 30f)), expandingChild.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(110f, 22f), new Electron2D.Vector2(10f, 8f)), trailingChild.GetRect());
        Assert.Equal(new Electron2D.Vector2(70f, 12f), container.GetMinimumSize());
    }

    [Fact]
    public void VBoxContainerAlignsChildrenWhenNoChildExpands()
    {
        var tree = new Electron2D.SceneTree();
        var container = new Electron2D.VBoxContainer
        {
            Size = new Electron2D.Vector2(40f, 100f),
            Alignment = Electron2D.BoxContainerAlignmentMode.Center
        };
        var first = new MinimumControl(new Electron2D.Vector2(10f, 10f));
        var second = new MinimumControl(new Electron2D.Vector2(20f, 20f));

        container.AddThemeConstantOverride("separation", 5);
        container.AddChild(first);
        container.AddChild(second);
        tree.Root.AddChild(container);

        tree.ProcessFrame(0d);

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(0f, 32.5f), new Electron2D.Vector2(40f, 10f)), first.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(0f, 47.5f), new Electron2D.Vector2(40f, 20f)), second.GetRect());
        Assert.Equal(new Electron2D.Vector2(20f, 35f), container.GetMinimumSize());
    }

    [Fact]
    public void GridContainerUsesColumnAndRowMinimums()
    {
        var tree = new Electron2D.SceneTree();
        var container = new Electron2D.GridContainer
        {
            Columns = 2,
            Size = new Electron2D.Vector2(100f, 80f)
        };
        var first = new MinimumControl(new Electron2D.Vector2(10f, 10f));
        var second = new MinimumControl(new Electron2D.Vector2(30f, 20f));
        var third = new MinimumControl(new Electron2D.Vector2(15f, 12f));

        container.AddThemeConstantOverride("h_separation", 2);
        container.AddThemeConstantOverride("v_separation", 3);
        container.AddChild(first);
        container.AddChild(new Electron2D.Node());
        container.AddChild(second);
        container.AddChild(third);
        tree.Root.AddChild(container);

        tree.ProcessFrame(0d);

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(0f, 0f), new Electron2D.Vector2(15f, 20f)), first.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(17f, 0f), new Electron2D.Vector2(30f, 20f)), second.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(0f, 23f), new Electron2D.Vector2(15f, 12f)), third.GetRect());
        Assert.Equal(new Electron2D.Vector2(47f, 35f), container.GetMinimumSize());
    }

    [Fact]
    public void MarginAndCenterContainersRespectInsetsAndCentering()
    {
        var tree = new Electron2D.SceneTree();
        var margin = new Electron2D.MarginContainer
        {
            Size = new Electron2D.Vector2(80f, 40f)
        };
        var child = new MinimumControl(new Electron2D.Vector2(10f, 6f));
        var center = new Electron2D.CenterContainer
        {
            Position = new Electron2D.Vector2(0f, 50f),
            Size = new Electron2D.Vector2(80f, 40f)
        };
        var centeredChild = new MinimumControl(new Electron2D.Vector2(20f, 10f));

        margin.AddThemeConstantOverride("margin_left", 5);
        margin.AddThemeConstantOverride("margin_top", 3);
        margin.AddThemeConstantOverride("margin_right", 7);
        margin.AddThemeConstantOverride("margin_bottom", 4);
        margin.AddChild(child);
        center.AddChild(centeredChild);
        tree.Root.AddChild(margin);
        tree.Root.AddChild(center);

        tree.ProcessFrame(0d);

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(5f, 3f), new Electron2D.Vector2(68f, 33f)), child.GetRect());
        Assert.Equal(new Electron2D.Vector2(22f, 13f), margin.GetMinimumSize());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(30f, 15f), new Electron2D.Vector2(20f, 10f)), centeredChild.GetRect());

        center.UseTopLeft = true;
        tree.ProcessFrame(0d);

        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(40f, 20f), new Electron2D.Vector2(20f, 10f)), centeredChild.GetRect());
    }

    [Fact]
    public void ScrollContainerClipsOffsetsAndEnsuresDescendantVisible()
    {
        var tree = new Electron2D.SceneTree();
        var scroll = new Electron2D.ScrollContainer
        {
            Size = new Electron2D.Vector2(50f, 30f),
            ScrollHorizontal = 500,
            ScrollVertical = 500
        };
        var content = new MinimumControl(new Electron2D.Vector2(120f, 90f));
        var target = new MinimumControl(new Electron2D.Vector2(10f, 10f))
        {
            Position = new Electron2D.Vector2(100f, 70f),
            Size = new Electron2D.Vector2(10f, 10f)
        };

        content.AddChild(target);
        scroll.AddChild(content);
        tree.Root.AddChild(scroll);

        tree.ProcessFrame(0d);

        Assert.True(scroll.ClipContents);
        Assert.Equal(70, scroll.ScrollHorizontal);
        Assert.Equal(60, scroll.ScrollVertical);
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(-70f, -60f), new Electron2D.Vector2(120f, 90f)), content.GetRect());

        scroll.ScrollHorizontal = 0;
        scroll.ScrollVertical = 0;
        scroll.EnsureControlVisible(target);
        tree.ProcessFrame(0d);

        Assert.Equal(60, scroll.ScrollHorizontal);
        Assert.Equal(50, scroll.ScrollVertical);
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(-60f, -50f), new Electron2D.Vector2(120f, 90f)), content.GetRect());
    }

    private sealed class MinimumControl : Electron2D.Control
    {
        private readonly Electron2D.Vector2 minimumSize;

        public MinimumControl(Electron2D.Vector2 minimumSize)
        {
            this.minimumSize = minimumSize;
        }

        public override Electron2D.Vector2 _GetMinimumSize()
        {
            return minimumSize;
        }
    }
}
