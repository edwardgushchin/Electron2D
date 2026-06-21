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
using System.Text;
using SDL3;

namespace Electron2D;

internal static class SdlInputEventMapper
{
    public static IReadOnlyList<InputEvent> Map(SDL.Event sdlEvent)
    {
        return (SDL.EventType)sdlEvent.Type switch
        {
            SDL.EventType.KeyDown or SDL.EventType.KeyUp => [MapKeyboard(sdlEvent.Key)],
            SDL.EventType.MouseButtonDown or SDL.EventType.MouseButtonUp => [MapMouseButton(sdlEvent.Button)],
            SDL.EventType.MouseMotion => [MapMouseMotion(sdlEvent.Motion)],
            SDL.EventType.MouseWheel => MapMouseWheel(sdlEvent.Wheel),
            SDL.EventType.TextInput => MapTextInput(sdlEvent.Text),
            _ => Array.Empty<InputEvent>()
        };
    }

    private static InputEventKey MapKeyboard(SDL.KeyboardEvent keyEvent)
    {
        var keycode = MapKeycode(keyEvent.Key);
        return new InputEventKey
        {
            WindowId = checked((int)keyEvent.WindowID),
            Device = checked((int)keyEvent.Which),
            Pressed = keyEvent.Down,
            Echo = keyEvent.Repeat,
            Keycode = keycode,
            PhysicalKeycode = MapScancode(keyEvent.Scancode),
            KeyLabel = keycode,
            Location = MapKeyLocation(keyEvent.Scancode),
            Unicode = 0,
            ShiftPressed = HasModifier(keyEvent.Mod, SDL.Keymod.Shift),
            AltPressed = HasModifier(keyEvent.Mod, SDL.Keymod.Alt),
            CtrlPressed = HasModifier(keyEvent.Mod, SDL.Keymod.Ctrl),
            MetaPressed = HasModifier(keyEvent.Mod, SDL.Keymod.GUI)
        };
    }

    private static InputEventMouseButton MapMouseButton(SDL.MouseButtonEvent buttonEvent)
    {
        var position = new Vector2(buttonEvent.X, buttonEvent.Y);
        return new InputEventMouseButton
        {
            WindowId = checked((int)buttonEvent.WindowID),
            Device = checked((int)buttonEvent.Which),
            ButtonIndex = MapMouseButton(buttonEvent.Button),
            Pressed = buttonEvent.Down,
            DoubleClick = buttonEvent.Clicks >= 2,
            Factor = 1f,
            Position = position,
            GlobalPosition = position
        };
    }

    private static InputEventMouseMotion MapMouseMotion(SDL.MouseMotionEvent motionEvent)
    {
        var position = new Vector2(motionEvent.X, motionEvent.Y);
        var relative = new Vector2(motionEvent.XRel, motionEvent.YRel);
        return new InputEventMouseMotion
        {
            WindowId = checked((int)motionEvent.WindowID),
            Device = checked((int)motionEvent.Which),
            ButtonMask = MapMouseButtonMask(motionEvent.State),
            Position = position,
            GlobalPosition = position,
            Relative = relative,
            ScreenRelative = relative
        };
    }

    private static IReadOnlyList<InputEvent> MapMouseWheel(SDL.MouseWheelEvent wheelEvent)
    {
        var x = wheelEvent.X;
        var y = wheelEvent.Y;
        if (wheelEvent.Direction == SDL.MouseWheelDirection.Flipped)
        {
            x = -x;
            y = -y;
        }

        var position = new Vector2(wheelEvent.MouseX, wheelEvent.MouseY);
        var events = new List<InputEvent>(2);
        if (y != 0f)
        {
            events.Add(CreateWheelButton(
                wheelEvent,
                position,
                y > 0f ? MouseButton.WheelUp : MouseButton.WheelDown,
                MathF.Abs(y)));
        }

        if (x != 0f)
        {
            events.Add(CreateWheelButton(
                wheelEvent,
                position,
                x > 0f ? MouseButton.WheelRight : MouseButton.WheelLeft,
                MathF.Abs(x)));
        }

        return events;
    }

