namespace Electron2D;

/// <summary>Selects the grid cell outline.</summary>
public enum TileShape
{
    /// <summary>Axis-aligned square cells.</summary>
    Square = 0,
    /// <summary>Diamond cells with half overlap.</summary>
    Isometric = 1,
    /// <summary>Rectangular cells in alternating offset rows or columns.</summary>
    HalfOffsetSquare = 2,
    /// <summary>Six-sided cells with one-quarter overlap.</summary>
    Hexagon = 3,
}
