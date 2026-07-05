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
/// Places direct child controls around the center of this container.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>CenterContainer</c> keeps child controls at their combined minimum size.
/// It does not stretch children to the full container rectangle.
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
public class CenterContainer : Container
{
    /// <summary>
    /// Initializes a new instance of the <see cref="CenterContainer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new container centers child rectangles by their full size until
    /// <see cref="UseTopLeft"/> is enabled.
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
    /// <seealso cref="UseTopLeft"/>
    public CenterContainer()
    {
    }

    /// <summary>
    /// Gets or sets whether the child top-left corner is placed at the center.
    /// </summary>
    ///
    /// <value>
    /// <c>true</c> to place child top-left corners at the center point;
    /// otherwise, <c>false</c> to center full child rectangles.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// This property affects all direct child controls on the next layout pass.
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
    /// <seealso cref="Container.QueueSort"/>
    public bool UseTopLeft { get; set; }

    /// <summary>
    /// Gets the minimum size required by this center container.
    /// </summary>
    ///
    /// <returns>
    /// The largest combined minimum size among direct child controls.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Centering does not add padding or separation.
    /// </para>
    /// </remarks>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1-preview.
    /// </since>
    ///
    /// <seealso cref="UseTopLeft"/>
    public override Vector2 _GetMinimumSize()
    {
        var minimum = Vector2.Zero;
        foreach (var child in GetLayoutChildren())
        {
            minimum = Max(minimum, child.GetCombinedMinimumSize());
        }

        return minimum;
    }

    protected override void SortChildren()
    {
        foreach (var child in GetLayoutChildren())
        {
            var childSize = child.GetCombinedMinimumSize();
            var position = UseTopLeft
                ? new Vector2(Size.X / 2f, Size.Y / 2f)
                : new Vector2((Size.X - childSize.X) / 2f, (Size.Y - childSize.Y) / 2f);
            FitChildInRect(child, new Rect2(position, childSize));
        }
    }
}
