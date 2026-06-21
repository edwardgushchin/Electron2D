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
/// Provides the Electron2D base node for 2D user interface controls.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Control</c> inherits from <see cref="CanvasItem"/> and adds a rectangular
/// UI area, theme font overrides, GUI input callbacks, mouse filtering and
/// focus ownership used by Electron2D UI nodes.
/// </para>
/// <para>
/// Anchors, containers, keyboard navigation graphs and full widgets are later
/// UI tasks, but the baseline input pipeline can already route mouse and
/// focused keyboard events to controls.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate controls on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="CanvasItem" />
/// <seealso cref="Label" />
public class Control : CanvasItem
{

    /// <summary>
    /// Initializes a new instance of the Control type.
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
    /// <seealso cref="Control" />
    ///
    public Control()
    {
    }

    private readonly Dictionary<string, Font> fontOverrides = new(StringComparer.Ordinal);
    private readonly Dictionary<string, int> fontSizeOverrides = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets or sets the local position of this control.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current position value.
    /// </value>
    ///
    /// <seealso cref="Control" />
    ///
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets the local size of this control.
    /// </summary>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This property follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <value>
    /// The current size value.
    /// </value>
    ///
    /// <seealso cref="Control" />
    ///
    public Vector2 Size { get; set; } = Vector2.Zero;

    /// <summary>
    /// Gets or sets how this control receives and consumes mouse input.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="MouseFilter"/> value. The default is
    /// <see cref="MouseFilter.Stop"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The root <see cref="Viewport"/> reads this property while routing
    /// <see cref="InputEventMouse"/> events to <see cref="_GuiInput(InputEvent)"/>.
    /// </para>
    /// <para>
    /// <see cref="MouseFilter.Stop"/> handles the event after this control
    /// receives it. <see cref="MouseFilter.Pass"/> lets unhandled events bubble
    /// to the parent control. <see cref="MouseFilter.Ignore"/> skips this
    /// control for mouse hit-testing.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="MouseFilter"/>
    /// <seealso cref="_GuiInput(InputEvent)"/>
    public MouseFilter MouseFilter { get; set; } = MouseFilter.Stop;

    /// <summary>
    /// Gets or sets how this control can receive focus.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="Electron2D.FocusMode"/> value. The default is
    /// <see cref="Electron2D.FocusMode.None"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Focus is owned by the nearest <see cref="Viewport"/>. Only one visible
    /// control inside that viewport can report <see cref="HasFocus"/> at a
    /// time.
    /// </para>
    /// <para>
    /// <see cref="Electron2D.FocusMode.Click"/> and
    /// <see cref="Electron2D.FocusMode.All"/> allow mouse press events to focus
    /// this control before <see cref="_GuiInput(InputEvent)"/> is called.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="FocusMode"/>
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="HasFocus"/>
    public FocusMode FocusMode { get; set; } = FocusMode.None;

    /// <summary>
    /// Called when a GUI input event is delivered to this control.
    /// </summary>
    ///
    /// <param name="inputEvent">
    /// The input event delivered by the containing <see cref="Viewport"/>.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// Mouse events reach this method when the event position falls inside the
    /// control rectangle and <see cref="MouseFilter"/> is not
    /// <see cref="MouseFilter.Ignore"/>. Non-mouse events reach only the
    /// currently focused control.
    /// </para>
    /// <para>
    /// Call <see cref="AcceptEvent"/> to stop further propagation of the
    /// current event.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. It is called on the main scene thread
    /// during input dispatch.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="AcceptEvent"/>
    /// <seealso cref="MouseFilter"/>
    /// <seealso cref="FocusMode"/>
    public virtual void _GuiInput(InputEvent inputEvent)
    {
    }

    /// <summary>
    /// Marks the current GUI input event as handled.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// This method forwards to <see cref="Viewport.SetInputAsHandled"/> on the
    /// containing viewport. It has no effect when this control is outside a
    /// scene tree or when no input event is currently being dispatched.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread while
    /// handling <see cref="_GuiInput(InputEvent)"/>.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="_GuiInput(InputEvent)"/>
    /// <seealso cref="Viewport.SetInputAsHandled"/>
    public void AcceptEvent()
    {
        ThrowIfFreed();
        GetViewport()?.SetInputAsHandled();
    }

