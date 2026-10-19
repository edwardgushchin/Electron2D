namespace Electron2D;

/// <summary>Controls explicit shaped-buffer trimming and ellipsis insertion.</summary>
[Flags]
public enum TextOverrunFlags
{
    /// <summary>Removes any active trim without cutting the original glyphs.</summary>
    NoTrim = 0,
    /// <summary>Trims when the available width is exceeded.</summary>
    Trim = 1,
    /// <summary>Prefers word opportunities, retaining cluster fallback for fewer than six visible records.</summary>
    TrimWordOnly = 2,
    /// <summary>Adds an ellipsis when permitted by the remaining length.</summary>
    AddEllipsis = 4,
    /// <summary>Forces an ellipsis even when the original string fits.</summary>
    EnforceEllipsis = 8,
    /// <summary>Trims only after a fit operation cannot meet the requested width.</summary>
    JustificationAware = 16,
    /// <summary>Allows an ellipsis for fewer than six retained glyph records.</summary>
    ShortStringEllipsis = 32
}
