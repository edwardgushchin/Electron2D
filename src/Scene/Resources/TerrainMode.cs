namespace Electron2D;

/// <summary>Selects which parts of neighboring tiles must match when painting terrain.</summary>
public enum TerrainMode
{
    /// <summary>Matches both corners and sides.</summary>
    MatchCornersAndSides = 0,
    /// <summary>Matches corners only.</summary>
    MatchCorners = 1,
    /// <summary>Matches sides only.</summary>
    MatchSides = 2,
}
