namespace Electron2D;

/// <summary>Selects the coordinate system for offset grids.</summary>
public enum TileLayout
{
    /// <summary>Alternating rows or columns shift by half a cell.</summary>
    Stacked = 0,
    /// <summary>Alternating offsets start on the opposite parity.</summary>
    StackedOffset = 1,
    /// <summary>Coordinates advance along a staircase toward the right.</summary>
    StairsRight = 2,
    /// <summary>Coordinates advance along a staircase downward.</summary>
    StairsDown = 3,
    /// <summary>Coordinates advance along diamond axes toward the right.</summary>
    DiamondRight = 4,
    /// <summary>Coordinates advance along diamond axes downward.</summary>
    DiamondDown = 5,
}
