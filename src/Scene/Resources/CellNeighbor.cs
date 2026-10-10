namespace Electron2D;

/// <summary>Names clockwise cell edge and corner directions; available directions depend on grid shape.</summary>
public enum CellNeighbor
{
    /// <summary>Right edge.</summary>
    RightSide = 0,
    /// <summary>Right corner.</summary>
    RightCorner = 1,
    /// <summary>Bottom right edge.</summary>
    BottomRightSide = 2,
    /// <summary>Bottom right corner.</summary>
    BottomRightCorner = 3,
    /// <summary>Bottom edge.</summary>
    BottomSide = 4,
    /// <summary>Bottom corner.</summary>
    BottomCorner = 5,
    /// <summary>Bottom left edge.</summary>
    BottomLeftSide = 6,
    /// <summary>Bottom left corner.</summary>
    BottomLeftCorner = 7,
    /// <summary>Left edge.</summary>
    LeftSide = 8,
    /// <summary>Left corner.</summary>
    LeftCorner = 9,
    /// <summary>Top left edge.</summary>
    TopLeftSide = 10,
    /// <summary>Top left corner.</summary>
    TopLeftCorner = 11,
    /// <summary>Top edge.</summary>
    TopSide = 12,
    /// <summary>Top corner.</summary>
    TopCorner = 13,
    /// <summary>Top right edge.</summary>
    TopRightSide = 14,
    /// <summary>Top right corner.</summary>
    TopRightCorner = 15,
}
