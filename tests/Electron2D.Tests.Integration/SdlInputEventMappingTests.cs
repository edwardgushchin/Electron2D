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
using System.Runtime.InteropServices;
using SDL3;
using Xunit;

namespace Electron2D.Tests.Integration;

public sealed class SdlInputEventMappingTests
{
    [Fact]
    public void KeyboardDownAndUpMapToInputEventKey()
    {
        var down = Assert.IsType<Electron2D.InputEventKey>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(KeyEvent(SDL.EventType.KeyDown, down: true, repeat: true))));
        var up = Assert.IsType<Electron2D.InputEventKey>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(KeyEvent(SDL.EventType.KeyUp, down: false, repeat: false))));

        Assert.Equal(17, down.WindowId);
        Assert.Equal(3, down.Device);
        Assert.True(down.Pressed);
        Assert.True(down.Echo);
        Assert.Equal(Electron2D.Key.A, down.Keycode);
        Assert.Equal(Electron2D.Key.A, down.PhysicalKeycode);
        Assert.Equal(Electron2D.Key.A, down.KeyLabel);
        Assert.Equal(Electron2D.KeyLocation.Unspecified, down.Location);
        Assert.Equal(0, down.Unicode);
        Assert.True(down.CtrlPressed);
        Assert.True(down.ShiftPressed);
        Assert.False(down.AltPressed);
        Assert.False(down.MetaPressed);

        Assert.False(up.Pressed);
        Assert.False(up.Echo);
        Assert.Equal(Electron2D.Key.A, up.Keycode);
    }

    [Fact]
    public void MouseButtonMapsPositionButtonAndClickState()
    {
        var mapped = Assert.IsType<Electron2D.InputEventMouseButton>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(MouseButtonEvent(SDL.EventType.MouseButtonDown, button: 1, down: true, clicks: 2))));

        Assert.Equal(12, mapped.WindowId);
        Assert.Equal(4, mapped.Device);
        Assert.True(mapped.Pressed);
        Assert.True(mapped.DoubleClick);
        Assert.False(mapped.Canceled);
        Assert.Equal(1f, mapped.Factor);
        Assert.Equal(Electron2D.MouseButton.Left, mapped.ButtonIndex);
        Assert.Equal(new Electron2D.Vector2(100f, 110f), mapped.Position);
        Assert.Equal(new Electron2D.Vector2(100f, 110f), mapped.GlobalPosition);
    }

    [Fact]
    public void MouseMotionMapsPositionRelativeAndButtonMask()
    {
        var mapped = Assert.IsType<Electron2D.InputEventMouseMotion>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(MouseMotionEvent())));

        Assert.Equal(13, mapped.WindowId);
        Assert.Equal(5, mapped.Device);
        Assert.Equal(
            Electron2D.MouseButtonMask.Left | Electron2D.MouseButtonMask.Right,
            mapped.ButtonMask);
        Assert.Equal(new Electron2D.Vector2(200f, 210f), mapped.Position);
        Assert.Equal(new Electron2D.Vector2(200f, 210f), mapped.GlobalPosition);
        Assert.Equal(new Electron2D.Vector2(4f, -2f), mapped.Relative);
        Assert.Equal(new Electron2D.Vector2(4f, -2f), mapped.ScreenRelative);
        Assert.Equal(Electron2D.Vector2.Zero, mapped.Velocity);
        Assert.Equal(Electron2D.Vector2.Zero, mapped.ScreenVelocity);
    }

    [Fact]
    public void MouseWheelMapsToElectron2DMouseButtonWheelEvent()
    {
        var mapped = Assert.IsType<Electron2D.InputEventMouseButton>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(MouseWheelEvent(y: -3f, direction: SDL.MouseWheelDirection.Normal))));
        var flipped = Assert.IsType<Electron2D.InputEventMouseButton>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(MouseWheelEvent(y: -3f, direction: SDL.MouseWheelDirection.Flipped))));

        Assert.True(mapped.Pressed);
        Assert.Equal(Electron2D.MouseButton.WheelDown, mapped.ButtonIndex);
        Assert.Equal(3f, mapped.Factor);
        Assert.Equal(new Electron2D.Vector2(20f, 30f), mapped.Position);

        Assert.Equal(Electron2D.MouseButton.WheelUp, flipped.ButtonIndex);
        Assert.Equal(3f, flipped.Factor);
    }

    [Fact]
    public void TextInputMapsEachUnicodeScalarToInputEventKeyUnicode()
    {
        var sdlEvent = TextInputEvent("AЖ");
        try
        {
            var mapped = Electron2D.SdlInputEventMapper.Map(sdlEvent);

            Assert.Collection(
                mapped,
                first =>
                {
                    var key = Assert.IsType<Electron2D.InputEventKey>(first);
                    Assert.True(key.Pressed);
                    Assert.Equal(Electron2D.Key.None, key.Keycode);
                    Assert.Equal(65, key.Unicode);
                },
                second =>
                {
                    var key = Assert.IsType<Electron2D.InputEventKey>(second);
                    Assert.True(key.Pressed);
                    Assert.Equal(Electron2D.Key.None, key.Keycode);
                    Assert.Equal(1046, key.Unicode);
                });
        }
        finally
        {
            Marshal.FreeCoTaskMem(sdlEvent.Text.Text);
        }
    }

    [Fact]
    public void SdlInputDispatcherPreservesMappedEventOrder()
    {
        var tree = new Electron2D.SceneTree();
        var recorder = new RecordingInputNode();
        tree.Root.AddChild(recorder);
        var textEvent = TextInputEvent("x");
        try
        {
            var source = new QueueEventSource(
                KeyEvent(SDL.EventType.KeyDown, down: true, repeat: false),
                MouseMotionEvent(),
                textEvent);

            var dispatched = Electron2D.SdlInputEventDispatcher.DispatchPending(tree, source);

            Assert.Equal(3, dispatched);
            Assert.Equal(
                new[]
                {
                    "key:A:True:0",
                    "motion:200,210:4,-2",
                    "key:None:True:120"
                },
                recorder.Events);
        }
        finally
        {
            Marshal.FreeCoTaskMem(textEvent.Text.Text);
        }
    }

    private static SDL.Event KeyEvent(SDL.EventType eventType, bool down, bool repeat)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Key = new SDL.KeyboardEvent
        {
            Type = eventType,
            Timestamp = 1,
            WindowID = 17,
            Which = 3,
            Scancode = SDL.Scancode.A,
            Key = SDL.Keycode.A,
            Mod = SDL.Keymod.Ctrl | SDL.Keymod.LShift,
            Down = down,
            Repeat = repeat
        };
        return sdlEvent;
    }

    private static SDL.Event MouseButtonEvent(SDL.EventType eventType, byte button, bool down, byte clicks)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Button = new SDL.MouseButtonEvent
        {
            Type = eventType,
            Timestamp = 2,
            WindowID = 12,
            Which = 4,
            Button = button,
            Down = down,
            Clicks = clicks,
            X = 100f,
            Y = 110f
        };
        return sdlEvent;
    }

    private static SDL.Event MouseMotionEvent()
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Motion = new SDL.MouseMotionEvent
        {
            Type = SDL.EventType.MouseMotion,
            Timestamp = 3,
            WindowID = 13,
            Which = 5,
            State = SDL.MouseButtonFlags.Left | SDL.MouseButtonFlags.Right,
            X = 200f,
            Y = 210f,
            XRel = 4f,
            YRel = -2f
        };
        return sdlEvent;
    }

    private static SDL.Event MouseWheelEvent(float y, SDL.MouseWheelDirection direction)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Wheel = new SDL.MouseWheelEvent
        {
            Type = SDL.EventType.MouseWheel,
            Timestamp = 4,
            WindowID = 14,
            Which = 6,
            X = 0f,
            Y = y,
            Direction = direction,
            MouseX = 20f,
            MouseY = 30f
        };
        return sdlEvent;
    }

    private static SDL.Event TextInputEvent(string text)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Text = new SDL.TextInputEvent
        {
            Type = SDL.EventType.TextInput,
            Timestamp = 5,
            WindowID = 15,
            Text = Marshal.StringToCoTaskMemUTF8(text)
        };
        return sdlEvent;
    }

    private sealed class QueueEventSource : Electron2D.ISdlEventSource
    {
        private readonly Queue<SDL.Event> _events;

        public QueueEventSource(params SDL.Event[] events)
        {
            _events = new Queue<SDL.Event>(events);
        }

        public bool PollEvent(out SDL.Event sdlEvent)
        {
            if (_events.Count == 0)
            {
                sdlEvent = default;
                return false;
            }

            sdlEvent = _events.Dequeue();
            return true;
        }
    }

    private sealed class RecordingInputNode : Electron2D.Node
    {
        public List<string> Events { get; } = [];

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            switch (inputEvent)
            {
                case Electron2D.InputEventKey key:
                    Events.Add($"key:{key.Keycode}:{key.Pressed}:{key.Unicode}");
                    break;
                case Electron2D.InputEventMouseMotion motion:
                    Events.Add($"motion:{motion.Position.X:0},{motion.Position.Y:0}:{motion.Relative.X:0},{motion.Relative.Y:0}");
                    break;
            }
        }
    }
}
