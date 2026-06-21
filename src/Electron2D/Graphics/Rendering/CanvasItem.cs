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
/// Provides the Electron2D base node for items that can be drawn on a 2D canvas.
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
/// inherited canvas chain, matching the canvas inheritance behavior used by this preview.
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

    /// <summary>
    /// Initializes a new instance of the CanvasItem type.
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
    /// <seealso cref="CanvasItem" />
    ///
    public CanvasItem()
    {
    }

    private static long nextCanvasItemId;
    private readonly Rid canvasItemRid = new(Interlocked.Increment(ref nextCanvasItemId));
    private readonly List<CanvasItemDrawingCommand> drawingCommands = new();
    private bool redrawQueued = true;
    private bool drawing;

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
    /// <value>
    /// The current visible value.
    /// </value>
    ///
    /// <seealso cref="CanvasItem" />
    ///
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
    /// <value>
    /// The current modulate value.
    /// </value>
    ///
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
    /// <value>
    /// The current self modulate value.
    /// </value>
    ///
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
    /// <value>
    /// The current zindex value.
    /// </value>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public int ZIndex { get; set; }

    /// <summary>
    /// Gets or sets whether this item participates in Y-sort ordering.
    /// </summary>
    ///
    /// <remarks>
    /// Electron2D 0.1.0 Preview forwards this flag to the internal render
    /// queue. The full Y-sort container behavior is intentionally
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
    /// <value>
    /// The current ysort enabled value.
    /// </value>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public bool YSortEnabled { get; set; }

    internal Rid CanvasItemRid => canvasItemRid;

    internal IReadOnlyList<CanvasItemDrawingCommand> DrawingCommands => drawingCommands;

    /// <summary>
    /// Gets the 2D world associated with this canvas item.
    /// </summary>
    /// <returns>
    /// A <see cref="World2D" /> object whose direct space state can query the
    /// current <see cref="SceneTree" />, or an empty world when this item is not
    /// inside a tree.
    /// </returns>
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
    /// <seealso cref="CanvasItem" />
    ///
    public World2D GetWorld2D()
    {
        ThrowIfFreed();
        return new World2D(GetTree()?.Root);
    }

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
    /// Called when this canvas item has been requested to redraw.
    /// </summary>
    ///
    /// <remarks>
    /// Override this method and call the `Draw*` methods from inside the
    /// override. Draw commands are cached and reused until
    /// <see cref="QueueRedraw" /> requests another draw callback.
    /// </remarks>
    ///
    /// <threadsafety>
    /// This callback is invoked on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="QueueRedraw" />
    public virtual void _Draw()
    {
    }

    /// <summary>
    /// Queues this canvas item to redraw during the next processed frame.
    /// </summary>
    ///
    /// <remarks>
    /// Multiple calls before the next frame are coalesced into one
    /// <see cref="_Draw" /> callback. Cached draw commands remain active until
    /// the callback runs again.
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
    /// <seealso cref="_Draw" />
    public void QueueRedraw()
    {
        ThrowIfFreed();
        redrawQueued = true;
    }

    /// <summary>
    /// Draws a line between two local-space points.
    /// </summary>
    ///
    /// <param name="from">The local-space start point.</param>
    /// <param name="to">The local-space end point.</param>
    /// <param name="color">The line color.</param>
    /// <param name="width">The line width. Negative values represent a thin non-scaling primitive line.</param>
    /// <param name="antialiased">Whether the line should request antialiasing when supported.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public void DrawLine(Vector2 from, Vector2 to, Color color, float width = -1f, bool antialiased = false)
    {
        AddDrawingCommand(CanvasItemDrawingCommand.CreateLine(from, to, color, width, antialiased));
    }

    /// <summary>
    /// Draws a filled or stroked rectangle in local space.
    /// </summary>
    ///
    /// <param name="rect">The local-space rectangle.</param>
    /// <param name="color">The rectangle color.</param>
    /// <param name="filled">Whether the rectangle should be filled.</param>
    /// <param name="width">The stroke width used when <paramref name="filled" /> is <c>false</c>.</param>
    /// <param name="antialiased">Whether the stroke should request antialiasing when supported.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public void DrawRect(Rect2 rect, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        AddDrawingCommand(CanvasItemDrawingCommand.CreateRect(rect, color, filled, width, antialiased));
    }

    /// <summary>
    /// Draws a filled or stroked circle in local space.
    /// </summary>
    ///
    /// <param name="position">The local-space circle center.</param>
    /// <param name="radius">The circle radius. It must be finite and non-negative.</param>
    /// <param name="color">The circle color.</param>
    /// <param name="filled">Whether the circle should be filled.</param>
    /// <param name="width">The stroke width used when <paramref name="filled" /> is <c>false</c>.</param>
    /// <param name="antialiased">Whether the stroke should request antialiasing when supported.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="radius" /> is negative or not finite.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public void DrawCircle(Vector2 position, float radius, Color color, bool filled = true, float width = -1f, bool antialiased = false)
    {
        if (!Mathf.IsFinite(radius) || radius < 0f)
        {
            throw new ArgumentOutOfRangeException(nameof(radius), radius, "Circle radius must be finite and non-negative.");
        }

        AddDrawingCommand(CanvasItemDrawingCommand.CreateCircle(position, radius, color, filled, width, antialiased));
    }

    /// <summary>
    /// Draws a solid polygon using per-point colors.
    /// </summary>
    ///
    /// <param name="points">The local-space polygon points. At least three points are required.</param>
    /// <param name="colors">The colors for each point. The array length must match <paramref name="points" />.</param>
    /// <param name="uvs">Optional texture coordinates. When provided, the array length must match <paramref name="points" />.</param>
    /// <param name="texture">An optional texture sampled by the polygon.</param>
    ///
    /// <exception cref="ArgumentException">
    /// Thrown when the point, color or UV arrays do not describe a valid polygon.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public void DrawPolygon(Vector2[] points, Color[] colors, Vector2[]? uvs = null, Texture2D? texture = null)
    {
        ArgumentNullException.ThrowIfNull(points);
        ArgumentNullException.ThrowIfNull(colors);
        if (points.Length < 3)
        {
            throw new ArgumentException("A polygon requires at least three points.", nameof(points));
        }

        if (colors.Length != points.Length)
        {
            throw new ArgumentException("Polygon colors length must match points length.", nameof(colors));
        }

        var commandUvs = uvs ?? Array.Empty<Vector2>();
        if (commandUvs.Length != 0 && commandUvs.Length != points.Length)
        {
            throw new ArgumentException("Polygon UV length must match points length when UVs are provided.", nameof(uvs));
        }

        AddDrawingCommand(CanvasItemDrawingCommand.CreatePolygon(
            points.ToArray(),
            colors.ToArray(),
            commandUvs.ToArray(),
            texture));
    }

    /// <summary>
    /// Draws a texture at a local-space position.
    /// </summary>
    ///
    /// <param name="texture">The texture to draw.</param>
    /// <param name="position">The local-space top-left draw position.</param>
    /// <param name="modulate">The optional color multiplied into the texture. The default is <see cref="Color.White" />.</param>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
    public void DrawTexture(Texture2D texture, Vector2 position, Color? modulate = null)
    {
        ArgumentNullException.ThrowIfNull(texture);
        AddDrawingCommand(CanvasItemDrawingCommand.CreateTexture(texture, position, modulate ?? Color.White));
    }

    /// <summary>
    /// Draws text using a font at a local-space baseline position.
    /// </summary>
    ///
    /// <param name="font">The font resource used to draw the text.</param>
    /// <param name="position">The local-space baseline position.</param>
    /// <param name="text">The text to draw.</param>
    /// <param name="alignment">The horizontal alignment used when <paramref name="width" /> is non-negative.</param>
    /// <param name="width">The optional clipping/alignment width. Negative values mean no width limit.</param>
    /// <param name="fontSize">The requested font size in pixels.</param>
    /// <param name="modulate">The optional color multiplied into the text. The default is <see cref="Color.White" />.</param>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when <paramref name="fontSize" /> is less than or equal to zero.
    /// </exception>
    ///
    /// <threadsafety>
    /// This method is not synchronized. Call it only from <see cref="_Draw" />
    /// on the main scene thread.
    /// </threadsafety>
    ///
    /// <since>
    /// This method is available since Electron2D 0.1.0 Preview.
    /// </since>
    ///
    /// <seealso cref="Font" />
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    public void DrawString(
        Font font,
        Vector2 position,
        string text,
        HorizontalAlignment alignment = HorizontalAlignment.Left,
        float width = -1f,
        int fontSize = 16,
        Color? modulate = null)
    {
        ArgumentNullException.ThrowIfNull(font);
        ArgumentNullException.ThrowIfNull(text);
        if (fontSize <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(fontSize), fontSize, "Font size must be greater than zero.");
        }

        AddDrawingCommand(CanvasItemDrawingCommand.CreateString(font, position, text, alignment, width, fontSize, modulate ?? Color.White));
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
    /// <remarks>
    /// This method follows the validation and lifetime rules of its declaring type.
    /// </remarks>
    ///
    /// <seealso cref="CanvasItem" />
    ///
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

    internal void DrawIfNeeded()
    {
        if (!redrawQueued || !IsVisibleInTree())
        {
            return;
        }

        redrawQueued = false;
        drawingCommands.Clear();
        drawing = true;
        try
        {
            GetTree()?.InvokeUserCallback(this, nameof(_Draw), _Draw);
        }
        finally
        {
            drawing = false;
        }
    }

    private void AddDrawingCommand(CanvasItemDrawingCommand command)
    {
        ThrowIfFreed();
        if (!drawing)
        {
            throw new InvalidOperationException("CanvasItem Draw* methods can only be called from _Draw().");
        }

        drawingCommands.Add(command);
    }
}