    private static InputEventMouseButton CreateWheelButton(
        SDL.MouseWheelEvent wheelEvent,
        Vector2 position,
        MouseButton button,
        float factor)
    {
        return new InputEventMouseButton
        {
            WindowId = checked((int)wheelEvent.WindowID),
            Device = checked((int)wheelEvent.Which),
            ButtonIndex = button,
            Pressed = true,
            Factor = factor,
            Position = position,
            GlobalPosition = position
        };
    }

    private static IReadOnlyList<InputEvent> MapTextInput(SDL.TextInputEvent textEvent)
    {
        if (textEvent.Text == IntPtr.Zero)
        {
            return Array.Empty<InputEvent>();
        }

        var text = Marshal.PtrToStringUTF8(textEvent.Text);
        if (string.IsNullOrEmpty(text))
        {
            return Array.Empty<InputEvent>();
        }

        var events = new List<InputEvent>();
        foreach (var rune in text.EnumerateRunes())
        {
            events.Add(new InputEventKey
            {
                WindowId = checked((int)textEvent.WindowID),
                Pressed = true,
                Keycode = Key.None,
                PhysicalKeycode = Key.None,
                KeyLabel = Key.None,
                Unicode = rune.Value
            });
        }

        return events;
    }

    private static Key MapKeycode(SDL.Keycode keycode)
    {
        var value = checked((int)keycode);
        if (value >= 'a' && value <= 'z')
        {
            return (Key)(value - ('a' - 'A'));
        }

        if (value >= 32 && value <= 126)
        {
            return (Key)value;
        }

        return keycode switch
        {
            SDL.Keycode.Backspace => Key.Backspace,
            SDL.Keycode.Tab => Key.Tab,
            SDL.Keycode.Return => Key.Enter,
            SDL.Keycode.Escape => Key.Escape,
            SDL.Keycode.Delete => Key.Delete,
            SDL.Keycode.Capslock => Key.Capslock,
            SDL.Keycode.F1 => Key.F1,
            SDL.Keycode.F2 => Key.F2,
            SDL.Keycode.F3 => Key.F3,
            SDL.Keycode.F4 => Key.F4,
            SDL.Keycode.F5 => Key.F5,
            SDL.Keycode.F6 => Key.F6,
            SDL.Keycode.F7 => Key.F7,
            SDL.Keycode.F8 => Key.F8,
            SDL.Keycode.F9 => Key.F9,
            SDL.Keycode.F10 => Key.F10,
            SDL.Keycode.F11 => Key.F11,
            SDL.Keycode.F12 => Key.F12,
            SDL.Keycode.PrintScreen => Key.Print,
            SDL.Keycode.Pause => Key.Pause,
            SDL.Keycode.Insert => Key.Insert,
            SDL.Keycode.Home => Key.Home,
            SDL.Keycode.Pageup => Key.Pageup,
            SDL.Keycode.End => Key.End,
            SDL.Keycode.Pagedown => Key.Pagedown,
            SDL.Keycode.Right => Key.Right,
            SDL.Keycode.Left => Key.Left,
            SDL.Keycode.Down => Key.Down,
            SDL.Keycode.Up => Key.Up,
            SDL.Keycode.NumLockClear => Key.Numlock,
            SDL.Keycode.ScrollLock => Key.Scrolllock,
            SDL.Keycode.KpEnter => Key.KpEnter,
            SDL.Keycode.LCtrl or SDL.Keycode.RCtrl => Key.Ctrl,
            SDL.Keycode.LShift or SDL.Keycode.RShift => Key.Shift,
            SDL.Keycode.LAlt or SDL.Keycode.RAlt => Key.Alt,
            SDL.Keycode.LGUI or SDL.Keycode.RGUI => Key.Meta,
            _ => Key.Unknown
        };
    }

