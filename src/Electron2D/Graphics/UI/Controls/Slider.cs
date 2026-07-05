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
/// Provides an editable horizontal slider control.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Slider</c> changes <see cref="Range.Value"/> from pointer, touch,
/// keyboard and gamepad input.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate sliders on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Range"/>
public class Slider : Range
{
    /// <summary>
    /// Initializes a new instance of the <see cref="Slider"/> class.
    /// </summary>
    /// <remarks>The new slider can receive keyboard/gamepad focus and edit values.</remarks>
    /// <threadsafety>This constructor is not synchronized. Call it from the main scene thread.</threadsafety>
    /// <since>This constructor is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Slider"/>
    public Slider()
    {
        FocusMode = FocusMode.All;
    }

    /// <summary>
    /// Gets or sets whether user input can change the slider.
    /// </summary>
    /// <value><c>true</c> when input can edit <see cref="Range.Value"/>; otherwise, <c>false</c>.</value>
    /// <remarks>Programmatic assignments to <see cref="Range.Value"/> remain allowed.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Range.Value"/>
    public bool Editable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether wheel-like scrolling is allowed.
    /// </summary>
    /// <value><c>true</c> when scroll input is allowed; otherwise, <c>false</c>.</value>
    /// <remarks>The preview stores this property for API consumers; wheel handling is not part of T-0069.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Editable"/>
    public bool Scrollable { get; set; } = true;

    /// <summary>
    /// Gets or sets the number of visual ticks.
    /// </summary>
    /// <value>A non-negative tick count.</value>
    /// <remarks>Tick rendering is not drawn in the preview visual style.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="TicksOnBorders"/>
    public int Ticks
    {
        get => ticks;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            ticks = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether ticks are drawn on borders.
    /// </summary>
    /// <value><c>true</c> to include border ticks; otherwise, <c>false</c>.</value>
    /// <remarks>The preview stores this property for API consumers.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Ticks"/>
    public bool TicksOnBorders { get; set; }

    private int ticks;

    /// <summary>
    /// Handles GUI input routed to this slider.
    /// </summary>
    /// <param name="inputEvent">The input event delivered by the viewport.</param>
    /// <remarks>Input is ignored when <see cref="Editable"/> is <c>false</c>.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Control._GuiInput(InputEvent)"/>
    public override void _GuiInput(InputEvent inputEvent)
    {
        if (!Editable)
        {
            return;
        }

        switch (inputEvent)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } mouseButton:
                SetValueFromPosition(mouseButton.GlobalPosition);
                AcceptEvent();
                break;
            case InputEventMouseMotion mouseMotion when (mouseMotion.ButtonMask & MouseButtonMask.Left) != 0:
                SetValueFromPosition(mouseMotion.GlobalPosition);
                AcceptEvent();
                break;
            case InputEventScreenTouch { Pressed: true } touch:
                SetValueFromPosition(touch.Position);
                AcceptEvent();
                break;
            case InputEventScreenDrag drag:
                SetValueFromPosition(drag.Position);
                AcceptEvent();
                break;
            case InputEventKey key when key.Pressed:
                HandleKey(key.Keycode);
                break;
            case InputEventJoypadButton joypadButton when joypadButton.Pressed:
                HandleJoypad(joypadButton.ButtonIndex);
                break;
        }
    }

    /// <summary>
    /// Draws the slider track and fill.
    /// </summary>
    /// <remarks>The preview drawing path uses rectangle primitives.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Range.Ratio"/>
    public override void _Draw()
    {
        if (Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        var y = MathF.Max(0f, (Size.Y - 4f) * 0.5f);
        DrawRect(new Rect2(0f, y, Size.X, 4f), new Color(0.22f, 0.23f, 0.25f, 1f));
        DrawRect(new Rect2(0f, y, Size.X * (float)Ratio, 4f), new Color(0.35f, 0.55f, 0.95f, 1f));
        var knobX = Math.Clamp((float)Ratio * Size.X, 0f, Size.X);
        DrawRect(new Rect2(knobX - 4f, MathF.Max(0f, (Size.Y - 12f) * 0.5f), 8f, 12f), Color.White);
    }

    /// <summary>
    /// Gets the minimum size requested by this slider.
    /// </summary>
    /// <returns>A baseline horizontal slider size.</returns>
    /// <remarks>The minimum size does not depend on ticks in this preview.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Control.GetMinimumSize"/>
    public override Vector2 _GetMinimumSize()
    {
        return new Vector2(64f, 16f);
    }

    private void SetValueFromPosition(Vector2 globalPosition)
    {
        if (Size.X <= 0f)
        {
            return;
        }

        var localX = globalPosition.X - GlobalPosition.X;
        Ratio = Math.Clamp(localX / Size.X, 0f, 1f);
    }

    private void HandleKey(Key key)
    {
        switch (key)
        {
            case Key.Left:
            case Key.Down:
                OffsetValue(-GetStepOrOne());
                AcceptEvent();
                break;
            case Key.Right:
            case Key.Up:
                OffsetValue(GetStepOrOne());
                AcceptEvent();
                break;
            case Key.Home:
                Value = MinValue;
                AcceptEvent();
                break;
            case Key.End:
                Value = MaxValue;
                AcceptEvent();
                break;
        }
    }

    private void HandleJoypad(JoyButton button)
    {
        switch (button)
        {
            case JoyButton.DpadLeft:
            case JoyButton.DpadDown:
                OffsetValue(-GetStepOrOne());
                AcceptEvent();
                break;
            case JoyButton.DpadRight:
            case JoyButton.DpadUp:
                OffsetValue(GetStepOrOne());
                AcceptEvent();
                break;
        }
    }

    private double GetStepOrOne()
    {
        return Step > 0d ? Step : 1d;
    }
}
