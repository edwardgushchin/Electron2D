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
/// Provides a texture-backed UI rectangle with nine-patch margins.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>NinePatchRect</c> splits a source texture region into corner, edge and
/// center segments. Corners keep their margin sizes while edge and center
/// segments stretch or tile according to axis stretch settings.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate nine-patch rectangles on
/// the main scene thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1-preview.
/// </since>
///
/// <seealso cref="Texture2D"/>
/// <seealso cref="TextureRect"/>
public class NinePatchRect : Control
{
    private Texture2D? texture;
    private Rect2 regionRect;
    private int patchMarginLeft;
    private int patchMarginTop;
    private int patchMarginRight;
    private int patchMarginBottom;

    /// <summary>
    /// Identifies how the center segment of a nine-patch axis is expanded.
    /// </summary>
    ///
    /// <remarks>
    /// <para>
    /// The mode applies only to the middle column or row. Corners keep their
    /// destination margin sizes.
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
    /// <seealso cref="NinePatchRect.AxisStretchHorizontal"/>
    /// <seealso cref="NinePatchRect.AxisStretchVertical"/>
    public enum AxisStretchModeEnum
    {
        /// <summary>Stretches the center segment to fill the destination span.</summary>
        /// <remarks>Use this value for ordinary scalable panels.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="AxisStretchModeEnum"/>
        Stretch = 0,

        /// <summary>Tiles the center segment using its source size.</summary>
        /// <remarks>Partial edge tiles use a clipped source segment.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="AxisStretchModeEnum"/>
        Tile = 1,

        /// <summary>Tiles the center segment with adjusted tile size so it fits exactly.</summary>
        /// <remarks>Use this value to avoid clipped final tiles.</remarks>
        /// <since>This value is available since Electron2D 0.1-preview.</since>
        /// <seealso cref="AxisStretchModeEnum"/>
        TileFit = 2
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NinePatchRect"/> class.
    /// </summary>
    /// <remarks>The new control draws its center segment by default.</remarks>
    /// <threadsafety>This constructor is not synchronized. Call it from the main scene thread.</threadsafety>
    /// <since>This constructor is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="NinePatchRect"/>
    public NinePatchRect()
    {
        DrawCenter = true;
    }

