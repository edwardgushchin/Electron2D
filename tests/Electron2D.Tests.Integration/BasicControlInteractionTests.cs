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
public sealed class BasicControlInteractionTests
{
    [Fact]
    public void ButtonSignalsFollowPointerKeyboardAndDisabledState()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var button = new Electron2D.Button
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(80f, 24f)
        };
        var events = new List<string>();
        button.Connect("button_down", Electron2D.Callable.From(() => events.Add("down")));
        button.Connect("button_up", Electron2D.Callable.From(() => events.Add("up")));
        button.Connect("pressed", Electron2D.Callable.From(() => events.Add("pressed")));
        tree.Root.AddChild(button);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(10f, 10f), true));
        tree.DispatchInput(MouseButton(new Electron2D.Vector2(10f, 10f), false));

        Assert.True(button.HasFocus());
        Assert.Equal(new[] { "down", "up", "pressed" }, events);

        events.Clear();
        button.ActionMode = Electron2D.BaseButton.ActionModeEnum.ButtonPress;
        tree.DispatchInput(Key(Electron2D.Key.Space, true));
        tree.DispatchInput(Key(Electron2D.Key.Space, false));

        Assert.Equal(new[] { "down", "pressed", "up" }, events);

        events.Clear();
        button.Disabled = true;
        button.ReleaseFocus();
        tree.DispatchInput(MouseButton(new Electron2D.Vector2(10f, 10f), true));
        tree.DispatchInput(MouseButton(new Electron2D.Vector2(10f, 10f), false));

        Assert.False(button.HasFocus());
        Assert.Empty(events);
    }

    [Fact]
    public void CheckBoxTogglesFromTouchAndGamepad()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var checkBox = new Electron2D.CheckBox
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(80f, 24f)
        };
        var toggles = new List<bool>();
        checkBox.Connect("toggled", Electron2D.Callable.From<bool>(toggles.Add));
        tree.Root.AddChild(checkBox);

        tree.DispatchInput(Touch(new Electron2D.Vector2(12f, 12f), true));
        tree.DispatchInput(Touch(new Electron2D.Vector2(12f, 12f), false));

        Assert.True(checkBox.ButtonPressed);
        Assert.Equal(new[] { true }, toggles);

        checkBox.GrabFocus();
        tree.DispatchInput(JoyButton(Electron2D.JoyButton.A, true));
        tree.DispatchInput(JoyButton(Electron2D.JoyButton.A, false));

        Assert.False(checkBox.ButtonPressed);
        Assert.Equal(new[] { true, false }, toggles);
    }

    [Fact]
    public void LineEditEditsTextRejectsOverflowAndSubmits()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var lineEdit = new Electron2D.LineEdit
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(120f, 24f),
            MaxLength = 3
        };
        var changes = new List<string>();
        var rejected = new List<string>();
        var submitted = new List<string>();
        lineEdit.Connect("text_changed", Electron2D.Callable.From<string>(changes.Add));
        lineEdit.Connect("text_change_rejected", Electron2D.Callable.From<string>(rejected.Add));
        lineEdit.Connect("text_submitted", Electron2D.Callable.From<string>(submitted.Add));
        tree.Root.AddChild(lineEdit);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(4f, 4f), true));
        tree.DispatchInput(Text("a"));
        tree.DispatchInput(Text("b"));
        tree.DispatchInput(Text("c"));
        tree.DispatchInput(Text("d"));
        tree.DispatchInput(Key(Electron2D.Key.Backspace, true));
        tree.DispatchInput(Key(Electron2D.Key.Enter, true));

        Assert.Equal("ab", lineEdit.Text);
        Assert.Equal(2, lineEdit.CaretColumn);
        Assert.Equal(new[] { "a", "ab", "abc", "ab" }, changes);
        Assert.Equal(new[] { "d" }, rejected);
        Assert.Equal(new[] { "ab" }, submitted);

        lineEdit.Editable = false;
        tree.DispatchInput(Text("z"));

        Assert.Equal("ab", lineEdit.Text);
    }

    [Fact]
    public void SliderChangesRangeFromPointerKeyboardAndGamepad()
    {
        ResetInputState();

        var tree = new Electron2D.SceneTree();
        var slider = new Electron2D.Slider
        {
            Position = new Electron2D.Vector2(0f, 0f),
            Size = new Electron2D.Vector2(100f, 20f),
            MinValue = 0d,
            MaxValue = 10d,
            Step = 1d
        };
        var values = new List<double>();
        slider.Connect("value_changed", Electron2D.Callable.From<double>(values.Add));
        tree.Root.AddChild(slider);

        tree.DispatchInput(MouseButton(new Electron2D.Vector2(75f, 10f), true));
        tree.DispatchInput(Key(Electron2D.Key.Left, true));
        tree.DispatchInput(JoyButton(Electron2D.JoyButton.DpadRight, true));

        Assert.Equal(8d, slider.Value);
        Assert.Equal(new[] { 8d, 7d, 8d }, values);

        slider.Editable = false;
        tree.DispatchInput(Touch(new Electron2D.Vector2(10f, 10f), true));

        Assert.Equal(8d, slider.Value);
    }

    [Fact]
    public void TextureRectAndNinePatchSubmitTextureCommands()
    {
        var texture = new Electron2D.RuntimeTexture2D(10, 4, hasAlpha: true);
        var tree = new Electron2D.SceneTree();
        var textureRect = new Electron2D.TextureRect
        {
            Position = new Electron2D.Vector2(2f, 3f),
            Size = new Electron2D.Vector2(30f, 20f),
            Texture = texture,
            StretchMode = Electron2D.TextureRect.StretchModeEnum.KeepCentered
        };
        var ninePatch = new Electron2D.NinePatchRect
        {
            Position = new Electron2D.Vector2(40f, 5f),
            Size = new Electron2D.Vector2(12f, 8f),
            Texture = texture,
            PatchMarginLeft = 2,
            PatchMarginTop = 1,
            PatchMarginRight = 2,
            PatchMarginBottom = 1
        };
        tree.Root.AddChild(textureRect);
        tree.Root.AddChild(ninePatch);

        tree.ProcessFrame(1.0 / 60.0);

        var commands = new Electron2D.CanvasSubmissionContext().BuildPlan(tree.Root).Commands
            .Where(command => command.Kind == Electron2D.CanvasItemRenderCommandKind.Texture)
            .ToArray();

        Assert.Equal(10, commands.Length);
        Assert.Equal(new Electron2D.Rect2(10f, 8f, 10f, 4f), commands[0].DestinationRect);
        Assert.Equal(new Electron2D.Vector2(2f, 3f), commands[0].Transform.Origin);
        var ninePatchCommands = commands.Skip(1).ToArray();
        Assert.All(ninePatchCommands, command => Assert.Equal(new Electron2D.Vector2(40f, 5f), command.Transform.Origin));
        Assert.Contains(ninePatchCommands, command =>
            command.SourceRect == new Electron2D.Rect2(2f, 1f, 6f, 2f) &&
            command.DestinationRect == new Electron2D.Rect2(2f, 1f, 8f, 6f));
    }

    private static Electron2D.InputEventMouseButton MouseButton(Electron2D.Vector2 position, bool pressed)
    {
        return new Electron2D.InputEventMouseButton
        {
            ButtonIndex = Electron2D.MouseButton.Left,
            Pressed = pressed,
            Position = position,
            GlobalPosition = position
        };
    }

    private static Electron2D.InputEventScreenTouch Touch(Electron2D.Vector2 position, bool pressed)
    {
        return new Electron2D.InputEventScreenTouch
        {
            Position = position,
            Pressed = pressed
        };
    }

    private static Electron2D.InputEventKey Text(string text)
    {
        return new Electron2D.InputEventKey
        {
            Pressed = true,
            Unicode = char.ConvertToUtf32(text, 0)
        };
    }

    private static Electron2D.InputEventKey Key(Electron2D.Key key, bool pressed)
    {
        return new Electron2D.InputEventKey
        {
            Keycode = key,
            Pressed = pressed
        };
    }

    private static Electron2D.InputEventJoypadButton JoyButton(Electron2D.JoyButton button, bool pressed)
    {
        return new Electron2D.InputEventJoypadButton
        {
            ButtonIndex = button,
            Pressed = pressed,
            Pressure = pressed ? 1f : 0f
        };
    }

    private static void ResetInputState()
    {
        Electron2D.Input.ResetForTests();
        Electron2D.InputMap.ClearForTests();
    }
}
