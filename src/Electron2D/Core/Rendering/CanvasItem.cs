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
using System.Threading;

namespace Electron2D;

/// <summary>
/// Provides the Godot-like base node for items that can be drawn on a 2D canvas.
/// </summary>
///
/// <remarks>
/// <para>
/// `CanvasItem` owns visibility, color modulation and draw-order properties for
/// 2D nodes. Electron2D 0.1.0 Preview implements the subset required by
/// `Node2D`, `Sprite2D` and internal sprite submission.
/// </para>
///
/// <para>
/// Visibility and `Modulate` are inherited only through direct `CanvasItem`
/// ancestors. A plain <see cref="Node" /> between two canvas items breaks the
/// inherited canvas chain, matching the Godot behavior used by this preview.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate canvas items on the main
/// thread that owns the scene tree.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Node2D" />
/// <seealso cref="Sprite2D" />
public class CanvasItem : Node
{
    private static long nextCanvasItemId;
    private readonly Rid canvasItemRid = new(Interlocked.Increment(ref nextCanvasItemId));

    /// <summary>
    /// Gets or sets whether this canvas item is visible.
    /// </summary>
    ///
    /// <remarks>
    /// Hidden canvas items also hide direct `CanvasItem` descendants in the
    /// inherited canvas chain. Descendants separated by a non-`CanvasItem`
    /// node start a new independent canvas chain.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Gets or sets the color multiplied into this item and its direct canvas descendants.
    /// </summary>
    ///
    /// <remarks>
    /// The default value is <see cref="Color.White" />. During submission,
    /// inherited `Modulate` values are multiplied before the current
    /// <see cref="SelfModulate" /> is applied to the current item.
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
    /// <seealso cref="SelfModulate" />
    public Color Modulate { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the color multiplied into this item only.
    /// </summary>
    ///
    /// <remarks>
    /// `SelfModulate` does not affect children. Use <see cref="Modulate" />
    /// when a color should be inherited by direct canvas descendants.
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
    /// <seealso cref="Modulate" />
    public Color SelfModulate { get; set; } = Color.White;

    /// <summary>
    /// Gets or sets the 2D draw-order index inside the current canvas layer.
    /// </summary>
    ///
    /// <remarks>
    /// Lower values are drawn before higher values within the same
    /// <see cref="CanvasLayer" />. Layer order still has priority over
    /// `ZIndex`.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public int ZIndex { get; set; }

    /// <summary>
    /// Gets or sets whether this item participates in Y-sort ordering.
    /// </summary>
    ///
    /// <remarks>
    /// Electron2D 0.1.0 Preview forwards this flag to the internal render
    /// queue. The full Godot Y-sort container behavior is intentionally
    /// limited to the existing queue ordering rules.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This property is not synchronized. Mutate it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This property is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool YSortEnabled { get; set; }

    internal Rid CanvasItemRid => canvasItemRid;

    /// <summary>
    /// Shows this canvas item.
    /// </summary>
    ///
    /// <remarks>
    /// This is equivalent to setting <see cref="Visible" /> to <c>true</c>.
    /// Ancestor visibility can still make <see cref="IsVisibleInTree" />
    /// return <c>false</c>.
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
    /// <seealso cref="Hide" />
    public void Show()
    {
        ThrowIfFreed();
        Visible = true;
    }

    /// <summary>
    /// Hides this canvas item.
    /// </summary>
    ///
    /// <remarks>
    /// This is equivalent to setting <see cref="Visible" /> to <c>false</c>.
    /// Direct canvas descendants become hidden through the inherited canvas
    /// chain.
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
    /// <seealso cref="Show" />
    public void Hide()
    {
        ThrowIfFreed();
        Visible = false;
    }

    /// <summary>
    /// Checks whether this canvas item is visible after direct canvas ancestors are considered.
    /// </summary>
    ///
    /// <returns>
    /// <c>true</c> when this item and its direct `CanvasItem` ancestor chain
    /// are visible; otherwise, <c>false</c>.
    /// </returns>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    public bool IsVisibleInTree()
    {
        ThrowIfFreed();
        if (!Visible)
        {
            return false;
        }

        var current = GetParent();
        while (current is CanvasItem canvasItem)
        {
            if (!canvasItem.Visible)
            {
                return false;
            }

            current = canvasItem.GetParent();
        }

        return true;
    }
}