    /// <summary>
    /// Gives keyboard and gamepad focus to this control.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The control must be inside a scene tree, visible in that tree and have
    /// <see cref="FocusMode"/> set to a value other than
    /// <see cref="Electron2D.FocusMode.None"/>. If any of those conditions is
    /// not met, the call has no effect.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="ReleaseFocus"/>
    /// <seealso cref="HasFocus"/>
    /// <seealso cref="FocusMode"/>
    public void GrabFocus()
    {
        ThrowIfFreed();
        GetViewport()?.GrabFocus(this);
    }

    /// <summary>
    /// Releases focus from this control when it currently owns it.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// Calling this method on a control that does not currently own focus is a
    /// no-op.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="HasFocus"/>
    public void ReleaseFocus()
    {
        ThrowIfFreed();
        GetViewport()?.ReleaseFocus(this);
    }

    /// <summary>
    /// Checks whether this control currently owns focus in its viewport.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when this control is the focused visible control in its
    /// viewport; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Hidden controls and controls outside a scene tree do not report focus,
    /// even if they were the last control selected before becoming invalid for
    /// focus dispatch.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GrabFocus"/>
    /// <seealso cref="ReleaseFocus"/>
    public bool HasFocus()
    {
        ThrowIfFreed();
        return GetViewport()?.HasFocus(this) == true;
    }

    /// <summary>
    /// Adds or replaces a font theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font name, for example <c>font</c>.</param>
    /// <param name="font">The font resource to use.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="font" /> is <c>null</c>.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Control" />
    ///
    public void AddThemeFontOverride(string name, Font font)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(font);

        fontOverrides[name] = font;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font name.</param>
    /// <returns>The overridden font, or <c>null</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Control" />
    ///
    public Font? GetThemeFont(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontOverrides.TryGetValue(name, out var font) ? font : null;
    }

    /// <summary>
    /// Adds or replaces a font size theme override for this control.
    /// </summary>
    ///
    /// <param name="name">The theme font size name, for example <c>font_size</c>.</param>
    /// <param name="fontSize">The font size in pixels. It must be greater than zero.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fontSize" /> is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Control" />
    ///
    public void AddThemeFontSizeOverride(string name, int fontSize)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
        }

        fontSizeOverrides[name] = fontSize;
        QueueRedraw();
    }

    /// <summary>
    /// Gets a font size theme override by name.
    /// </summary>
    ///
    /// <param name="name">The theme font size name.</param>
    /// <returns>The overridden font size in pixels, or <c>16</c> when no override exists.</returns>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="name" /> is <c>null</c>, empty or whitespace.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="Control" />
    ///
    public int GetThemeFontSize(string name)
    {
        ThrowIfFreed();
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return fontSizeOverrides.TryGetValue(name, out var fontSize) ? fontSize : 16;
    }

    internal Vector2 GlobalPosition
    {
        get
        {
            ThrowIfFreed();
            return GetParent() switch
            {
                Control parentControl => parentControl.GlobalPosition + Position,
                Node2D parentNode2D => parentNode2D.GlobalTransform.Xform(Position),
                _ => Position
            };
        }
    }

    internal Transform2D GlobalTransform => new(Vector2.Right, Vector2.Down, GlobalPosition);

    internal bool CanReceiveMouseInput(Vector2 globalPosition)
    {
        ThrowIfFreed();
        return MouseFilter != MouseFilter.Ignore &&
            Size.X > 0f &&
            Size.Y > 0f &&
            IsVisibleInTree() &&
            new Rect2(GlobalPosition, Size).HasPoint(globalPosition);
    }

    internal bool CanReceiveFocus(Viewport viewport)
    {
        ThrowIfFreed();
        return FocusMode != FocusMode.None &&
            IsInsideTree() &&
            IsVisibleInTree() &&
            ReferenceEquals(GetViewport(), viewport);
    }

    internal void DispatchGuiInput(InputEvent inputEvent)
    {
        ArgumentNullException.ThrowIfNull(inputEvent);
        GetTree()?.InvokeUserCallback(this, nameof(_GuiInput), () => _GuiInput(inputEvent));
    }
}
