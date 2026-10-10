namespace Electron2D;

public sealed partial class TileSet
{
    private TileShape _tileShape;
    private TileLayout _tileLayout;
    private TileOffsetAxis _tileOffsetAxis;
    /// <summary>Gets or sets the cell outline; square is the default.</summary>
    /// <remarks>Changing shape changes cell placement, not authored images or polygons.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The shape is undefined.</exception>
    public TileShape TileShape
    {
        get { ThrowIfDisposed(); return _tileShape; }
        set { ThrowIfDisposed(); if ((uint)value > 3) throw new ArgumentOutOfRangeException(nameof(value)); if (_tileShape == value) return; ChangeTerrainDefinitions(() => _tileShape = value, data => data.TerrainSchemaChanged(false)); }
    }
    /// <summary>Gets or sets the coordinate layout; stacked is the default and square cells ignore it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The layout is undefined.</exception>
    public TileLayout TileLayout
    {
        get { ThrowIfDisposed(); return _tileLayout; }
        set { ThrowIfDisposed(); if ((uint)value > 5) throw new ArgumentOutOfRangeException(nameof(value)); if (_tileLayout == value) return; _tileLayout = value; EmitChanged(); }
    }
    /// <summary>Gets or sets the offset axis; horizontal is the default and square cells ignore it.</summary>
    /// <exception cref="ArgumentOutOfRangeException">The axis is undefined.</exception>
    public TileOffsetAxis TileOffsetAxis
    {
        get { ThrowIfDisposed(); return _tileOffsetAxis; }
        set { ThrowIfDisposed(); if ((uint)value > 1) throw new ArgumentOutOfRangeException(nameof(value)); if (_tileOffsetAxis == value) return; ChangeTerrainDefinitions(() => _tileOffsetAxis = value, data => data.TerrainSchemaChanged(false)); }
    }
    private float Overlap => _tileShape == TileShape.Isometric ? .5f : _tileShape == TileShape.Hexagon ? .75f : 1;
    private IEnumerable<PropertyDescriptor> GeometryProperties()
    {
        yield return new PropertyDescriptor<TileSet, TileShape>(nameof(TileShape), n => n.TileShape, (n, v) => n.TileShape = v, _ => TileShape.Square, stored: true);
        yield return new PropertyDescriptor<TileSet, TileLayout>(nameof(TileLayout), n => n.TileLayout, (n, v) => n.TileLayout = v, _ => TileLayout.Stacked, stored: true);
        yield return new PropertyDescriptor<TileSet, TileOffsetAxis>(nameof(TileOffsetAxis), n => n.TileOffsetAxis, (n, v) => n.TileOffsetAxis = v, _ => TileOffsetAxis.Horizontal, stored: true);
    }
    internal Vector2 MapToLocal(Vector2i coords)
    {
        ThrowIfDisposed(); float x = coords.X, y = coords.Y;
        if (_tileShape != TileShape.Square)
        {
            if (_tileOffsetAxis == TileOffsetAxis.Horizontal)
            {
                (x, y) = _tileLayout switch
                {
                    TileLayout.Stacked => (x + (coords.Y & 1) * .5f, y),
                    TileLayout.StackedOffset => (x + (1 - (coords.Y & 1)) * .5f, y),
                    TileLayout.StairsRight => (x + y / 2, y),
                    TileLayout.StairsDown => (x / 2, y * 2 + x),
                    TileLayout.DiamondRight => ((x + y) / 2, y - x),
                    _ => ((x - y) / 2, y + x)
                };
                y *= Overlap;
            }
            else
            {
                (x, y) = _tileLayout switch
                {
                    TileLayout.Stacked => (x, y + (coords.X & 1) * .5f),
                    TileLayout.StackedOffset => (x, y + (1 - (coords.X & 1)) * .5f),
                    TileLayout.StairsRight => (x * 2 + y, y / 2),
                    TileLayout.StairsDown => (x, y + x / 2),
                    TileLayout.DiamondRight => (x + y, (y - x) / 2),
                    _ => (x - y, (y + x) / 2)
                };
                x *= Overlap;
            }
        }
        return new((x + .5f) * _tileSize.X, (y + .5f) * _tileSize.Y);
    }
    internal Vector2i LocalToMap(Vector2 position)
    {
        ThrowIfDisposed(); if (!position.IsFinite()) throw new ArgumentOutOfRangeException(nameof(position));
        float x = position.X / _tileSize.X, y = position.Y / _tileSize.Y;
        if (_tileShape == TileShape.Square) return Coordinates(MathF.Floor(x + .00005f), MathF.Floor(y + .00005f));
        var ratio = Overlap;
        // Round the double-precision slope once to preserve sloping-edge ties.
        var slope = (float)(1d / ratio - 1);
        bool first, second;
        if (_tileOffsetAxis == TileOffsetAxis.Horizontal)
        {
            y /= ratio; var floorY = MathF.Floor(y);
            var offset = (floorY % 2 != 0) ^ (_tileLayout == TileLayout.StackedOffset);
            var floorX = offset ? (float)(Math.Floor((double)x + .5) - .5) : MathF.Floor(x);
            var inX = x - floorX - .5f; var inY = y - floorY;
            first = inX * slope + inY * .5f <= 0;
            second = inX * slope - inY * .5f > 0;
            (x, y) = _tileLayout switch
            {
                TileLayout.Stacked or TileLayout.StackedOffset => (MathF.Floor(floorX), floorY),
                TileLayout.StairsRight => (MathF.Floor(floorX - floorY / 2), floorY),
                TileLayout.StairsDown => (MathF.Floor(floorX * 2), MathF.Floor(floorY / 2 - floorX)),
                TileLayout.DiamondRight => (MathF.Floor(floorX - floorY / 2), MathF.Floor(floorY / 2 + floorX)),
                _ => (MathF.Floor(floorX + floorY / 2), MathF.Floor(floorY / 2 - floorX))
            };
            if (first || second)
            {
                var odd = floorY % 2 != 0;
                (float dx, float dy) = _tileLayout switch
                {
                    TileLayout.Stacked => (first ? (odd ? 0 : -1) : (odd ? 1 : 0), -1),
                    TileLayout.StackedOffset => (first ? (odd ? -1 : 0) : (odd ? 0 : 1), -1),
                    TileLayout.StairsRight => (first ? 0 : 1, -1),
                    TileLayout.StairsDown => (first ? -1 : 1, first ? 0 : -1),
                    TileLayout.DiamondRight => (first ? 0 : 1, first ? -1 : 0),
                    _ => (first ? -1 : 0, first ? 0 : -1)
                };
                x += dx; y += dy;
            }
        }
        else
        {
            x /= ratio; var floorX = MathF.Floor(x);
            var offset = (floorX % 2 != 0) ^ (_tileLayout == TileLayout.StackedOffset);
            var floorY = offset ? (float)(Math.Floor((double)y + .5) - .5) : MathF.Floor(y);
            var inX = x - floorX; var inY = y - floorY - .5f;
            first = inX * -.5f - inY * slope > 0;
            second = inX * .5f - inY * slope <= 0;
            (x, y) = _tileLayout switch
            {
                TileLayout.Stacked or TileLayout.StackedOffset => (floorX, MathF.Floor(floorY)),
                TileLayout.StairsRight => (MathF.Floor(floorX / 2 - floorY), MathF.Floor(floorY * 2)),
                TileLayout.StairsDown => (floorX, MathF.Floor(floorY - floorX / 2)),
                TileLayout.DiamondRight => (MathF.Floor(floorX / 2 - floorY), MathF.Floor(floorY + floorX / 2)),
                _ => (MathF.Floor(floorX / 2 + floorY), MathF.Floor(floorY - floorX / 2))
            };
            if (first || second)
            {
                var odd = floorX % 2 != 0;
                (float dx, float dy) = _tileLayout switch
                {
                    TileLayout.Stacked => (-1, first ? (odd ? 0 : -1) : (odd ? 1 : 0)),
                    TileLayout.StackedOffset => (-1, first ? (odd ? -1 : 0) : (odd ? 0 : 1)),
                    TileLayout.StairsRight => (first ? 0 : -1, first ? -1 : 1),
                    TileLayout.StairsDown => (-1, first ? 0 : 1),
                    TileLayout.DiamondRight => (first ? 0 : -1, first ? -1 : 0),
                    _ => (first ? -1 : 0, first ? 0 : 1)
                };
                x += dx; y += dy;
            }
        }
        return Coordinates(x, y);
    }
    private static Vector2i Coordinates(double x, double y) => new(checked((int)x), checked((int)y));
    internal Vector2i GetNeighborCell(Vector2i coords, CellNeighbor neighbor)
    {
        ThrowIfDisposed();
        if (_tileShape == TileShape.Square)
        {
            (int x, int y) = neighbor switch
            {
                CellNeighbor.RightSide => (1, 0),
                CellNeighbor.BottomRightCorner => (1, 1),
                CellNeighbor.BottomSide => (0, 1),
                CellNeighbor.BottomLeftCorner => (-1, 1),
                CellNeighbor.LeftSide => (-1, 0),
                CellNeighbor.TopLeftCorner => (-1, -1),
                CellNeighbor.TopSide => (0, -1),
                CellNeighbor.TopRightCorner => (1, -1),
                _ => throw new ArgumentOutOfRangeException(nameof(neighbor))
            };
            return new(checked(coords.X + x), checked(coords.Y + y));
        }
        var (sx, sy) = ToStacked(coords); var odd = (_tileOffsetAxis == TileOffsetAxis.Horizontal ? sy : sx) & 1;
        var iso = _tileShape == TileShape.Isometric;
        (long X, long Y) delta = (_tileOffsetAxis, neighbor) switch
        {
            (TileOffsetAxis.Horizontal, CellNeighbor.RightCorner) when iso => (1, 0),
            (TileOffsetAxis.Horizontal, CellNeighbor.RightSide) when !iso => (1, 0),
            (TileOffsetAxis.Horizontal, CellNeighbor.BottomRightSide) => (odd, 1),
            (TileOffsetAxis.Horizontal, CellNeighbor.BottomCorner) when iso => (0, 2),
            (TileOffsetAxis.Horizontal, CellNeighbor.BottomLeftSide) => (odd - 1, 1),
            (TileOffsetAxis.Horizontal, CellNeighbor.LeftCorner) when iso => (-1, 0),
            (TileOffsetAxis.Horizontal, CellNeighbor.LeftSide) when !iso => (-1, 0),
            (TileOffsetAxis.Horizontal, CellNeighbor.TopLeftSide) => (odd - 1, -1),
            (TileOffsetAxis.Horizontal, CellNeighbor.TopCorner) when iso => (0, -2),
            (TileOffsetAxis.Horizontal, CellNeighbor.TopRightSide) => (odd, -1),
            (TileOffsetAxis.Vertical, CellNeighbor.BottomCorner) when iso => (0, 1),
            (TileOffsetAxis.Vertical, CellNeighbor.BottomSide) when !iso => (0, 1),
            (TileOffsetAxis.Vertical, CellNeighbor.BottomRightSide) => (1, odd),
            (TileOffsetAxis.Vertical, CellNeighbor.RightCorner) when iso => (2, 0),
            (TileOffsetAxis.Vertical, CellNeighbor.TopRightSide) => (1, odd - 1),
            (TileOffsetAxis.Vertical, CellNeighbor.TopCorner) when iso => (0, -1),
            (TileOffsetAxis.Vertical, CellNeighbor.TopSide) when !iso => (0, -1),
            (TileOffsetAxis.Vertical, CellNeighbor.TopLeftSide) => (-1, odd - 1),
            (TileOffsetAxis.Vertical, CellNeighbor.LeftCorner) when iso => (-2, 0),
            (TileOffsetAxis.Vertical, CellNeighbor.BottomLeftSide) => (-1, odd),
            _ => throw new ArgumentOutOfRangeException(nameof(neighbor))
        };
        var (rx, ry) = FromStacked(sx + delta.X, sy + delta.Y); return new(checked((int)rx), checked((int)ry));
    }
    internal Vector2i[] GetSurroundingCells(Vector2i coords)
    {
        ThrowIfDisposed();
        ReadOnlySpan<CellNeighbor> directions = _tileShape switch
        {
            TileShape.Square => [CellNeighbor.RightSide, CellNeighbor.BottomSide, CellNeighbor.LeftSide, CellNeighbor.TopSide],
            TileShape.Isometric => [CellNeighbor.BottomRightSide, CellNeighbor.BottomLeftSide, CellNeighbor.TopLeftSide, CellNeighbor.TopRightSide],
            _ when _tileOffsetAxis == TileOffsetAxis.Horizontal => [CellNeighbor.RightSide, CellNeighbor.BottomRightSide, CellNeighbor.BottomLeftSide, CellNeighbor.LeftSide, CellNeighbor.TopLeftSide, CellNeighbor.TopRightSide],
            _ => [CellNeighbor.BottomRightSide, CellNeighbor.BottomSide, CellNeighbor.BottomLeftSide, CellNeighbor.TopLeftSide, CellNeighbor.TopSide, CellNeighbor.TopRightSide]
        };
        var result = new Vector2i[directions.Length]; for (var i = 0; i < result.Length; i++) result[i] = GetNeighborCell(coords, directions[i]); return result;
    }
    private (long X, long Y) ToStacked(Vector2i coords)
    {
        long x = coords.X, y = coords.Y; var vertical = _tileOffsetAxis == TileOffsetAxis.Vertical;
        if (vertical) (x, y) = (y, x);
        (x, y) = _tileLayout switch
        {
            TileLayout.Stacked => (x, y),
            TileLayout.StackedOffset => (x - (y & 1), y),
            TileLayout.StairsRight or TileLayout.StairsDown when (_tileLayout == TileLayout.StairsRight) ^ vertical => (x + (y >> 1), y),
            TileLayout.StairsRight or TileLayout.StairsDown => (x >> 1, x + y * 2),
            TileLayout.DiamondRight or TileLayout.DiamondDown when (_tileLayout == TileLayout.DiamondRight) ^ vertical => ((x + y) >> 1, y - x),
            _ => ((x - y) >> 1, x + y)
        };
        return vertical ? (y, x) : (x, y);
    }
    private (long X, long Y) FromStacked(long x, long y)
    {
        var vertical = _tileOffsetAxis == TileOffsetAxis.Vertical; if (vertical) (x, y) = (y, x);
        var half = y >> 1; var odd = y & 1;
        (x, y) = _tileLayout switch
        {
            TileLayout.Stacked => (x, y),
            TileLayout.StackedOffset => (x + odd, y),
            TileLayout.StairsRight or TileLayout.StairsDown when (_tileLayout == TileLayout.StairsRight) ^ vertical => (x - half, y),
            TileLayout.StairsRight or TileLayout.StairsDown => (x * 2 + odd, half - x),
            TileLayout.DiamondRight or TileLayout.DiamondDown when (_tileLayout == TileLayout.DiamondRight) ^ vertical => (x - half, x + half + odd),
            _ => (x + half + odd, half - x)
        };
        return vertical ? (y, x) : (x, y);
    }
}
