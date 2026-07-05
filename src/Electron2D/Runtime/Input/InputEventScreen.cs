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
/// Represents a screen touch press, release or cancel event.
/// </summary>
///
/// <remarks>
/// <para>
/// This event stores one pointer in a multitouch sequence. The
/// <see cref="Index"/> value identifies the finger within the active touch
/// device, and <see cref="Position"/> stores the current viewport position.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="InputEventScreenDrag"/>
public class InputEventScreenTouch : InputEventFromWindow
{

    /// <summary>
    /// Initializes a new instance of the InputEventScreenTouch type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public InputEventScreenTouch()
    {
    }

    /// <summary>
    /// Gets or sets the touch index.
    /// </summary>
    ///
    /// <remarks>
    /// One index represents one finger within the active multitouch sequence.
    /// Platform finger ids that do not fit into <see cref="int"/> are ignored by
    /// the input mapper.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current index value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the touch position.
    /// </summary>
    ///
    /// <remarks>
    /// The 0.1-preview input mapper stores the platform-provided touch
    /// coordinates directly. Future viewport scaling work can transform this
    /// value before dispatch.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets whether this event presses the touch.
    /// </summary>
    ///
    /// <remarks>
    /// A value of <c>false</c> represents release or cancel, depending on
    /// <see cref="Canceled"/>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current pressed value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public bool Pressed { get; set; }

    /// <summary>
    /// Gets or sets whether this touch represents a double tap.
    /// </summary>
    ///
    /// <remarks>
    /// The 0.1-preview platform mapper leaves this value <c>false</c> until a
    /// platform backend reports double-tap semantics.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current double tap value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public bool DoubleTap { get; set; }

    /// <summary>
    /// Gets or sets whether the platform canceled this touch.
    /// </summary>
    ///
    /// <remarks>
    /// Canceled events are delivered as released touches with
    /// <see cref="Pressed"/> set to <c>false</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current canceled value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenTouch" />
    ///
    public bool Canceled { get; set; }
}

/// <summary>
/// Represents a screen touch drag event.
/// </summary>
///
/// <remarks>
/// <para>
/// Drag events carry the current pointer position, motion delta and pressure.
/// Velocity values remain zero until frame timing is connected to touch input
/// dispatch.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="InputEventScreenTouch"/>
public class InputEventScreenDrag : InputEventFromWindow
{

    /// <summary>
    /// Initializes a new instance of the InputEventScreenDrag type.
    /// </summary>
    ///
    /// <remarks>
    /// The new instance follows the lifetime and validation rules of its declaring type.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This member is not synchronized. Call it from the thread that owns the related object unless the declaring type states otherwise.
    /// </threadsafety>
    ///
    /// <since>
    /// This API is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public InputEventScreenDrag()
    {
    }

    private float pressure;

    /// <summary>
    /// Gets or sets the touch index.
    /// </summary>
    ///
    /// <remarks>
    /// One index represents one finger within the active multitouch sequence.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current index value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public int Index { get; set; }

    /// <summary>
    /// Gets or sets the current drag position.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the drag delta in viewport coordinates.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current relative value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 Relative { get; set; }

    /// <summary>
    /// Gets or sets the unscaled drag delta in screen coordinates.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current screen relative value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 ScreenRelative { get; set; }

    /// <summary>
    /// Gets or sets the drag velocity in viewport coordinates.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current velocity value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Gets or sets the unscaled drag velocity in screen coordinates.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current screen velocity value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 ScreenVelocity { get; set; }

    /// <summary>
    /// Gets or sets the pen pressure.
    /// </summary>
    ///
    /// <remarks>
    /// Values are clamped to the range <c>0.0</c> through <c>1.0</c>. Non-finite
    /// values are treated as <c>0.0</c>.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <value>
    /// The current pressure value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public float Pressure
    {
        get => pressure;
        set => pressure = float.IsFinite(value) ? Mathf.Clamp(value, 0f, 1f) : 0f;
    }

    /// <summary>
    /// Gets or sets the pen tilt.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current tilt value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public Vector2 Tilt { get; set; }

    /// <summary>
    /// Gets or sets whether the eraser end of a stylus pen is active.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current pen inverted value.
    /// </value>
    ///
    /// <seealso cref="InputEventScreenDrag" />
    ///
    public bool PenInverted { get; set; }
}
