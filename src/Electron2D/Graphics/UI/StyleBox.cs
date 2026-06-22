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
/// Provides the base resource for drawing themed UI boxes.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>StyleBox</c> stores content margins and exposes a drawing hook used by
/// controls that resolve style box theme items.
/// </para>
/// <para>
/// The base implementation does not submit draw commands. Use
/// <see cref="StyleBoxFlat"/> when a rectangular background and borders are
/// needed.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate style boxes on the main
/// scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Theme"/>
/// <seealso cref="StyleBoxFlat"/>
public class StyleBox : Resource
{
    private float contentMarginLeft;
    private float contentMarginTop;
    private float contentMarginRight;
    private float contentMarginBottom;

    /// <summary>
    /// Initializes a new instance of the <see cref="StyleBox"/> class.
    /// </summary>
    ///
    /// <remarks>
    /// The new style box starts with zero content margins and no drawing
    /// output.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This constructor is not synchronized. Call it from the main scene
    /// thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This constructor is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="StyleBox"/>
    public StyleBox()
    {
    }

    /// <summary>
    /// Gets or sets the left content margin.
    /// </summary>
    ///
    /// <value>
    /// The left margin in UI units. The value must be finite and
    /// non-negative.
    /// </value>
    ///
    /// <remarks>
    /// Controls can use this margin when computing a minimum content area.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMinimumSize"/>
    public float ContentMarginLeft
    {
        get
        {
            ThrowIfFreed();
            return contentMarginLeft;
        }
        set
        {
            ThrowIfFreed();
            contentMarginLeft = ValidateMargin(value, nameof(ContentMarginLeft));
        }
    }

    /// <summary>
    /// Gets or sets the top content margin.
    /// </summary>
    ///
    /// <value>
    /// The top margin in UI units. The value must be finite and non-negative.
    /// </value>
    ///
    /// <remarks>
    /// Controls can use this margin when computing a minimum content area.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMinimumSize"/>
    public float ContentMarginTop
    {
        get
        {
            ThrowIfFreed();
            return contentMarginTop;
        }
        set
        {
            ThrowIfFreed();
            contentMarginTop = ValidateMargin(value, nameof(ContentMarginTop));
        }
    }

    /// <summary>
    /// Gets or sets the right content margin.
    /// </summary>
    ///
    /// <value>
    /// The right margin in UI units. The value must be finite and
    /// non-negative.
    /// </value>
    ///
    /// <remarks>
    /// Controls can use this margin when computing a minimum content area.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMinimumSize"/>
    public float ContentMarginRight
    {
        get
        {
            ThrowIfFreed();
            return contentMarginRight;
        }
        set
        {
            ThrowIfFreed();
            contentMarginRight = ValidateMargin(value, nameof(ContentMarginRight));
        }
    }

    /// <summary>
    /// Gets or sets the bottom content margin.
    /// </summary>
    ///
    /// <value>
    /// The bottom margin in UI units. The value must be finite and
    /// non-negative.
    /// </value>
    ///
    /// <remarks>
    /// Controls can use this margin when computing a minimum content area.
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the assigned value is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="GetMinimumSize"/>
    public float ContentMarginBottom
    {
        get
        {
            ThrowIfFreed();
            return contentMarginBottom;
        }
        set
        {
            ThrowIfFreed();
            contentMarginBottom = ValidateMargin(value, nameof(ContentMarginBottom));
        }
    }

    /// <summary>
    /// Gets the minimum size contributed by content margins.
    /// </summary>
    ///
    /// <returns>
    /// The sum of horizontal and vertical content margins.
    /// </returns>
    ///
    /// <remarks>
    /// The base style box has no intrinsic graphic size beyond its margins.
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
    /// <seealso cref="ContentMarginLeft"/>
    /// <seealso cref="ContentMarginRight"/>
    public Vector2 GetMinimumSize()
    {
        ThrowIfFreed();
        return new Vector2(ContentMarginLeft + ContentMarginRight, ContentMarginTop + ContentMarginBottom);
    }

    /// <summary>
    /// Draws this style box into a canvas item.
    /// </summary>
    ///
    /// <param name="canvasItem">
    /// The canvas item that receives draw commands.
    /// </param>
    ///
    /// <param name="rect">
    /// The local rectangle to draw into.
    /// </param>
    ///
    /// <remarks>
    /// The base implementation validates the input and submits no commands.
    /// Derived style boxes override this method to draw their visuals.
    /// </remarks>
    ///
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="canvasItem"/> is <c>null</c>.
    /// </exception>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="rect"/> contains non-finite components.
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
    /// <seealso cref="CanvasItem.DrawRect(Rect2, Color, bool, float, bool)"/>
    public virtual void Draw(CanvasItem canvasItem, Rect2 rect)
    {
        ThrowIfFreed();
        ArgumentNullException.ThrowIfNull(canvasItem);
        ValidateRect(rect, nameof(rect));
    }

    protected static void ValidateRect(Rect2 rect, string parameterName)
    {
        if (!Mathf.IsFinite(rect.Position.X) ||
            !Mathf.IsFinite(rect.Position.Y) ||
            !Mathf.IsFinite(rect.Size.X) ||
            !Mathf.IsFinite(rect.Size.Y))
        {
            throw new ArgumentOutOfRangeException(parameterName, rect, "Rectangle components must be finite.");
        }
    }

    private static float ValidateMargin(float value, string parameterName)
    {
        if (!Mathf.IsFinite(value) || value < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, "Content margin must be finite and non-negative.");
        }

        return value;
    }
}
