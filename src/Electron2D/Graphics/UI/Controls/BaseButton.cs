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
/// Provides shared input, focus and signal behavior for button controls.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>BaseButton</c> handles pointer, touch, keyboard and gamepad activation.
/// Derived controls provide visual content such as text, check marks or
/// textures.
/// </para>
/// <para>
/// The following signals are available: <c>button_down</c>,
/// <c>button_up</c>, <c>pressed</c> and <c>toggled</c>.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate buttons on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Button"/>
/// <seealso cref="TextureButton"/>
/// <seealso cref="CheckBox"/>
public class BaseButton : Control
{
    private bool buttonPressed;
    private bool disabled;
    private bool pointerPressing;
    private bool touchPressing;
    private bool keyPressing;
    private bool joyPressing;

    /// <summary>
    /// Identifies when a button emits its activation signal.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The action mode applies to pointer, touch, keyboard and gamepad input.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This enum is immutable and is safe to use from any thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This enum is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="BaseButton.ActionMode"/>
    public enum ActionModeEnum
    {
        /// <summary>
        /// Activates the button when the input is pressed.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with <see cref="BaseButton.ActionMode"/>.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ActionModeEnum"/>
        ButtonPress = 0,

        /// <summary>
        /// Activates the button when the input is released.
        /// </summary>
        ///
        /// <remarks>
        /// Use this value with <see cref="BaseButton.ActionMode"/>.
        /// </remarks>
        ///
        /// <since>
        /// This value is available since Electron2D 0.1-preview.
        /// </since>
        ///
        /// <seealso cref="ActionModeEnum"/>
        ButtonRelease = 1
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="BaseButton"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The constructor registers the standard button signals and enables
    /// keyboard/gamepad focus by default.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="BaseButton"/>
    public BaseButton()
    {
        FocusMode = FocusMode.All;
        AddUserSignal("button_down");
        AddUserSignal("button_up");
        AddUserSignal("pressed");
        AddUserSignal("toggled");
    }

    /// <summary>
    /// Gets or sets when this button activates.
    /// </summary>
    ///
    /// <value>
    /// A value that controls whether activation happens on press or release.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="ActionModeEnum.ButtonRelease"/> is the default. It lets users
    /// cancel pointer/touch activation by releasing outside the control.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ActionModeEnum"/>
    public ActionModeEnum ActionMode { get; set; } = ActionModeEnum.ButtonRelease;

    /// <summary>
    /// Gets or sets whether this button is currently pressed as a toggle.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when the button is in the pressed state; otherwise,
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// User activation changes this property only when <see cref="ToggleMode"/>
    /// is enabled. Assigning this property directly updates state without
    /// emitting <c>pressed</c> or <c>toggled</c>.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ToggleMode"/>
    public bool ButtonPressed
    {
        get => buttonPressed;
        set
        {
            ThrowIfFreed();
            buttonPressed = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether this button is disabled.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> when input and focus are disabled; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Disabled buttons ignore pointer, touch, keyboard and gamepad input and
    /// cannot receive focus.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.FocusMode"/>
    public bool Disabled
    {
        get => disabled;
        set
        {
            ThrowIfFreed();
            if (disabled == value)
            {
                return;
            }

            disabled = value;
            ResetPressingState();
            if (disabled)
            {
                ReleaseFocus();
            }

            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether this button toggles its pressed state.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> for toggle behavior; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Toggle buttons emit <c>toggled</c> with the new
    /// <see cref="ButtonPressed"/> value when activated.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ButtonPressed"/>
    public bool ToggleMode { get; set; }

    internal bool IsPressing => pointerPressing || touchPressing || keyPressing || joyPressing;

    internal override bool CanReceiveFocusCore()
    {
        return !Disabled;
    }

    /// <summary>
    /// Handles GUI input routed to this button.
    /// </summary>
    ///
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    ///
    /// <remarks>
    /// <para>
    /// This method consumes supported activation input through
    /// <see cref="Control.AcceptEvent"/> after updating button state.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        if (Disabled)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventMouseButton mouseButton when mouseButton.ButtonIndex == MouseButton.Left:
                HandlePointer(mouseButton.Pressed, IsInside(mouseButton.GlobalPosition));
                AcceptEvent();
                break;
            case InputEventScreenTouch touch:
                HandleTouch(touch.Pressed, !touch.Canceled && IsInside(touch.Position));
                AcceptEvent();
                break;
            case InputEventKey key when IsActivationKey(key.Keycode) && !key.Echo:
                HandleKey(key.Pressed);
                AcceptEvent();
                break;
            case InputEventJoypadButton joypadButton when joypadButton.ButtonIndex == JoyButton.A:
                HandleJoypad(joypadButton.Pressed);
                AcceptEvent();
                break;
        }
    }

    private void HandlePointer(bool pressed, bool inside)
    {
        if (pressed)
        {
            pointerPressing = true;
            EmitSignal("button_down");
            if (ActionMode == ActionModeEnum.ButtonPress)
            {
                Activate();
            }
        }
        else if (pointerPressing)
        {
            pointerPressing = false;
            EmitSignal("button_up");
            if (inside && ActionMode == ActionModeEnum.ButtonRelease)
            {
                Activate();
            }
        }

        QueueRedraw();
    }

    private void HandleTouch(bool pressed, bool inside)
    {
        if (pressed)
        {
            touchPressing = true;
            EmitSignal("button_down");
            if (ActionMode == ActionModeEnum.ButtonPress)
            {
                Activate();
            }
        }
        else if (touchPressing)
        {
            touchPressing = false;
            EmitSignal("button_up");
            if (inside && ActionMode == ActionModeEnum.ButtonRelease)
            {
                Activate();
            }
        }

        QueueRedraw();
    }

    private void HandleKey(bool pressed)
    {
        if (pressed)
        {
            if (keyPressing)
            {
                return;
            }

            keyPressing = true;
            EmitSignal("button_down");
            if (ActionMode == ActionModeEnum.ButtonPress)
            {
                Activate();
            }
        }
        else if (keyPressing)
        {
            keyPressing = false;
            EmitSignal("button_up");
            if (ActionMode == ActionModeEnum.ButtonRelease)
            {
                Activate();
            }
        }

        QueueRedraw();
    }

    private void HandleJoypad(bool pressed)
    {
        if (pressed)
        {
            if (joyPressing)
            {
                return;
            }

            joyPressing = true;
            EmitSignal("button_down");
            if (ActionMode == ActionModeEnum.ButtonPress)
            {
                Activate();
            }
        }
        else if (joyPressing)
        {
            joyPressing = false;
            EmitSignal("button_up");
            if (ActionMode == ActionModeEnum.ButtonRelease)
            {
                Activate();
            }
        }

        QueueRedraw();
    }

    private void Activate()
    {
        if (ToggleMode)
        {
            buttonPressed = !buttonPressed;
            EmitSignal("toggled", buttonPressed);
        }

        EmitSignal("pressed");
        QueueRedraw();
    }

    private void ResetPressingState()
    {
        pointerPressing = false;
        touchPressing = false;
        keyPressing = false;
        joyPressing = false;
    }

    private bool IsInside(Vector2 globalPosition)
    {
        return GetGlobalRect().HasPoint(globalPosition);
    }

    private static bool IsActivationKey(Key key)
    {
        return key is Key.Space or Key.Enter or Key.KpEnter;
    }
}
