namespace Electron2D;

public partial class TileMapLayer
{
    private readonly record struct TerrainKey(Vector2i Coords, int Bit) : IComparable<TerrainKey>
    {
        public int CompareTo(TerrainKey other) { var x = Coords.X.CompareTo(other.Coords.X); if (x != 0) return x; var y = Coords.Y.CompareTo(other.Coords.Y); return y != 0 ? y : Bit.CompareTo(other.Bit); }
    }
    private static TerrainKey PeeringKey(TileSet set, Vector2i coords, CellNeighbor bit)
    {
        (int Key, CellNeighbor Move) value;
        if (set.TileShape == TileShape.Square)
            value = bit switch
            {
                CellNeighbor.RightSide => (1, (CellNeighbor)(-1)),
                CellNeighbor.BottomRightCorner => (2, (CellNeighbor)(-1)),
                CellNeighbor.BottomSide => (3, (CellNeighbor)(-1)),
                CellNeighbor.BottomLeftCorner => (2, CellNeighbor.LeftSide),
                CellNeighbor.LeftSide => (1, CellNeighbor.LeftSide),
                CellNeighbor.TopLeftCorner => (2, CellNeighbor.TopLeftCorner),
                CellNeighbor.TopSide => (3, CellNeighbor.TopSide),
                CellNeighbor.TopRightCorner => (2, CellNeighbor.TopSide),
                _ => throw new ArgumentOutOfRangeException(nameof(bit))
            };
        else if (set.TileShape == TileShape.Isometric)
            value = bit switch
            {
                CellNeighbor.RightCorner => (2, CellNeighbor.TopRightSide),
                CellNeighbor.BottomRightSide => (1, (CellNeighbor)(-1)),
                CellNeighbor.BottomCorner => (2, (CellNeighbor)(-1)),
                CellNeighbor.BottomLeftSide => (3, (CellNeighbor)(-1)),
                CellNeighbor.LeftCorner => (2, CellNeighbor.TopLeftSide),
                CellNeighbor.TopLeftSide => (1, CellNeighbor.TopLeftSide),
                CellNeighbor.TopCorner => (2, CellNeighbor.TopCorner),
                CellNeighbor.TopRightSide => (3, CellNeighbor.TopRightSide),
                _ => throw new ArgumentOutOfRangeException(nameof(bit))
            };
        else if (set.TileOffsetAxis == TileOffsetAxis.Horizontal)
            value = bit switch
            {
                CellNeighbor.RightSide => (1, (CellNeighbor)(-1)),
                CellNeighbor.BottomRightCorner => (2, (CellNeighbor)(-1)),
                CellNeighbor.BottomRightSide => (3, (CellNeighbor)(-1)),
                CellNeighbor.BottomCorner => (4, (CellNeighbor)(-1)),
                CellNeighbor.BottomLeftSide => (5, (CellNeighbor)(-1)),
                CellNeighbor.BottomLeftCorner => (2, CellNeighbor.LeftSide),
                CellNeighbor.LeftSide => (1, CellNeighbor.LeftSide),
                CellNeighbor.TopLeftCorner => (4, CellNeighbor.TopLeftSide),
                CellNeighbor.TopLeftSide => (3, CellNeighbor.TopLeftSide),
                CellNeighbor.TopCorner => (2, CellNeighbor.TopLeftSide),
                CellNeighbor.TopRightSide => (5, CellNeighbor.TopRightSide),
                CellNeighbor.TopRightCorner => (4, CellNeighbor.TopRightSide),
                _ => throw new ArgumentOutOfRangeException(nameof(bit))
            };
        else
            value = bit switch
            {
                CellNeighbor.RightCorner => (1, (CellNeighbor)(-1)),
                CellNeighbor.BottomRightSide => (2, (CellNeighbor)(-1)),
                CellNeighbor.BottomRightCorner => (3, (CellNeighbor)(-1)),
                CellNeighbor.BottomSide => (4, (CellNeighbor)(-1)),
                CellNeighbor.BottomLeftCorner => (1, CellNeighbor.BottomLeftSide),
                CellNeighbor.BottomLeftSide => (5, (CellNeighbor)(-1)),
                CellNeighbor.LeftCorner => (3, CellNeighbor.TopLeftSide),
                CellNeighbor.TopLeftSide => (2, CellNeighbor.TopLeftSide),
                CellNeighbor.TopLeftCorner => (1, CellNeighbor.TopLeftSide),
                CellNeighbor.TopSide => (4, CellNeighbor.TopSide),
                CellNeighbor.TopRightCorner => (3, CellNeighbor.TopSide),
                CellNeighbor.TopRightSide => (5, CellNeighbor.TopRightSide),
                _ => throw new ArgumentOutOfRangeException(nameof(bit))
            };
        return new((int)value.Move < 0 ? coords : set.GetNeighborCell(coords, value.Move), value.Key);
    }
    private static (Vector2i Coords, CellNeighbor Bit)[] OverlappingBits(TileSet set, TerrainKey key)
    {
        if (set.TileShape == TileShape.Square) return key.Bit switch
        {
            1 => [(key.Coords, CellNeighbor.RightSide), (set.GetNeighborCell(key.Coords, CellNeighbor.RightSide), CellNeighbor.LeftSide)],
            2 => [(key.Coords, CellNeighbor.BottomRightCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.RightSide), CellNeighbor.BottomLeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightCorner), CellNeighbor.TopLeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomSide), CellNeighbor.TopRightCorner)],
            3 => [(key.Coords, CellNeighbor.BottomSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomSide), CellNeighbor.TopSide)],
            _ => throw new InvalidOperationException("Invalid terrain constraint.")
        };
        if (set.TileShape == TileShape.Isometric) return key.Bit switch
        {
            1 => [(key.Coords, CellNeighbor.BottomRightSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopLeftSide)],
            2 => [(key.Coords, CellNeighbor.BottomCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.LeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomCorner), CellNeighbor.TopCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomLeftSide), CellNeighbor.RightCorner)],
            3 => [(key.Coords, CellNeighbor.BottomLeftSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomLeftSide), CellNeighbor.TopRightSide)],
            _ => throw new InvalidOperationException("Invalid terrain constraint.")
        };
        if (set.TileOffsetAxis == TileOffsetAxis.Horizontal) return key.Bit switch
        {
            1 => [(key.Coords, CellNeighbor.RightSide), (set.GetNeighborCell(key.Coords, CellNeighbor.RightSide), CellNeighbor.LeftSide)],
            2 => [(key.Coords, CellNeighbor.BottomRightCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.RightSide), CellNeighbor.BottomLeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopCorner)],
            3 => [(key.Coords, CellNeighbor.BottomRightSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopLeftSide)],
            4 => [(key.Coords, CellNeighbor.BottomCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopLeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomLeftSide), CellNeighbor.TopRightCorner)],
            5 => [(key.Coords, CellNeighbor.BottomLeftSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomLeftSide), CellNeighbor.TopRightSide)],
            _ => throw new InvalidOperationException("Invalid terrain constraint.")
        };
        return key.Bit switch
        {
            1 => [(key.Coords, CellNeighbor.RightCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.TopRightSide), CellNeighbor.BottomLeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopLeftCorner)],
            2 => [(key.Coords, CellNeighbor.BottomRightSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.TopLeftSide)],
            3 => [(key.Coords, CellNeighbor.BottomRightCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomRightSide), CellNeighbor.LeftCorner), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomSide), CellNeighbor.TopLeftCorner)],
            4 => [(key.Coords, CellNeighbor.BottomSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomSide), CellNeighbor.TopSide)],
            5 => [(key.Coords, CellNeighbor.BottomLeftSide), (set.GetNeighborCell(key.Coords, CellNeighbor.BottomLeftSide), CellNeighbor.TopRightSide)],
            _ => throw new InvalidOperationException("Invalid terrain constraint.")
        };
    }
}
