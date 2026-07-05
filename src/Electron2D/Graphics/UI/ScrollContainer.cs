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
/// Clips and scrolls direct child controls inside a viewport-sized rectangle.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>ScrollContainer</c> stores programmatic scroll offsets and offsets visible
/// child controls by those values. It does not create public scrollbar nodes in
/// the 0.1-preview runtime.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Container"/>
/// <seealso cref="ScrollMode"/>
public class ScrollContainer : Container
{
    private int scrollHorizontal;
    private int scrollVertical;
    private int scrollDeadzone;
    private float scrollHorizontalCustomStep = -1f;
    private float scrollVerticalCustomStep = -1f;

    /// <summary>
    /// Initializes a new instance of the <see cref="ScrollContainer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new scroll container enables <see cref="Control.ClipContents"/> so
    /// pointer hit-testing is clipped to the viewport rectangle by default.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="Control.ClipContents"/>
    public ScrollContainer()
    {
        ClipContents = true;
    }

    /// <summary>
    /// Gets or sets whether a focus border should be drawn by future widget renderers.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to request a focus border; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The preview runtime stores this UI policy value but does not render a
    /// built-in focus border yet.
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
    public bool DrawFocusBorder { get; set; }

    /// <summary>
    /// Gets or sets whether focused descendant controls should be revealed automatically.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to let future focus handling reveal focused descendants;
    /// otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Automatic focus following is stored for API compatibility. Manual
    /// reveal is available through <see cref="EnsureControlVisible(Control)"/>.
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
    /// <seealso cref="EnsureControlVisible(Control)"/>
    public bool FollowFocus { get; set; }

    /// <summary>
    /// Gets or sets the horizontal scroll mode.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="ScrollMode"/> for the horizontal axis.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="ScrollMode.Disabled"/> clamps <see cref="ScrollHorizontal"/>
    /// to zero during layout. Other values keep programmatic scrolling enabled.
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
    /// <seealso cref="VerticalScrollMode"/>
    public ScrollMode HorizontalScrollMode { get; set; } = ScrollMode.Auto;

    /// <summary>
    /// Gets or sets the vertical scroll mode.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="ScrollMode"/> for the vertical axis.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// <see cref="ScrollMode.Disabled"/> clamps <see cref="ScrollVertical"/> to
    /// zero during layout. Other values keep programmatic scrolling enabled.
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
    /// <seealso cref="HorizontalScrollMode"/>
    public ScrollMode VerticalScrollMode { get; set; } = ScrollMode.Auto;

    /// <summary>
    /// Gets or sets the deadzone used by future drag-scroll handling.
    /// </summary>
    ///
    /// <value>
    /// A non-negative number of pixels.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Drag-scroll input is outside this task. The value is validated and stored
    /// so higher-level controls can share the same public API.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    public int ScrollDeadzone
    {
        get => scrollDeadzone;
        set
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Scroll deadzone must be non-negative.");
            }

