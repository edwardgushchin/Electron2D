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
namespace Electron2D;

/// <summary>
/// Base class for Godot-like input events delivered to <see cref="Node._Input(InputEvent)"/>.
/// </summary>
/// <remarks>
/// <para>
/// This class is an event resource and does not represent a concrete device
/// action by itself. Concrete keyboard and mouse events derive from it.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
/// <seealso cref="InputEventKey"/>
/// <seealso cref="InputEventMouseButton"/>
/// <seealso cref="InputEventMouseMotion"/>
public class InputEvent : Resource
{
    /// <summary>
    /// Gets or sets the device index that produced this event.
    /// </summary>
    /// <remarks>
    /// SDL device identifiers are copied into this value by the internal input
    /// mapper. The value is informational in 0.1.0 Preview and is not yet tied
    /// to `InputMap` action state.
    /// </remarks>
    public int Device { get; set; }
}

/// <summary>
/// Base class for input events that originate from a platform window.
/// </summary>
/// <remarks>
/// <para>
/// This mirrors Godot's `InputEventFromWindow` layer and stores the window
/// identifier reported by the platform event source.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventFromWindow : InputEvent
{
    /// <summary>
    /// Gets or sets the platform window identifier associated with this event.
    /// </summary>
    public int WindowId { get; set; }
}

/// <summary>
/// Base class for input events that carry keyboard modifier state.
/// </summary>
/// <remarks>
/// <para>
/// The SDL mapper fills these booleans from the SDL key modifier mask for
/// keyboard events. Mouse events currently keep the default values until SDL
/// modifier state is plumbed through the event pump.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventWithModifiers : InputEventFromWindow
{
    /// <summary>
    /// Gets or sets whether Shift was pressed when the event was generated.
    /// </summary>
    public bool ShiftPressed { get; set; }

    /// <summary>
    /// Gets or sets whether Alt or Option was pressed when the event was generated.
    /// </summary>
    public bool AltPressed { get; set; }

    /// <summary>
    /// Gets or sets whether Control was pressed when the event was generated.
    /// </summary>
    public bool CtrlPressed { get; set; }

    /// <summary>
    /// Gets or sets whether Meta, Command, or Windows was pressed when the event was generated.
    /// </summary>
    public bool MetaPressed { get; set; }
}

/// <summary>
/// Represents a keyboard key press, release, echo, or text input scalar value.
/// </summary>
/// <remarks>
/// <para>
/// SDL key down/up events fill <see cref="Keycode"/>, <see cref="PhysicalKeycode"/>,
/// <see cref="KeyLabel"/>, <see cref="Pressed"/> and <see cref="Echo"/>.
/// SDL text input events are represented as one or more key events with
/// <see cref="Unicode"/> set and the keycode fields left as <see cref="Key.None"/>.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventKey : InputEventWithModifiers
{
    /// <summary>
    /// Gets or sets whether the key is pressed.
    /// </summary>
    public bool Pressed { get; set; }

    /// <summary>
    /// Gets or sets whether this event is an echo repeat while the key is held.
    /// </summary>
    public bool Echo { get; set; }

    /// <summary>
    /// Gets or sets the layout key code.
    /// </summary>
    public Key Keycode { get; set; }

    /// <summary>
    /// Gets or sets the physical key code on a US QWERTY keyboard layout.
    /// </summary>
    public Key PhysicalKeycode { get; set; }

    /// <summary>
    /// Gets or sets the localized key label.
    /// </summary>
    public Key KeyLabel { get; set; }

    /// <summary>
    /// Gets or sets the left/right key location for modifier keys.
    /// </summary>
    public KeyLocation Location { get; set; }

    /// <summary>
    /// Gets or sets the Unicode scalar value produced by SDL text input.
    /// </summary>
    public int Unicode { get; set; }
}

/// <summary>
/// Base class for mouse input events.
/// </summary>
/// <remarks>
/// <para>
/// Mouse events carry viewport-local and global positions. In the 0.1.0 Preview
/// baseline these positions are identical because viewport scaling and GUI local
/// coordinates are not yet part of the input pipeline.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventMouse : InputEventWithModifiers
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InputEventMouse"/> class.
    /// </summary>
    public InputEventMouse()
    {
        Device = 32;
    }

    /// <summary>
    /// Gets or sets the button mask active when the mouse event was generated.
    /// </summary>
    public MouseButtonMask ButtonMask { get; set; }

    /// <summary>
    /// Gets or sets the event position in the current viewport.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the event position in the root viewport.
    /// </summary>
    public Vector2 GlobalPosition { get; set; }
}

/// <summary>
/// Represents a mouse button press, release, or wheel step.
/// </summary>
/// <remarks>
/// <para>
/// Wheel events use <see cref="ButtonIndex"/> values such as
/// <see cref="MouseButton.WheelUp"/> and <see cref="MouseButton.WheelDown"/>,
/// matching Godot's mouse wheel model.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventMouseButton : InputEventMouse
{
    /// <summary>
    /// Gets or sets the mouse button or wheel constant.
    /// </summary>
    public MouseButton ButtonIndex { get; set; }

    /// <summary>
    /// Gets or sets whether the button is pressed.
    /// </summary>
    public bool Pressed { get; set; }

    /// <summary>
    /// Gets or sets whether the platform canceled this mouse button event.
    /// </summary>
    public bool Canceled { get; set; }

    /// <summary>
    /// Gets or sets whether this event represents a double-click.
    /// </summary>
    public bool DoubleClick { get; set; }

    /// <summary>
    /// Gets or sets the wheel delta or high-precision scroll amount.
    /// </summary>
    public float Factor { get; set; } = 1f;
}

/// <summary>
/// Represents a mouse movement event.
/// </summary>
/// <remarks>
/// <para>
/// SDL relative motion fills <see cref="Relative"/> and <see cref="ScreenRelative"/>.
/// Velocity values remain zero until frame timing is connected to the input pump.
/// </para>
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
/// </remarks>
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
public class InputEventMouseMotion : InputEventMouse
{
    /// <summary>
    /// Gets or sets the mouse position relative to the previous mouse position.
    /// </summary>
    public Vector2 Relative { get; set; }

    /// <summary>
    /// Gets or sets the unscaled mouse position relative to the previous mouse position.
    /// </summary>
    public Vector2 ScreenRelative { get; set; }

    /// <summary>
    /// Gets or sets the mouse velocity in pixels per second.
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Gets or sets the unscaled mouse velocity in pixels per second.
    /// </summary>
    public Vector2 ScreenVelocity { get; set; }

    /// <summary>
    /// Gets or sets the pen tilt.
    /// </summary>
    public Vector2 Tilt { get; set; }

    /// <summary>
    /// Gets or sets the pen pressure from 0.0 to 1.0.
    /// </summary>
    public float Pressure { get; set; }

    /// <summary>
    /// Gets or sets whether the eraser end of a stylus pen is active.
    /// </summary>
    public bool PenInverted { get; set; }
}
