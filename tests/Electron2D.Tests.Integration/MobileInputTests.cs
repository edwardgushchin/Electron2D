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
using System.Globalization;
using SDL3;
using Xunit;

namespace Electron2D.Tests.Integration;

[Collection(InputStateCollection.Name)]
public sealed class MobileInputTests
{
    [Fact]
    public void TouchFingerEventsMapToScreenTouchAndDragAndDispatchInOrder()
    {
        ResetMobileState();

        var tree = new Electron2D.SceneTree();
        var recorder = new RecordingInputNode();
        tree.Root.AddChild(recorder);

        var dispatched = Electron2D.SdlInputEventDispatcher.DispatchPending(
            tree,
            new QueueEventSource(
                FingerEvent(SDL.EventType.FingerDown, touchId: 2, fingerId: 7, x: 0.25f, y: 0.75f, dx: 0f, dy: 0f, pressure: 0.8f),
                FingerEvent(SDL.EventType.FingerMotion, touchId: 2, fingerId: 7, x: 0.35f, y: 0.70f, dx: 0.10f, dy: -0.05f, pressure: 0.6f),
                FingerEvent(SDL.EventType.FingerUp, touchId: 2, fingerId: 7, x: 0.35f, y: 0.70f, dx: 0f, dy: 0f, pressure: 0f),
                FingerEvent(SDL.EventType.FingerCanceled, touchId: 2, fingerId: 7, x: 0.35f, y: 0.70f, dx: 0f, dy: 0f, pressure: 0f)));

        Assert.Equal(4, dispatched);
        Assert.Equal(
            new[]
            {
                "touch:31:2:7:0.25,0.75:True:False",
                "drag:31:2:7:0.35,0.70:0.10,-0.05:0.60",
                "touch:31:2:7:0.35,0.70:False:False",
                "touch:31:2:7:0.35,0.70:False:True"
            },
            recorder.Events);
    }

    [Fact]
    public void InvalidTouchIdsFailClosed()
    {
        var tooLargeTouch = FingerEvent(SDL.EventType.FingerDown, touchId: (ulong)int.MaxValue + 1UL, fingerId: 7, x: 0.5f, y: 0.5f, dx: 0f, dy: 0f, pressure: 1f);
        var tooLargeFinger = FingerEvent(SDL.EventType.FingerDown, touchId: 2, fingerId: (ulong)int.MaxValue + 1UL, x: 0.5f, y: 0.5f, dx: 0f, dy: 0f, pressure: 1f);

        Assert.Empty(Electron2D.SdlInputEventMapper.Map(tooLargeTouch));
        Assert.Empty(Electron2D.SdlInputEventMapper.Map(tooLargeFinger));
    }