            scrollDeadzone = value;
        }
    }

    /// <summary>
    /// Gets or sets the scroll hint mode.
    /// </summary>
    ///
    /// <value>
    /// The current <see cref="Electron2D.ScrollHintMode"/>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is stored for future visual hint rendering and does not change
    /// layout in the 0.1-preview runtime.
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
    public ScrollHintMode ScrollHintMode { get; set; } = ScrollHintMode.All;

    /// <summary>
    /// Gets or sets the horizontal scroll offset.
    /// </summary>
    ///
    /// <value>
    /// The non-negative horizontal offset in pixels.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is clamped during layout according to content size and
    /// <see cref="HorizontalScrollMode"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ScrollVertical"/>
    public int ScrollHorizontal
    {
        get => scrollHorizontal;
        set
        {
            scrollHorizontal = ValidateScrollOffset(value, nameof(value));
            QueueSort();
        }
    }

    /// <summary>
    /// Gets or sets whether horizontal scrolling is preferred by default.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to prefer horizontal scroll gestures; otherwise,
    /// <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is stored for input policy and does not affect layout.
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
    public bool ScrollHorizontalByDefault { get; set; }

    /// <summary>
    /// Gets or sets the custom horizontal scroll step.
    /// </summary>
    ///
    /// <value>
    /// A positive finite step value, or a negative value to use the default.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The preview runtime stores the step for future input handling.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is zero or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    public float ScrollHorizontalCustomStep
    {
        get => scrollHorizontalCustomStep;
        set => scrollHorizontalCustomStep = ValidateCustomStep(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets the vertical scroll offset.
    /// </summary>
    ///
    /// <value>
    /// The non-negative vertical offset in pixels.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is clamped during layout according to content size and
    /// <see cref="VerticalScrollMode"/>.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ScrollHorizontal"/>
    public int ScrollVertical
    {
        get => scrollVertical;
        set
        {
            scrollVertical = ValidateScrollOffset(value, nameof(value));
            QueueSort();
        }
    }

    /// <summary>
    /// Gets or sets the custom vertical scroll step.
    /// </summary>
    ///
    /// <value>
    /// A positive finite step value, or a negative value to use the default.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The preview runtime stores the step for future input handling.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is zero or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1-preview.
    /// </since>
    public float ScrollVerticalCustomStep
    {
        get => scrollVerticalCustomStep;
        set => scrollVerticalCustomStep = ValidateCustomStep(value, nameof(value));
    }

    /// <summary>
    /// Gets or sets whether scroll hints should be tiled by future renderers.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to request tiled hints; otherwise, <c>false</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// The value is stored for future visual hint rendering.
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
    public bool TileScrollHint { get; set; }

    /// <summary>
    /// Scrolls until the specified descendant control is visible.
    /// </summary>
    ///
    /// <param name="control">
    /// The descendant control that should be made visible inside this scroll
    /// container.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// The method adjusts <see cref="ScrollHorizontal"/> and
    /// <see cref="ScrollVertical"/> so the descendant rectangle fits inside
    /// this container's current <see cref="Control.Size"/> when possible.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="control"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="control"/> is not a descendant of this
    /// scroll container.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="ScrollHorizontal"/>
    /// <seealso cref="ScrollVertical"/>
    public void EnsureControlVisible(Control control)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(control);
        if (!IsDescendant(control))
        {
            throw new InvalidOperationException("The control must be a descendant of this scroll container.");
        }

        var targetRect = GetRectRelativeToThis(control);
        var contentPosition = targetRect.Position;
        var desiredHorizontal = scrollHorizontal;
        var desiredVertical = scrollVertical;

        if (contentPosition.X < desiredHorizontal)
        {
            desiredHorizontal = (int)MathF.Floor(contentPosition.X);
        }
        else if (contentPosition.X + targetRect.Size.X > desiredHorizontal + Size.X)
        {
            desiredHorizontal = (int)MathF.Ceiling(contentPosition.X + targetRect.Size.X - Size.X);
        }

        if (contentPosition.Y < desiredVertical)
        {
            desiredVertical = (int)MathF.Floor(contentPosition.Y);
        }
        else if (contentPosition.Y + targetRect.Size.Y > desiredVertical + Size.Y)
        {
            desiredVertical = (int)MathF.Ceiling(contentPosition.Y + targetRect.Size.Y - Size.Y);
        }

        scrollHorizontal = Math.Max(0, desiredHorizontal);
        scrollVertical = Math.Max(0, desiredVertical);
        ClampScrollOffsets(MeasureContentSize());
        QueueSort();
    }

    protected override void SortChildren()
    {
        var contentSize = MeasureContentSize();
        ClampScrollOffsets(contentSize);
        foreach (var child in GetLayoutChildren())
        {
            FitChildInRect(
                child,
                new Rect2(
                    -scrollHorizontal,
                    -scrollVertical,
                    MathF.Max(Size.X, child.GetCombinedMinimumSize().X),
                    MathF.Max(Size.Y, child.GetCombinedMinimumSize().Y)));
        }
    }

    private Vector2 MeasureContentSize()
    {
        var contentSize = Size;
        foreach (var child in GetLayoutChildren())
        {
            contentSize = Max(contentSize, child.GetCombinedMinimumSize());
        }

        return contentSize;
    }

    private void ClampScrollOffsets(Vector2 contentSize)
    {
        var maxHorizontal = HorizontalScrollMode == ScrollMode.Disabled ? 0 : (int)MathF.Max(0f, contentSize.X - Size.X);
        var maxVertical = VerticalScrollMode == ScrollMode.Disabled ? 0 : (int)MathF.Max(0f, contentSize.Y - Size.Y);
        scrollHorizontal = Math.Clamp(scrollHorizontal, 0, maxHorizontal);
        scrollVertical = Math.Clamp(scrollVertical, 0, maxVertical);
    }

    private bool IsDescendant(Control control)
    {
        var current = control.GetParent();
        while (current is not null)
        {
            if (ReferenceEquals(current, this))
            {
                return true;
            }

            current = current.GetParent();
        }

        return false;
    }

    private Rect2 GetRectRelativeToThis(Control control)
    {
        var position = control.Position;
        var current = control.GetParent();
        while (current is Control parentControl)
        {
            var parent = parentControl.GetParent();
            if (ReferenceEquals(parent, this))
            {
                break;
            }

            if (ReferenceEquals(parentControl, this))
            {
                break;
            }

            position += parentControl.Position;
            current = parent;
        }

        return new Rect2(position, control.Size);
    }

    private static int ValidateScrollOffset(int value, string parameterName)
    {
        if (value < 0)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Scroll offset must be non-negative.");
        }

        return value;
    }

    private static float ValidateCustomStep(float value, string parameterName)
    {
        if (!Mathf.IsFinite(value) || value == 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Custom scroll step must be finite and either negative for default behavior or greater than zero.");
        }

        return value;
    }
}
