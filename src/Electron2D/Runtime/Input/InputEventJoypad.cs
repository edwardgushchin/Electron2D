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
/// Represents a gamepad button press or release.
/// </summary>
///
/// <remarks>
/// <para>
/// The event is delivered through <see cref="Node._Input(InputEvent)"/> and is
/// also consumed by <see cref="Input"/> to update button state and by
/// <see cref="InputMap"/> to update action bindings.
/// </para>
/// <para>
/// The device id is stored in <see cref="InputEvent.Device"/>. Unknown devices
/// are registered as connected placeholder devices when a button event is
/// processed.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="JoyButton"/>
/// <seealso cref="Input.IsJoyButtonPressed(int, JoyButton)"/>
/// <seealso cref="InputMap"/>
public class InputEventJoypadButton : InputEvent
{

    /// <summary>
    /// Initializes a new instance of the InputEventJoypadButton type.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="InputEventJoypadButton" />
    ///
    public InputEventJoypadButton()
    {
    }

    private float pressure;

    /// <summary>
    /// Gets or sets the gamepad button represented by this event.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="JoyButton"/> value, or <see cref="JoyButton.Invalid"/> for
    /// an unmapped button.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Invalid button values do not match <see cref="InputMap"/> action
    /// bindings and are ignored by <see cref="Input.IsJoyButtonPressed"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="InputEventJoypadButton" />
    ///
    public JoyButton ButtonIndex { get; set; } = JoyButton.Invalid;

    /// <summary>
    /// Gets or sets whether the button is pressed.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Pressed events update <see cref="Input.IsJoyButtonPressed(int, JoyButton)"/>
    /// and action state. Released events clear the corresponding button state.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <value>
    /// The current pressed value.
    /// </value>
    ///
    /// <seealso cref="InputEventJoypadButton" />
    ///
    public bool Pressed { get; set; }

    /// <summary>
    /// Gets or sets the analog pressure for this button event.
    /// </summary>
    ///
    /// <value>
    /// A value clamped to the range <c>0.0</c> through <c>1.0</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Digital platform button events set this to <c>1.0</c> while pressed and
    /// <c>0.0</c> while released. Action bindings use this value when it is
    /// greater than zero.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="InputEventJoypadButton" />
    ///
    public float Pressure
    {
        get => pressure;
        set => pressure = float.IsFinite(value) ? Mathf.Clamp(value, 0f, 1f) : 0f;
    }
}

/// <summary>
/// Represents a gamepad axis motion event.
/// </summary>
///
/// <remarks>
/// <para>
/// Axis motion is delivered through <see cref="Node._Input(InputEvent)"/> and
/// updates both <see cref="Input.GetJoyAxis(int, JoyAxis)"/> and
/// <see cref="InputMap"/> action state.
/// </para>
/// <para>
/// The device id is stored in <see cref="InputEvent.Device"/>. Unknown devices
/// are registered as connected placeholder devices when an axis event is
/// processed.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// Instances are mutable and are not synchronized; use them from the input
/// dispatch thread that owns the event.
/// </threadsafety>
///
/// <since>
/// This class is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="JoyAxis"/>
/// <seealso cref="Input.GetJoyAxis(int, JoyAxis)"/>
/// <seealso cref="InputMap"/>
public class InputEventJoypadMotion : InputEvent
{

    /// <summary>
    /// Initializes a new instance of the InputEventJoypadMotion type.
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
    /// This API is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="InputEventJoypadMotion" />
    ///
    public InputEventJoypadMotion()
    {
    }

    private float axisValue;

    /// <summary>
    /// Gets or sets the gamepad axis represented by this event.
    /// </summary>
    ///
    /// <value>
    /// A <see cref="JoyAxis"/> value, or <see cref="JoyAxis.Invalid"/> for an
    /// unmapped axis.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Invalid axes do not match action bindings and are ignored by
    /// <see cref="Input.GetJoyAxis(int, JoyAxis)"/>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="InputEventJoypadMotion" />
    ///
    public JoyAxis Axis { get; set; } = JoyAxis.Invalid;

    /// <summary>
    /// Gets or sets the normalized axis value.
    /// </summary>
    ///
    /// <value>
    /// A finite value clamped to the range <c>-1.0</c> through <c>1.0</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Action bindings compare this value with the sign stored in the binding
    /// event. The absolute value becomes action strength when it is outside the
    /// action deadzone.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <seealso cref="InputEventJoypadMotion" />
    ///
    public float AxisValue
    {
        get => axisValue;
        set => axisValue = float.IsFinite(value) ? Mathf.Clamp(value, -1f, 1f) : 0f;
    }
}