    [Fact]
    public void MobileBackAndMenuKeycodesMapToInputEventKey()
    {
        var back = Assert.IsType<Electron2D.InputEventKey>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(KeyEvent(SDL.Keycode.AcBack, SDL.Scancode.ACBack))));
        var menu = Assert.IsType<Electron2D.InputEventKey>(
            Assert.Single(Electron2D.SdlInputEventMapper.Map(KeyEvent(SDL.Keycode.Menu, SDL.Scancode.Menu))));

        Assert.Equal(Electron2D.Key.Back, back.Keycode);
        Assert.Equal(Electron2D.Key.Back, back.PhysicalKeycode);
        Assert.Equal(Electron2D.Key.Menu, menu.Keycode);
        Assert.Equal(Electron2D.Key.Menu, menu.PhysicalKeycode);
    }

    [Fact]
    public void DisplayServerTracksVirtualKeyboardOrientationAndSafeAreaState()
    {
        ResetMobileState();

        Assert.Equal(new Electron2D.Rect2I(0, 0, 0, 0), Electron2D.DisplayServer.GetDisplaySafeArea());
        Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.Landscape, Electron2D.DisplayServer.ScreenGetOrientation());

        Electron2D.DisplayServer.ScreenSetOrientation(Electron2D.DisplayServer.ScreenOrientation.Portrait);
        Electron2D.DisplayServer.VirtualKeyboardShow(
            existingText: "42",
            position: new Electron2D.Rect2(1f, 2f, 3f, 4f),
            type: Electron2D.DisplayServer.VirtualKeyboardType.Number,
            maxLength: 6,
            cursorStart: 1,
            cursorEnd: 2);
        Electron2D.DisplayServer.SetVirtualKeyboardHeightForTests(240);
        Electron2D.DisplayServer.SetDisplaySafeAreaForTests(new Electron2D.Rect2I(0, 10, 390, 780));

        Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.Portrait, Electron2D.DisplayServer.ScreenGetOrientation());
        Assert.Equal(240, Electron2D.DisplayServer.VirtualKeyboardGetHeight());
        Assert.Equal(new Electron2D.Rect2I(0, 10, 390, 780), Electron2D.DisplayServer.GetDisplaySafeArea());

        Electron2D.DisplayServer.VirtualKeyboardHide();

        Assert.Equal(0, Electron2D.DisplayServer.VirtualKeyboardGetHeight());
    }

    [Fact]
    public void PlatformDisplayEventsUpdateDisplayServerWithoutInputCallbacks()
    {
        ResetMobileState();

        var tree = new Electron2D.SceneTree();
        var recorder = new RecordingInputNode();
        tree.Root.AddChild(recorder);

        var dispatched = Electron2D.SdlInputEventDispatcher.DispatchPending(
            tree,
            new QueueEventSource(
                DisplayOrientationEvent(SDL.DisplayOrientation.Portrait),
                WindowSafeAreaChangedEvent(width: 390, height: 780)));

        Assert.Equal(0, dispatched);
        Assert.Empty(recorder.Events);
        Assert.Equal(Electron2D.DisplayServer.ScreenOrientation.Portrait, Electron2D.DisplayServer.ScreenGetOrientation());
        Assert.Equal(new Electron2D.Rect2I(0, 0, 390, 780), Electron2D.DisplayServer.GetDisplaySafeArea());
    }

    private static void ResetMobileState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.DisplayServer.ResetForTests();
    }

    private static SDL.Event FingerEvent(
        SDL.EventType eventType,
        ulong touchId,
        ulong fingerId,
        float x,
        float y,
        float dx,
        float dy,
        float pressure)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.TFinger = new SDL.TouchFingerEvent
        {
            Type = eventType,
            Timestamp = 11,
            TouchID = touchId,
            FingerID = fingerId,
            X = x,
            Y = y,
            DX = dx,
            DY = dy,
            Pressure = pressure,
            WindowID = 31
        };
        return sdlEvent;
    }

    private static SDL.Event KeyEvent(SDL.Keycode keycode, SDL.Scancode scancode)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Key = new SDL.KeyboardEvent
        {
            Type = SDL.EventType.KeyDown,
            Timestamp = 12,
            WindowID = 32,
            Which = 4,
            Scancode = scancode,
            Key = keycode,
            Down = true
        };
        return sdlEvent;
    }

    private static SDL.Event DisplayOrientationEvent(SDL.DisplayOrientation orientation)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Display = new SDL.DisplayEvent
        {
            Type = SDL.EventType.DisplayOrientation,
            Timestamp = 13,
            DisplayID = 1,
            Data1 = (int)orientation
        };
        return sdlEvent;
    }

    private static SDL.Event WindowSafeAreaChangedEvent(int width, int height)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.Window = new SDL.WindowEvent
        {
            Type = SDL.EventType.WindowSafeAreaChanged,
            Timestamp = 14,
            WindowID = 33,
            Data1 = width,
            Data2 = height
        };
        return sdlEvent;
    }

    private sealed class QueueEventSource : Electron2D.ISdlEventSource
    {
        private readonly Queue<SDL.Event> events;

        public QueueEventSource(params SDL.Event[] events)
        {
            this.events = new Queue<SDL.Event>(events);
        }

        public bool PollEvent(out SDL.Event sdlEvent)
        {
            if (events.Count == 0)
            {
                sdlEvent = default;
                return false;
            }

            sdlEvent = events.Dequeue();
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
                case Electron2D.InputEventScreenTouch touch:
                    Events.Add(
                        $"touch:{touch.WindowId}:{touch.Device}:{touch.Index}:{Format(touch.Position.X)},{Format(touch.Position.Y)}:{touch.Pressed}:{touch.Canceled}");
                    break;
                case Electron2D.InputEventScreenDrag drag:
                    Events.Add(
                        $"drag:{drag.WindowId}:{drag.Device}:{drag.Index}:{Format(drag.Position.X)},{Format(drag.Position.Y)}:{Format(drag.Relative.X)},{Format(drag.Relative.Y)}:{Format(drag.Pressure)}");
                    break;
            }
        }

        private static string Format(float value)
        {
            return value.ToString("0.00", CultureInfo.InvariantCulture);
        }
    }
}
