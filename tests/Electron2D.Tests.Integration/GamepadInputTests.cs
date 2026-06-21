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
public sealed class GamepadInputTests
{
    [Fact]
    public void PlatformDeviceEventsUpdateConnectedJoypadsWithoutInputCallbacks()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var recorder = new RecordingInputNode();
        tree.Root.AddChild(recorder);

        var connected = Electron2D.SdlInputEventDispatcher.DispatchPending(
            tree,
            new QueueEventSource(GamepadDeviceEvent(SDL.EventType.GamepadAdded, device: 42)));

        Assert.Equal(0, connected);
        Assert.Equal(new[] { 42 }, Electron2D.Input.GetConnectedJoypads());
        Assert.False(Electron2D.Input.IsJoyKnown(42));
        Assert.Equal(string.Empty, Electron2D.Input.GetJoyName(42));
        Assert.Empty(recorder.Events);

        var disconnected = Electron2D.SdlInputEventDispatcher.DispatchPending(
            tree,
            new QueueEventSource(GamepadDeviceEvent(SDL.EventType.GamepadRemoved, device: 42)));

        Assert.Equal(0, disconnected);
        Assert.Empty(Electron2D.Input.GetConnectedJoypads());
        Assert.Empty(recorder.Events);
    }

    [Fact]
    public void PlatformGamepadButtonAndAxisEventsDispatchInOrderAndUpdateInputState()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var recorder = new RecordingInputNode();
        tree.Root.AddChild(recorder);

        var dispatched = Electron2D.SdlInputEventDispatcher.DispatchPending(
            tree,
            new QueueEventSource(
                GamepadButtonEvent(SDL.EventType.GamepadButtonDown, device: 7, SDL.GamepadButton.South, down: true),
                GamepadAxisEvent(device: 7, SDL.GamepadAxis.LeftX, value: 32767),
                GamepadButtonEvent(SDL.EventType.GamepadButtonUp, device: 7, SDL.GamepadButton.South, down: false)));

        Assert.Equal(3, dispatched);
        Assert.Equal(
            new[]
            {
                "button:7:A:True:1",
                "axis:7:LeftX:1.000",
                "button:7:A:False:0"
            },
            recorder.Events);
        Assert.Equal(new[] { 7 }, Electron2D.Input.GetConnectedJoypads());
        Assert.False(Electron2D.Input.IsJoyButtonPressed(7, Electron2D.JoyButton.A));
        Assert.Equal(1f, Electron2D.Input.GetJoyAxis(7, Electron2D.JoyAxis.LeftX));
    }

    [Fact]
    public void InputMapMatchesGamepadButtonAndSignedAxisBindings()
    {
        ResetInputState();

        Electron2D.InputMap.AddAction("fire");
        Electron2D.InputMap.ActionAddEvent(
            "fire",
            new Electron2D.InputEventJoypadButton { ButtonIndex = Electron2D.JoyButton.A });
        Electron2D.InputMap.AddAction("move_right", 0.25f);
        Electron2D.InputMap.ActionAddEvent(
            "move_right",
            new Electron2D.InputEventJoypadMotion
            {
                Axis = Electron2D.JoyAxis.LeftX,
                AxisValue = 1f
            });

        var tree = new Electron2D.SceneTree();
        tree.DispatchInput(new Electron2D.InputEventJoypadButton
        {
            Device = 1,
            ButtonIndex = Electron2D.JoyButton.A,
            Pressed = true
        });

        Assert.True(Electron2D.Input.IsActionPressed("fire"));
        Assert.True(Electron2D.Input.IsActionJustPressed("fire"));
        Assert.Equal(1f, Electron2D.Input.GetActionStrength("fire"));

        tree.DispatchInput(new Electron2D.InputEventJoypadMotion
        {
            Device = 1,
            Axis = Electron2D.JoyAxis.LeftX,
            AxisValue = 0.75f
        });

        Assert.True(Electron2D.Input.IsActionPressed("move_right"));
        Assert.Equal(0.75f, Electron2D.Input.GetActionStrength("move_right"));
        Assert.True(Electron2D.InputMap.EventIsAction(
            new Electron2D.InputEventJoypadMotion
            {
                Axis = Electron2D.JoyAxis.LeftX,
                AxisValue = 0.75f
            },
            "move_right"));

        tree.DispatchInput(new Electron2D.InputEventJoypadMotion
        {
            Device = 1,
            Axis = Electron2D.JoyAxis.LeftX,
            AxisValue = 0.1f
        });

        Assert.False(Electron2D.Input.IsActionPressed("move_right"));
        Assert.Equal(0f, Electron2D.Input.GetActionStrength("move_right"));

        tree.DispatchInput(new Electron2D.InputEventJoypadButton
        {
            Device = 1,
            ButtonIndex = Electron2D.JoyButton.A,
            Pressed = false
        });

        Assert.False(Electron2D.Input.IsActionPressed("fire"));
    }

    [Fact]
    public void JoypadVibrationIsNoOpForUnknownOrUnsupportedDevicesAndStoresSupportedRequests()
    {
        ResetInputState();

        Electron2D.Input.StartJoyVibration(99, weakMagnitude: 1f, strongMagnitude: 1f, duration: 2f);
        Assert.Equal(Electron2D.Vector2.Zero, Electron2D.Input.GetJoyVibrationStrength(99));
        Assert.Equal(0f, Electron2D.Input.GetJoyVibrationDuration(99));

        Electron2D.Input.ConnectJoypad(1, "No Rumble", isKnown: true, vibrationSupported: false);
        Electron2D.Input.StartJoyVibration(1, weakMagnitude: 1f, strongMagnitude: 1f, duration: 2f);
        Assert.Equal(Electron2D.Vector2.Zero, Electron2D.Input.GetJoyVibrationStrength(1));
        Assert.Equal(0f, Electron2D.Input.GetJoyVibrationDuration(1));

        Electron2D.Input.ConnectJoypad(2, "Rumble Pad", isKnown: true, vibrationSupported: true);
        Electron2D.Input.StartJoyVibration(2, weakMagnitude: 1.5f, strongMagnitude: 0.25f, duration: 3f);

        Assert.Equal(new Electron2D.Vector2(1f, 0.25f), Electron2D.Input.GetJoyVibrationStrength(2));
        Assert.Equal(3f, Electron2D.Input.GetJoyVibrationDuration(2));

        Electron2D.Input.StopJoyVibration(2);

        Assert.Equal(Electron2D.Vector2.Zero, Electron2D.Input.GetJoyVibrationStrength(2));
        Assert.Equal(0f, Electron2D.Input.GetJoyVibrationDuration(2));
    }

    private static SDL.Event GamepadDeviceEvent(SDL.EventType eventType, uint device)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.GDevice = new SDL.GamepadDeviceEvent
        {
            Type = eventType,
            Timestamp = 1,
            Which = device
        };
        return sdlEvent;
    }

    private static SDL.Event GamepadButtonEvent(
        SDL.EventType eventType,
        uint device,
        SDL.GamepadButton button,
        bool down)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.GButton = new SDL.GamepadButtonEvent
        {
            Type = eventType,
            Timestamp = 2,
            Which = device,
            Button = checked((byte)button),
            Down = down
        };
        return sdlEvent;
    }

    private static SDL.Event GamepadAxisEvent(uint device, SDL.GamepadAxis axis, short value)
    {
        var sdlEvent = default(SDL.Event);
        sdlEvent.GAxis = new SDL.GamepadAxisEvent
        {
            Type = SDL.EventType.GamepadAxisMotion,
            Timestamp = 3,
            Which = device,
            Axis = checked((byte)axis),
            Value = value
        };
        return sdlEvent;
    }

    private static void ResetInputState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
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
                case Electron2D.InputEventJoypadButton button:
                    Events.Add($"button:{button.Device}:{button.ButtonIndex}:{button.Pressed}:{button.Pressure:0}");
                    break;
                case Electron2D.InputEventJoypadMotion motion:
                    Events.Add($"axis:{motion.Device}:{motion.Axis}:{motion.AxisValue.ToString("0.000", CultureInfo.InvariantCulture)}");
                    break;
            }
        }
    }
}
