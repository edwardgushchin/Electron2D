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
public sealed class InputDispatchControlTests
{
    [Fact]
    public void InputTraversalStopsAfterViewportMarksEventHandled()
    {
        ResetInputState();

        Electron2D.InputMap.AddAction("jump");
        Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });

        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var handler = new HandlingNode("handler", events);
        var skipped = new RecordingNode("skipped", events);
        tree.Root.AddChild(handler);
        tree.Root.AddChild(skipped);

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.Space,
            Pressed = true
        });

        Assert.Equal(new[] { "handler:_Input:True" }, events);
        Assert.True(Electron2D.Input.IsActionPressed("jump"));
    }

    [Fact]
    public void ControlMouseFilterRoutesTopmostHitAndBubblesPassToParent()
    {
        ResetInputState();

        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var parent = new RecordingControl("parent", events)
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(100f, 100f),
            MouseFilter = Electron2D.MouseFilter.Pass
        };
        var child = new RecordingControl("child", events)
        {
            Position = new Electron2D.Vector2(10f, 10f),
            Size = new Electron2D.Vector2(20f, 20f),
            MouseFilter = Electron2D.MouseFilter.Pass
        };
        parent.AddChild(child);
        tree.Root.AddChild(parent);

        tree.DispatchInput(MousePress(new Electron2D.Vector2(15f, 15f)));

        Assert.Equal(
            new[]
            {
                "parent:_Input",
                "child:_Input",
                "child:_GuiInput",
                "parent:_GuiInput"
            },
            events);

        events.Clear();
        child.MouseFilter = Electron2D.MouseFilter.Stop;

        tree.DispatchInput(MousePress(new Electron2D.Vector2(15f, 15f)));

        Assert.Equal(
            new[]
            {
                "parent:_Input",
                "child:_Input",
                "child:_GuiInput"
            },
            events);

        events.Clear();
        child.MouseFilter = Electron2D.MouseFilter.Ignore;

        tree.DispatchInput(MousePress(new Electron2D.Vector2(15f, 15f)));

        Assert.Equal(
            new[]
            {
                "parent:_Input",
                "child:_Input",
                "parent:_GuiInput"
            },
            events);
    }

    [Fact]
    public void FocusedControlReceivesKeyboardGuiInputUntilFocusIsReleased()
    {
        ResetInputState();

        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var first = new RecordingControl("first", events)
        {
            FocusMode = Electron2D.FocusMode.All
        };
        var second = new RecordingControl("second", events)
        {
            FocusMode = Electron2D.FocusMode.All
        };
        tree.Root.AddChild(first);
        tree.Root.AddChild(second);

        first.GrabFocus();

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.A,
            Pressed = true
        });

        Assert.True(first.HasFocus());
        Assert.False(second.HasFocus());
        Assert.Equal(
            new[]
            {
                "first:_Input",
                "second:_Input",
                "first:_GuiInput"
            },
            events);

        events.Clear();
        first.ReleaseFocus();

        tree.DispatchInput(new Electron2D.InputEventKey
        {
            Keycode = Electron2D.Key.A,
            Pressed = true
        });

        Assert.False(first.HasFocus());
        Assert.Equal(new[] { "first:_Input", "second:_Input" }, events);
    }

    [Fact]
    public void MousePressGrabsFocusForClickableControlBeforeGuiInput()
    {
        ResetInputState();

        var events = new List<string>();
        var tree = new Electron2D.SceneTree();
        var control = new FocusRecordingControl("control", events)
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(32f, 32f),
            FocusMode = Electron2D.FocusMode.Click,
            MouseFilter = Electron2D.MouseFilter.Stop
        };
        tree.Root.AddChild(control);

        tree.DispatchInput(MousePress(new Electron2D.Vector2(8f, 8f)));

        Assert.True(control.HasFocus());
        Assert.Equal(
            new[]
            {
                "control:_Input",
                "control:_GuiInput:True"
            },
            events);
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

    private sealed class RecordingNode : Electron2D.Node
    {
        private readonly List<string> events;

        public RecordingNode(string name, List<string> events)
        {
            Name = name;
            this.events = events;
        }

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            events.Add($"{Name}:_Input");
        }
    }

    private sealed class HandlingNode : Electron2D.Node
    {
        private readonly List<string> events;

        public HandlingNode(string name, List<string> events)
        {
            Name = name;
            this.events = events;
        }

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            events.Add($"{Name}:_Input:{Electron2D.Input.IsActionPressed("jump")}");
            GetViewport()?.SetInputAsHandled();
        }
    }

    private class RecordingControl : Electron2D.Control
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

    private sealed class FocusRecordingControl : RecordingControl
    {
        private readonly List<string> events;

        public FocusRecordingControl(string name, List<string> events)
            : base(name, events)
        {
            this.events = events;
        }

        public override void _GuiInput(Electron2D.InputEvent inputEvent)
        {
            events.Add($"{Name}:_GuiInput:{HasFocus()}");
        }
    }
}