    private static Key MapScancode(SDL.Scancode scancode)
    {
        if (scancode >= SDL.Scancode.A && scancode <= SDL.Scancode.Z)
        {
            return (Key)((int)Key.A + ((int)scancode - (int)SDL.Scancode.A));
        }

        if (scancode >= SDL.Scancode.Alpha1 && scancode <= SDL.Scancode.Alpha9)
        {
            return (Key)((int)Key.Key1 + ((int)scancode - (int)SDL.Scancode.Alpha1));
        }

        return scancode switch
        {
            SDL.Scancode.Alpha0 => Key.Key0,
            SDL.Scancode.Return => Key.Enter,
            SDL.Scancode.Escape => Key.Escape,
            SDL.Scancode.Backspace => Key.Backspace,
            SDL.Scancode.Tab => Key.Tab,
            SDL.Scancode.Space => Key.Space,
            SDL.Scancode.Capslock => Key.Capslock,
            SDL.Scancode.F1 => Key.F1,
            SDL.Scancode.F2 => Key.F2,
            SDL.Scancode.F3 => Key.F3,
            SDL.Scancode.F4 => Key.F4,
            SDL.Scancode.F5 => Key.F5,
            SDL.Scancode.F6 => Key.F6,
            SDL.Scancode.F7 => Key.F7,
            SDL.Scancode.F8 => Key.F8,
            SDL.Scancode.F9 => Key.F9,
            SDL.Scancode.F10 => Key.F10,
            SDL.Scancode.F11 => Key.F11,
            SDL.Scancode.F12 => Key.F12,
            SDL.Scancode.Printscreen => Key.Print,
            SDL.Scancode.Scrolllock => Key.Scrolllock,
            SDL.Scancode.Pause => Key.Pause,
            SDL.Scancode.Insert => Key.Insert,
            SDL.Scancode.Home => Key.Home,
            SDL.Scancode.Pageup => Key.Pageup,
            SDL.Scancode.Delete => Key.Delete,
            SDL.Scancode.End => Key.End,
            SDL.Scancode.Pagedown => Key.Pagedown,
            SDL.Scancode.Right => Key.Right,
            SDL.Scancode.Left => Key.Left,
            SDL.Scancode.Down => Key.Down,
            SDL.Scancode.Up => Key.Up,
            SDL.Scancode.NumLockClear => Key.Numlock,
            SDL.Scancode.KpEnter => Key.KpEnter,
            SDL.Scancode.LCtrl or SDL.Scancode.RCtrl => Key.Ctrl,
            SDL.Scancode.LShift or SDL.Scancode.RShift => Key.Shift,
            SDL.Scancode.LAlt or SDL.Scancode.RAlt => Key.Alt,
            SDL.Scancode.LGUI or SDL.Scancode.RGUI => Key.Meta,
            _ => Key.Unknown
        };
    }

    private static KeyLocation MapKeyLocation(SDL.Scancode scancode)
    {
        return scancode switch
        {
            SDL.Scancode.LCtrl or SDL.Scancode.LShift or SDL.Scancode.LAlt or SDL.Scancode.LGUI => KeyLocation.Left,
            SDL.Scancode.RCtrl or SDL.Scancode.RShift or SDL.Scancode.RAlt or SDL.Scancode.RGUI => KeyLocation.Right,
            _ => KeyLocation.Unspecified
        };
    }

    private static MouseButton MapMouseButton(byte button)
    {
        return button switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            4 => MouseButton.Xbutton1,
            5 => MouseButton.Xbutton2,
            _ => MouseButton.None
        };
    }

    private static MouseButtonMask MapMouseButtonMask(SDL.MouseButtonFlags flags)
    {
        var result = MouseButtonMask.None;
        if (flags.HasFlag(SDL.MouseButtonFlags.Left))
        {
            result |= MouseButtonMask.Left;
        }

        if (flags.HasFlag(SDL.MouseButtonFlags.Right))
        {
            result |= MouseButtonMask.Right;
        }

        if (flags.HasFlag(SDL.MouseButtonFlags.Middle))
        {
            result |= MouseButtonMask.Middle;
        }

        if (flags.HasFlag(SDL.MouseButtonFlags.X1))
        {
            result |= MouseButtonMask.Xbutton1;
        }

        if (flags.HasFlag(SDL.MouseButtonFlags.X2))
        {
            result |= MouseButtonMask.Xbutton2;
        }

        return result;
    }

    private static bool HasModifier(SDL.Keymod modifiers, SDL.Keymod modifier)
    {
        return (modifiers & modifier) != 0;
    }
}
