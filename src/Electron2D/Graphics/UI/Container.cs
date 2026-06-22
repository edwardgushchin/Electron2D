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
/// Provides the base control for nodes that arrange direct child controls.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>Container</c> is the common base for layout controls in the 0.1.0 Preview
/// UI surface. It only arranges direct children that inherit from
/// <see cref="Control"/> and leaves other child nodes untouched.
/// </para>
/// <para>
/// Layout is recalculated during the scene tree process step. Call
/// <see cref="QueueSort"/> after changing custom container state when an
/// immediate frame has not already been scheduled by the caller.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate containers on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Control"/>
/// <seealso cref="HBoxContainer"/>
/// <seealso cref="VBoxContainer"/>
public class Container : Control
{
    private bool sortQueued = true;

    /// <summary>
    /// Initializes a new instance of the <see cref="Container"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new container starts with layout queued so its children are arranged
    /// on the next scene tree process step.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="QueueSort"/>
    public Container()
    {
    }

    /// <summary>
    /// Fits a direct child control inside a local rectangle.
    /// </summary>
    ///
    /// <param name="child">
    /// The direct child control to place.
    /// </param>
    ///
    /// <param name="rect">
    /// The local rectangle assigned to <paramref name="child"/>.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// This method resets the child anchors to the local rectangle mode used by
    /// containers, then writes the final position and size through the public
    /// <see cref="Control.Position"/> and <see cref="Control.Size"/> API.
    /// </para>
    /// <para>
    /// A child is never made smaller than
    /// <see cref="Control.GetCombinedMinimumSize"/>. If <paramref name="rect"/>
    /// is too small, the child may extend past the rectangle.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="child"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rect"/> contains a negative size or a
    /// non-finite component.
    /// </exception>
    ///
    /// <exception cref="InvalidOperationException">
    /// Thrown when <paramref name="child"/> is not a direct child of this
    /// container.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="QueueSort"/>
    /// <seealso cref="Control.GetCombinedMinimumSize"/>
    public void FitChildInRect(Control child, Rect2 rect)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(child);
        ValidateRect(rect, nameof(rect));

        if (!ReferenceEquals(child.GetParent(), this))
        {
            throw new InvalidOperationException("The control must be a direct child of this container.");
        }

        var finalSize = Max(rect.Size, child.GetCombinedMinimumSize());
        child.AnchorLeft = 0f;
        child.AnchorTop = 0f;
        child.AnchorRight = 0f;
        child.AnchorBottom = 0f;
        child.Position = rect.Position;
        child.Size = finalSize;
    }

    /// <summary>
    /// Queues this container for a layout recalculation.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The preview runtime recalculates container layout during
    /// <see cref="Node._Process(double)"/>. Multiple calls before a frame are
    /// coalesced.
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
    /// <seealso cref="FitChildInRect(Control, Rect2)"/>
    public void QueueSort()
    {
        ThrowIfFreed();
        sortQueued = true;
    }

    /// <summary>
    /// Called every process frame to recalculate queued container layout.
    /// </summary>
    ///
    /// <param name="delta">
    /// The elapsed frame time in seconds.
    /// </param>
    ///
    /// <remarks>
    /// <para>
    /// Container layout is recalculated every process frame in the preview
    /// runtime so parent size changes are reflected without requiring a hidden
    /// resize notification system.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. The scene tree calls it on the main
    /// scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="QueueSort"/>
    public override void _Process(double delta)
    {
        if (sortQueued)
        {
            SortChildren();
            sortQueued = false;
            return;
        }

        SortChildren();
    }

    protected virtual void SortChildren()
    {
    }

    protected Control[] GetLayoutChildren()
    {
        return GetChildrenSnapshot()
            .OfType<Control>()
            .Where(child => child.Visible)
            .ToArray();
    }

    protected static Vector2 Max(Vector2 left, Vector2 right)
    {
        return new Vector2(MathF.Max(left.X, right.X), MathF.Max(left.Y, right.Y));
    }

    protected static bool UsesExpand(SizeFlags flags)
    {
        return (flags & SizeFlags.Expand) == SizeFlags.Expand;
    }

    protected static bool UsesFill(SizeFlags flags)
    {
        return (flags & SizeFlags.Fill) == SizeFlags.Fill;
    }

    protected static float AlignOffset(float slotSize, float itemSize, SizeFlags flags)
    {
        var free = MathF.Max(0f, slotSize - itemSize);
        if ((flags & SizeFlags.ShrinkEnd) == SizeFlags.ShrinkEnd)
        {
            return free;
        }

        return (flags & SizeFlags.ShrinkCenter) == SizeFlags.ShrinkCenter ? free / 2f : 0f;
    }

    protected static float ClampNonNegative(float value)
    {
        return Mathf.IsFinite(value) && value > 0f ? value : 0f;
    }

    private static void ValidateRect(Rect2 rect, string parameterName)
    {
        if (!Mathf.IsFinite(rect.Position.X) ||
            !Mathf.IsFinite(rect.Position.Y) ||
            !Mathf.IsFinite(rect.Size.X) ||
            !Mathf.IsFinite(rect.Size.Y) ||
            rect.Size.X < 0f ||
            rect.Size.Y < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, rect, "Rectangle components must be finite and size components must be non-negative.");
        }
    }
}
