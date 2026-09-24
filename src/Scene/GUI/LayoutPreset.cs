namespace Electron2D;

/// <summary>Identifies a standard arrangement of a control's four anchors.</summary>
public enum LayoutPreset
{
    /// <summary>All anchors at the upper-left corner.</summary>
    TopLeft = 0,
    /// <summary>All anchors at the upper-right corner.</summary>
    TopRight = 1,
    /// <summary>All anchors at the lower-left corner.</summary>
    BottomLeft = 2,
    /// <summary>All anchors at the lower-right corner.</summary>
    BottomRight = 3,
    /// <summary>All anchors at the center of the left edge.</summary>
    CenterLeft = 4,
    /// <summary>All anchors at the center of the upper edge.</summary>
    CenterTop = 5,
    /// <summary>All anchors at the center of the right edge.</summary>
    CenterRight = 6,
    /// <summary>All anchors at the center of the lower edge.</summary>
    CenterBottom = 7,
    /// <summary>All anchors at the center of the parent area.</summary>
    Center = 8,
    /// <summary>Anchors span the height of the left edge.</summary>
    LeftWide = 9,
    /// <summary>Anchors span the width of the upper edge.</summary>
    TopWide = 10,
    /// <summary>Anchors span the height of the right edge.</summary>
    RightWide = 11,
    /// <summary>Anchors span the width of the lower edge.</summary>
    BottomWide = 12,
    /// <summary>Anchors span the height of the vertical center.</summary>
    VCenterWide = 13,
    /// <summary>Anchors span the width of the horizontal center.</summary>
    HCenterWide = 14,
    /// <summary>Anchors follow all four edges of the parent area.</summary>
    FullRect = 15
}
