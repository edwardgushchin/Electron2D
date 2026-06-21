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
public sealed class InputMapActionTests
{
    [Fact]
    public void InputMapStoresEventsDeadzonesAndMatchesSupportedEventTypes()
    {
        ResetInputState();

        Electron2D.InputMap.AddAction("jump", 0.4f);
        Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
        Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventMouseButton { ButtonIndex = Electron2D.MouseButton.Left });

        Assert.True(Electron2D.InputMap.HasAction("jump"));
        Assert.Equal(0.4f, Electron2D.InputMap.ActionGetDeadzone("jump"));
        Assert.Equal(new[] { "jump" }, Electron2D.InputMap.GetActions());
        Assert.Equal(2, Electron2D.InputMap.ActionGetEvents("jump").Length);

        Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space }, "jump"));
        Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventMouseButton { ButtonIndex = Electron2D.MouseButton.Left }, "jump"));
        Assert.False(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.A }, "jump"));

        Assert.False(Electron2D.InputMap.EventIsAction(
            new Electron2D.InputEventAction { Action = "jump", Pressed = true, Strength = 0.3f },
            "jump"));
        Assert.True(Electron2D.InputMap.EventIsAction(
            new Electron2D.InputEventAction { Action = "jump", Pressed = true, Strength = 0.8f },
            "jump"));

        var snapshot = Electron2D.InputMap.ActionGetEvents("jump");
        Assert.IsType<Electron2D.InputEventKey>(snapshot[0]).Keycode = Electron2D.Key.A;

        Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space }, "jump"));
    }

    [Fact]
    public void InputTracksJustPressedAndSimultaneousBindingsFromSceneTreeDispatch()
    {
        ResetInputState();

        Electron2D.InputMap.AddAction("jump");
        Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
        Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Enter });

        var tree = new Electron2D.SceneTree();
        var recorder = new ActionStateRecorder();
        tree.Root.AddChild(recorder);

        tree.DispatchInput(KeyEvent(Electron2D.Key.Space, pressed: true));

        Assert.True(Electron2D.Input.IsActionPressed("jump"));
        Assert.True(Electron2D.Input.IsActionJustPressed("jump"));
        Assert.Equal(new[] { "True:True" }, recorder.States);

        tree.ProcessFrame(0d);
        Assert.True(Electron2D.Input.IsActionPressed("jump"));
        Assert.False(Electron2D.Input.IsActionJustPressed("jump"));

        tree.DispatchInput(KeyEvent(Electron2D.Key.Enter, pressed: true));
        Assert.True(Electron2D.Input.IsActionPressed("jump"));
        Assert.False(Electron2D.Input.IsActionJustPressed("jump"));

        tree.DispatchInput(KeyEvent(Electron2D.Key.Space, pressed: false));
        Assert.True(Electron2D.Input.IsActionPressed("jump"));

        tree.DispatchInput(KeyEvent(Electron2D.Key.Enter, pressed: false));
        Assert.False(Electron2D.Input.IsActionPressed("jump"));
    }

    [Fact]
    public void InputAppliesDeadzoneAndBuildsNormalizedVectorFromActionStrength()
    {
        ResetInputState();

        Electron2D.InputMap.AddAction("left", 0f);
        Electron2D.InputMap.AddAction("right", 0.5f);
        Electron2D.InputMap.AddAction("up", 0f);
        Electron2D.InputMap.AddAction("down", 0f);

        var tree = new Electron2D.SceneTree();
        tree.DispatchInput(new Electron2D.InputEventAction { Action = "right", Pressed = true, Strength = 0.4f });
        Assert.Equal(0f, Electron2D.Input.GetActionStrength("right"));

        tree.DispatchInput(new Electron2D.InputEventAction { Action = "right", Pressed = true, Strength = 0.8f });
        tree.DispatchInput(new Electron2D.InputEventAction { Action = "up", Pressed = true, Strength = 0.6f });

        Assert.Equal(0.8f, Electron2D.Input.GetActionStrength("right"));
        Assert.Equal(new Electron2D.Vector2(0.8f, -0.6f), Electron2D.Input.GetVector("left", "right", "up", "down"));

        tree.DispatchInput(new Electron2D.InputEventAction { Action = "right", Pressed = true, Strength = 1f });
        tree.DispatchInput(new Electron2D.InputEventAction { Action = "up", Pressed = true, Strength = 1f });

        var normalized = Electron2D.Input.GetVector("left", "right", "up", "down");
        Assert.True(MathF.Abs(normalized.X - 0.70710677f) < 0.0001f);
        Assert.True(MathF.Abs(normalized.Y + 0.70710677f) < 0.0001f);
        Assert.Equal(Electron2D.Vector2.Zero, Electron2D.Input.GetVector("left", "right", "up", "down", deadzone: 1f));
    }

    [Fact]
    public void InputMapProjectSettingsRoundTripsActionsWithoutPublicPersistenceApi()
    {
        ResetInputState();

        var directory = Path.Combine(Path.GetTempPath(), "electron2d-input-map-" + Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "input-map.json");
        Directory.CreateDirectory(directory);

        try
        {
            Electron2D.InputMap.AddAction("jump", 0.25f);
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space });
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventMouseButton { ButtonIndex = Electron2D.MouseButton.Right });
            Electron2D.InputMap.ActionAddEvent("jump", new Electron2D.InputEventJoypadButton { ButtonIndex = Electron2D.JoyButton.A });
            Electron2D.InputMap.ActionAddEvent(
                "jump",
                new Electron2D.InputEventJoypadMotion
                {
                    Axis = Electron2D.JoyAxis.LeftX,
                    AxisValue = 1f
                });
            Electron2D.InputMap.AddAction("move_left", 0.1f);
            Electron2D.InputMap.ActionAddEvent("move_left", new Electron2D.InputEventKey { Keycode = Electron2D.Key.A });

            Electron2D.InputMapProjectSettings.Save(path);

            Electron2D.InputMap.ClearForTests();
            Electron2D.InputMap.AddAction("old_action");
            Electron2D.InputMapProjectSettings.Load(path);

            Assert.False(Electron2D.InputMap.HasAction("old_action"));
            Assert.Equal(new[] { "jump", "move_left" }, Electron2D.InputMap.GetActions());
            Assert.Equal(0.25f, Electron2D.InputMap.ActionGetDeadzone("jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.Space }, "jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventMouseButton { ButtonIndex = Electron2D.MouseButton.Right }, "jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventJoypadButton { ButtonIndex = Electron2D.JoyButton.A }, "jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(
                new Electron2D.InputEventJoypadMotion
                {
                    Axis = Electron2D.JoyAxis.LeftX,
                    AxisValue = 0.8f
                },
                "jump"));
            Assert.True(Electron2D.InputMap.EventIsAction(new Electron2D.InputEventKey { Keycode = Electron2D.Key.A }, "move_left"));
        }
        finally
        {
            ResetInputState();
            Directory.Delete(directory, recursive: true);
        }
    }

    private static Electron2D.InputEventKey KeyEvent(Electron2D.Key key, bool pressed)
    {
        return new Electron2D.InputEventKey
        {
            Keycode = key,
            Pressed = pressed
        };
    }

    private static void ResetInputState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
    }

    private sealed class ActionStateRecorder : Electron2D.Node
    {
        public List<string> States { get; } = [];

        public override void _Input(Electron2D.InputEvent inputEvent)
        {
            States.Add($"{Electron2D.Input.IsActionPressed("jump")}:{Electron2D.Input.IsActionJustPressed("jump")}");
        }
    }
}
