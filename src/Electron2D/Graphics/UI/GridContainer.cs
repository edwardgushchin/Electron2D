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
/// Arranges direct child controls into rows and columns.
/// </summary>
///
/// <remarks>
/// <para>
/// <c>GridContainer</c> measures visible direct child controls in row-major
/// order. Each column takes the largest minimum width found in that column, and
/// each row takes the largest minimum height found in that row.
/// </para>
/// </remarks>
///
/// <threadsafety>
/// This type is not synchronized. Create and mutate it on the main scene
/// thread.
/// </threadsafety>
///
/// <since>
/// This type is available since Electron2D 0.1.0 Preview.
/// </since>
///
/// <seealso cref="Container"/>
public class GridContainer : Container
{
    private const int DefaultSeparation = 4;
    private int columns = 1;

    /// <summary>
    /// Initializes a new instance of the <see cref="GridContainer"/> type.
    /// </summary>
    ///
    /// <remarks>
    /// The new grid starts with one column.
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
    /// <seealso cref="Columns"/>
    public GridContainer()
    {
    }

    /// <summary>
    /// Gets or sets the number of columns used by this grid.
    /// </summary>
    ///
    /// <value>
    /// The positive column count. The default is <c>1</c>.
    /// </value>
    ///
    /// <remarks>
    /// <para>
    /// Children are assigned to cells in row-major order.
    /// </para>
    /// </remarks>
    ///
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown when the value is less than or equal to zero.
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
    /// <seealso cref="Container.QueueSort"/>
    public int Columns
    {
        get => columns;
        set
        {
            if (value <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value), value, "Column count must be greater than zero.");
            }

            columns = value;
            QueueSort();
        }
    }

    /// <summary>
    /// Gets the minimum size required by this grid container.
    /// </summary>
    ///
    /// <returns>
    /// The minimum size produced by row and column measurements.
    /// </returns>
    ///
    /// <remarks>
    /// <para>
    /// Horizontal and vertical separation are read from <c>h_separation</c> and
    /// <c>v_separation</c> theme constants.
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
    /// <seealso cref="Columns"/>
    public override Vector2 _GetMinimumSize()
    {
        var metrics = MeasureGrid();
        if (metrics.Count == 0)
        {
            return Vector2.Zero;
        }

        var width = metrics.ColumnWidths.Sum() + (metrics.HorizontalSeparation * Math.Max(0, metrics.ColumnWidths.Length - 1));
        var height = metrics.RowHeights.Sum() + (metrics.VerticalSeparation * Math.Max(0, metrics.RowHeights.Length - 1));
        return new Vector2(width, height);
    }

    protected override void SortChildren()
    {
        var metrics = MeasureGrid();
        if (metrics.Count == 0)
        {
            return;
        }

        var x = 0f;
        for (var column = 0; column < metrics.ColumnWidths.Length; column++)
        {
            var y = 0f;
            for (var row = 0; row < metrics.RowHeights.Length; row++)
            {
                var index = (row * metrics.ColumnWidths.Length) + column;
                if (index < metrics.Children.Length)
                {
                    FitChildInRect(
                        metrics.Children[index],
                        new Rect2(x, y, metrics.ColumnWidths[column], metrics.RowHeights[row]));
                }

                y += metrics.RowHeights[row] + metrics.VerticalSeparation;
            }

            x += metrics.ColumnWidths[column] + metrics.HorizontalSeparation;
        }
    }

    private GridMetrics MeasureGrid()
    {
        var children = GetLayoutChildren();
        if (children.Length == 0)
        {
            return new GridMetrics(children, Array.Empty<float>(), Array.Empty<float>(), 0, 0);
        }

        var columnCount = Math.Min(Columns, children.Length);
        var rowCount = (int)MathF.Ceiling(children.Length / (float)columnCount);
        var widths = new float[columnCount];
        var heights = new float[rowCount];
        for (var index = 0; index < children.Length; index++)
        {
            var minimum = children[index].GetCombinedMinimumSize();
            var row = index / columnCount;
            var column = index % columnCount;
            widths[column] = MathF.Max(widths[column], minimum.X);
            heights[row] = MathF.Max(heights[row], minimum.Y);
        }

        return new GridMetrics(
            children,
            widths,
            heights,
            GetThemeConstantOrDefault("h_separation", DefaultSeparation),
            GetThemeConstantOrDefault("v_separation", DefaultSeparation));
    }

    private sealed record GridMetrics(
        Control[] Children,
        float[] ColumnWidths,
        float[] RowHeights,
        int HorizontalSeparation,
        int VerticalSeparation)
    {
        public int Count => Children.Length;
    }
}
