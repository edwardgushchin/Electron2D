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

[Collection(InputStateCollection.Name)]
public sealed class ControlLayoutCoreTests
{
    [Fact]
    public void AnchorsAndOffsetsResolveAgainstViewportAndParentControl()
    {
        var tree = new Electron2D.SceneTree();
        var viewport = Assert.IsType<Electron2D.Viewport>(tree.Root);
        viewport.Size = new Electron2D.Vector2I(200, 100);

        var parent = new Electron2D.Control
        {
            AnchorLeft = 0.25f,
            AnchorTop = 0.10f,
            AnchorRight = 0.75f,
            AnchorBottom = 0.60f,
            OffsetLeft = 10f,
            OffsetTop = 5f,
            OffsetRight = -20f,
            OffsetBottom = 15f
        };
        var child = new Electron2D.Control
        {
            AnchorLeft = 0f,
            AnchorTop = 0f,
            AnchorRight = 1f,
            AnchorBottom = 1f,
            OffsetLeft = 5f,
            OffsetTop = 6f,
            OffsetRight = -7f,
            OffsetBottom = -8f
        };

        parent.AddChild(child);
        viewport.AddChild(parent);

        Assert.Equal(new Electron2D.Vector2(60f, 15f), parent.Position);
        Assert.Equal(new Electron2D.Vector2(70f, 60f), parent.Size);
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(60f, 15f), new Electron2D.Vector2(70f, 60f)), parent.GetRect());
        Assert.Equal(new Electron2D.Rect2(new Electron2D.Vector2(65f, 21f), new Electron2D.Vector2(58f, 46f)), child.GetGlobalRect());
    }

    [Fact]
    public void SetSizeAndResetSizeRespectCombinedMinimumSizeAndGrowDirection()
    {
        var control = new MinimumControl(new Electron2D.Vector2(30f, 20f))
        {
            Position = new Electron2D.Vector2(100f, 50f),
            Size = new Electron2D.Vector2(80f, 40f),
            CustomMinimumSize = new Electron2D.Vector2(40f, 12f),
            GrowHorizontal = Electron2D.GrowDirection.Begin,
            GrowVertical = Electron2D.GrowDirection.Both
        };

        Assert.Equal(new Electron2D.Vector2(40f, 20f), control.GetMinimumSize());
        Assert.Equal(new Electron2D.Vector2(40f, 20f), control.GetCombinedMinimumSize());

        control.SetSize(new Electron2D.Vector2(10f, 4f));

        Assert.Equal(new Electron2D.Vector2(40f, 20f), control.Size);
        Assert.Equal(new Electron2D.Vector2(70f, 42f), control.Position);

        control.CustomMinimumSize = new Electron2D.Vector2(12f, 8f);
        control.ResetSize();

        Assert.Equal(new Electron2D.Vector2(30f, 20f), control.Size);
    }

    [Fact]
    public void ClipContentsPreventsOutsideChildMouseHit()
    {
        ResetInputState();

        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var parent = new RecordingControl("parent", events)
        {
            Position = Electron2D.Vector2.Zero,
            Size = new Electron2D.Vector2(20f, 20f),
            ClipContents = true,
            MouseFilter = Electron2D.MouseFilter.Pass
        };
        var child = new RecordingControl("child", events)
        {
            Position = new Electron2D.Vector2(25f, 0f),
            Size = new Electron2D.Vector2(20f, 20f),
            MouseFilter = Electron2D.MouseFilter.Stop
        };

        parent.AddChild(child);
        tree.Root.AddChild(parent);

        tree.DispatchInput(MousePress(new Electron2D.Vector2(30f, 10f)));

        Assert.Equal(new[] { "parent:_Input", "child:_Input" }, events);

        events.Clear();
        parent.ClipContents = false;

        tree.DispatchInput(MousePress(new Electron2D.Vector2(30f, 10f)));

        Assert.Equal(new[] { "parent:_Input", "child:_Input", "child:_GuiInput" }, events);
    }

    [Fact]
    public void FocusNavigationUsesExplicitPathsAndTreeOrderFallback()
    {
        var tree = new Electron2D.SceneTree();
        var root = new Electron2D.Control
        {
            Name = "root"
        };
        var first = FocusControl("first");
        var second = FocusControl("second");
        var third = FocusControl("third");

        first.FocusNext = new Electron2D.NodePath("../third");
        third.FocusPrevious = new Electron2D.NodePath("../first");

        root.AddChild(first);
        root.AddChild(second);
        root.AddChild(third);
        tree.Root.AddChild(root);

        Assert.Same(third, first.FindNextValidFocus());
        Assert.Same(first, third.FindPrevValidFocus());
        Assert.Same(third, second.FindNextValidFocus());
        Assert.Same(first, second.FindPrevValidFocus());

        second.Hide();

        Assert.Same(third, first.FindNextValidFocus());
        Assert.Same(first, third.FindPrevValidFocus());
    }

    [Fact]
    public void KeyboardNavigationMovesFocusInsideViewport()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var root = new Electron2D.Control
        {
            Name = "root"
        };
        var first = FocusControl("first");
        var second = FocusControl("second");
        var third = FocusControl("third");

        root.AddChild(first);
        root.AddChild(second);
        root.AddChild(third);
        tree.Root.AddChild(root);

        first.GrabFocus();

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.Tab,
            Pressed = true
        });

        Assert.False(first.HasFocus());
        Assert.True(second.HasFocus());

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.Tab,
            Pressed = true,
            ShiftPressed = true
        });

        Assert.True(first.HasFocus());

        first.FocusNeighborRight = new Electron2D.NodePath("../third");

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.Right,
            Pressed = true
        });

        Assert.True(third.HasFocus());
    }

    private static Electron2D.Control FocusControl(string name)
    {
        return new Electron2D.Control
        {
            Name = name,
            FocusMode = Electron2D.FocusMode.All
        };
    }

    private static Electron2D.InputEventMouseButton MousePress(Electron2D.Vector2 position)
    {
        return new Electron2D.InputEventMouseButton
        {
            ButtonIndex = Electron2D.MouseButton.Left,
            Pressed = true,
            Position = position,
            GlobalPosition = position
        };
    }

    private static void ResetInputState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
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

    private sealed class RecordingControl : Electron2D.Control
    {
        private readonly List<string> events;

        public RecordingControl(string name, List<string> events)
        {
            Name = name;
            this.events = events;
        }

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            events.Add($"{Name}:_Input");
        }

        public override void _GuiInput(Electron2D.InputEvent inputEvent)
        {
            events.Add($"{Name}:_GuiInput");
        }
    }
}
