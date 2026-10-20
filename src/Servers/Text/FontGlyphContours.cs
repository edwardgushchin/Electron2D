namespace Electron2D;

public abstract partial class TextServer
{
    /// <summary>Identifies an outline point's role in its Bézier contour.</summary>
    public enum ContourPointTag
    {
        /// <summary>A quadratic Bézier control point.</summary>
        OffConic = 0,
        /// <summary>A point on the contour.</summary>
        On = 1,
        /// <summary>A cubic Bézier control point.</summary>
        OffCubic = 2
    }
}

/// <summary>A copied outline point in logical pixels, with positive Y below the baseline.</summary>
/// <param name="Position">Baseline-relative point position.</param><param name="Tag">Bézier point role.</param>
public readonly record struct FontContourPoint(Vector2 Position, TextServer.ContourPointTag Tag);

/// <summary>A caller-owned snapshot of a glyph's outline.</summary>
/// <param name="Points">Independent ordered point and control-point array.</param>
/// <param name="Contours">Inclusive last point index of each contour.</param>
/// <param name="Orientation">Whether the outline uses right-side fill orientation before Y inversion.</param>
public readonly record struct FontGlyphContours(FontContourPoint[] Points, int[] Contours, bool Orientation);

/// <summary>A caller-owned snapshot of one nominal font size and outline cache.</summary>
/// <param name="SizePixels">Nominal pixel size and outline radius.</param><param name="Glyphs">Cached glyph record count.</param>
/// <param name="Textures">Atlas page slot count.</param><param name="TextureBytes">Twice the atlas base-level RGBA byte count, estimating CPU and renderer storage.</param>
/// <param name="RasterScales">Independent distinct realized oversampling factors; empty for authored-only records.</param>
public readonly record struct FontSizeCacheInfo(Vector2i SizePixels, int Glyphs, int Textures, long TextureBytes, float[] RasterScales);