    /// <summary>
    /// Gets or sets the texture drawn by this control.
    /// </summary>
    /// <value>The texture resource, or <c>null</c> to draw nothing.</value>
    /// <remarks>Assigning this property queues a redraw.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture2D"/>
    public Texture2D? Texture
    {
        get => texture;
        set
        {
            texture = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets the source region inside <see cref="Texture"/>.
    /// </summary>
    /// <value>A source rectangle in texture pixels, or an empty rectangle to use the full texture.</value>
    /// <remarks>Negative size components are rejected.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the rectangle is not finite or has negative size.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture"/>
    public Rect2 RegionRect
    {
        get => regionRect;
        set
        {
            ValidateRect(value, nameof(RegionRect));
            regionRect = value;
            QueueRedraw();
        }
    }

    /// <summary>
    /// Gets or sets whether the center segment is drawn.
    /// </summary>
    /// <value><c>true</c> to draw the center segment; otherwise, <c>false</c>.</value>
    /// <remarks>Edges and corners are still drawn when this property is <c>false</c>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="NinePatchRect"/>
    public bool DrawCenter { get; set; }

    /// <summary>
    /// Gets or sets the left patch margin in pixels.
    /// </summary>
    /// <value>A non-negative margin.</value>
    /// <remarks>The margin is clamped to available source and destination size during drawing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="PatchMarginRight"/>
    public int PatchMarginLeft
    {
        get => patchMarginLeft;
        set => patchMarginLeft = SetMargin(value);
    }

    /// <summary>
    /// Gets or sets the top patch margin in pixels.
    /// </summary>
    /// <value>A non-negative margin.</value>
    /// <remarks>The margin is clamped to available source and destination size during drawing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="PatchMarginBottom"/>
    public int PatchMarginTop
    {
        get => patchMarginTop;
        set => patchMarginTop = SetMargin(value);
    }

    /// <summary>
    /// Gets or sets the right patch margin in pixels.
    /// </summary>
    /// <value>A non-negative margin.</value>
    /// <remarks>The margin is clamped to available source and destination size during drawing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="PatchMarginLeft"/>
    public int PatchMarginRight
    {
        get => patchMarginRight;
        set => patchMarginRight = SetMargin(value);
    }

    /// <summary>
    /// Gets or sets the bottom patch margin in pixels.
    /// </summary>
    /// <value>A non-negative margin.</value>
    /// <remarks>The margin is clamped to available source and destination size during drawing.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when the assigned value is negative.</exception>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="PatchMarginTop"/>
    public int PatchMarginBottom
    {
        get => patchMarginBottom;
        set => patchMarginBottom = SetMargin(value);
    }

    /// <summary>
    /// Gets or sets horizontal center stretch behavior.
    /// </summary>
    /// <value>The mode used for the center column.</value>
    /// <remarks>The default is <see cref="AxisStretchModeEnum.Stretch"/>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="AxisStretchModeEnum"/>
    public AxisStretchModeEnum AxisStretchHorizontal { get; set; }

    /// <summary>
    /// Gets or sets vertical center stretch behavior.
    /// </summary>
    /// <value>The mode used for the center row.</value>
    /// <remarks>The default is <see cref="AxisStretchModeEnum.Stretch"/>.</remarks>
    /// <threadsafety>This property is not synchronized. Mutate it on the main scene thread.</threadsafety>
    /// <since>This property is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="AxisStretchModeEnum"/>
    public AxisStretchModeEnum AxisStretchVertical { get; set; }

    /// <summary>
    /// Draws the nine-patch texture segments.
    /// </summary>
    /// <remarks>No draw command is submitted when <see cref="Texture"/> is <c>null</c>.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="Texture"/>
    public override void _Draw()
    {
        if (Texture is null || Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        var source = GetSourceRect(Texture);
        if (source.Size.X <= 0f || source.Size.Y <= 0f)
        {
            return;
        }

        DrawNinePatch(Texture, source);
    }

    /// <summary>
    /// Gets the minimum size requested by this nine-patch rectangle.
    /// </summary>
    /// <returns>The sum of horizontal and vertical patch margins.</returns>
    /// <remarks>The value is independent of <see cref="Texture"/>.</remarks>
    /// <threadsafety>This callback is invoked on the main scene thread.</threadsafety>
    /// <since>This method is available since Electron2D 0.1-preview.</since>
    /// <seealso cref="PatchMarginLeft"/>
    public override Vector2 _GetMinimumSize()
    {
        return new Vector2(PatchMarginLeft + PatchMarginRight, PatchMarginTop + PatchMarginBottom);
    }

    private void DrawNinePatch(Texture2D texture, Rect2 source)
    {
        var sourceMargins = NormalizeMargins(source.Size, PatchMarginLeft, PatchMarginTop, PatchMarginRight, PatchMarginBottom);
        var destinationMargins = NormalizeMargins(Size, PatchMarginLeft, PatchMarginTop, PatchMarginRight, PatchMarginBottom);
        var sourceX = SplitFixedAxis(source.Position.X, source.Size.X, sourceMargins.Left, sourceMargins.Right);
        var sourceY = SplitFixedAxis(source.Position.Y, source.Size.Y, sourceMargins.Top, sourceMargins.Bottom);
        var destinationX = SplitFixedAxis(0f, Size.X, destinationMargins.Left, destinationMargins.Right);
        var destinationY = SplitFixedAxis(0f, Size.Y, destinationMargins.Top, destinationMargins.Bottom);

        for (var y = 0; y < 3; y++)
        {
            for (var x = 0; x < 3; x++)
            {
                if (!DrawCenter && x == 1 && y == 1)
                {
                    continue;
                }

                EmitSegment(
                    texture,
                    new AxisSpan(sourceX[x].Start, sourceX[x].Length),
                    new AxisSpan(sourceY[y].Start, sourceY[y].Length),
                    new AxisSpan(destinationX[x].Start, destinationX[x].Length),
                    new AxisSpan(destinationY[y].Start, destinationY[y].Length),
                    x == 1 ? AxisStretchHorizontal : AxisStretchModeEnum.Stretch,
                    y == 1 ? AxisStretchVertical : AxisStretchModeEnum.Stretch);
            }
        }
    }

    private void EmitSegment(
        Texture2D texture,
        AxisSpan sourceX,
        AxisSpan sourceY,
        AxisSpan destinationX,
        AxisSpan destinationY,
        AxisStretchModeEnum horizontalMode,
        AxisStretchModeEnum verticalMode)
    {
        if (sourceX.Length <= 0f || sourceY.Length <= 0f || destinationX.Length <= 0f || destinationY.Length <= 0f)
        {
            return;
        }

        foreach (var x in SplitTiledAxis(sourceX, destinationX, horizontalMode))
        {
            foreach (var y in SplitTiledAxis(sourceY, destinationY, verticalMode))
            {
                DrawTextureRect(
                    texture,
                    new Rect2(x.Destination.Start, y.Destination.Start, x.Destination.Length, y.Destination.Length),
                    new Rect2(x.Source.Start, y.Source.Start, x.Source.Length, y.Source.Length));
            }
        }
    }

    private Rect2 GetSourceRect(Texture2D texture)
    {
        return RegionRect.Size.X > 0f && RegionRect.Size.Y > 0f
            ? RegionRect
            : new Rect2(0f, 0f, texture.GetWidth(), texture.GetHeight());
    }

    private int SetMargin(int value)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        QueueRedraw();
        return value;
    }

    private static (float Left, float Top, float Right, float Bottom) NormalizeMargins(
        Vector2 size,
        int left,
        int top,
        int right,
        int bottom)
    {
        var horizontal = NormalizePair(left, right, size.X);
        var vertical = NormalizePair(top, bottom, size.Y);
        return (horizontal.Start, vertical.Start, horizontal.End, vertical.End);
    }

    private static (float Start, float End) NormalizePair(int start, int end, float size)
    {
        var startValue = MathF.Max(0f, start);
        var endValue = MathF.Max(0f, end);
        var sum = startValue + endValue;
        if (sum <= size || sum <= 0f)
        {
            return (startValue, endValue);
        }

        var scale = size / sum;
        return (startValue * scale, endValue * scale);
    }

    private static AxisSpan[] SplitFixedAxis(float origin, float size, float leading, float trailing)
    {
        var center = MathF.Max(0f, size - leading - trailing);
        return new[]
        {
            new AxisSpan(origin, leading),
            new AxisSpan(origin + leading, center),
            new AxisSpan(origin + leading + center, trailing)
        };
    }

    private static IEnumerable<TiledAxisSpan> SplitTiledAxis(AxisSpan source, AxisSpan destination, AxisStretchModeEnum mode)
    {
        if (mode == AxisStretchModeEnum.Stretch)
        {
            yield return new TiledAxisSpan(source, destination);
            yield break;
        }

        var sourceLength = source.Length;
        if (sourceLength <= 0f)
        {
            yield break;
        }

        if (mode == AxisStretchModeEnum.TileFit)
        {
            var count = Math.Max(1, (int)MathF.Round(destination.Length / sourceLength, MidpointRounding.AwayFromZero));
            var fittedLength = destination.Length / count;
            for (var i = 0; i < count; i++)
            {
                yield return new TiledAxisSpan(source, new AxisSpan(destination.Start + (fittedLength * i), fittedLength));
            }

            yield break;
        }

        var offset = 0f;
        while (offset < destination.Length)
        {
            var segmentLength = MathF.Min(sourceLength, destination.Length - offset);
            yield return new TiledAxisSpan(
                new AxisSpan(source.Start, segmentLength),
                new AxisSpan(destination.Start + offset, segmentLength));
            offset += segmentLength;
        }
    }

    private static void ValidateRect(Rect2 rect, string parameterName)
    {
        if (!rect.Position.IsFinite() || !rect.Size.IsFinite() || rect.Size.X < 0f || rect.Size.Y < 0f)
        {
            throw new ArgumentOutOfRangeException(parameterName, rect, "Rectangle values must be finite and non-negative.");
        }
    }

    private readonly record struct AxisSpan(float Start, float Length);

    private readonly record struct TiledAxisSpan(AxisSpan Source, AxisSpan Destination);
}
