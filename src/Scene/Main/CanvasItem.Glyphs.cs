namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Records a texture region whose RGB channels encode independent subpixel coverage.</summary>
    /// <param name="texture">The borrowed coverage texture.</param><param name="rect">Finite destination bounds; signed dimensions flip around the origin.</param>
    /// <param name="sourceRect">Finite logical texture-pixel region; signed dimensions toggle flipping.</param><param name="modulate">Finite command tint, or null for white.</param>
    /// <remarks>Uses the texture identity and explicit source bounds. LCD sampling requires a supported native component-blend backend.</remarks>
    /// <exception cref="ArgumentException">Geometry or modulation is nonfinite.</exception><exception cref="InvalidOperationException">Recording or owner rules reject the call.</exception>
    /// <exception cref="ObjectDisposedException">The node or borrowed texture is disposed.</exception><exception cref="ArgumentNullException">The texture is null.</exception>
    public void DrawLCDTextureRectRegion(Texture texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null) => RecordGlyph(texture, rect, sourceRect, modulate ?? Colors.White, new(CanvasGlyphMode.LCD));
    /// <summary>Records an RGB signed-distance texture region with derivative-scaled coverage and an optional outline.</summary>
    /// <param name="texture">The borrowed encoded distance texture.</param><param name="rect">Finite destination bounds.</param><param name="sourceRect">Finite logical texture-pixel region.</param>
    /// <param name="modulate">Finite tint, or null for white.</param><param name="outline">Finite source outline size; nonpositive values select ordinary coverage.</param>
    /// <param name="pixelRange">Positive finite encoded distance range.</param><param name="scale">Positive finite source scale.</param>
    /// <remarks>Consumes encoded RGB distances and optional alpha distance data; it does not generate glyph images.
    /// Outline is truncated to an integer before scale conversion. Uses the backing texture RID and explicit source bounds without a virtual texture drawing hook.</remarks>
    /// <exception cref="ArgumentException">Geometry, tint or resulting parameters are nonfinite.</exception><exception cref="ArgumentOutOfRangeException">Range or scale is not positive and finite.</exception>
    /// <exception cref="InvalidOperationException">Recording or owner rules reject the call.</exception><exception cref="ObjectDisposedException">The node or texture is disposed.</exception>
    /// <exception cref="ArgumentNullException">The texture is null.</exception>
    public void DrawMSDFTextureRectRegion(Texture texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, float outline = 0, float pixelRange = 4, float scale = 1) => RecordGlyph(texture, rect, sourceRect, modulate ?? Colors.White, new(CanvasGlyphMode.MSDF, MathF.Truncate(outline), pixelRange, scale));
    private void RecordGlyph(Texture texture, Rect2 rect, Rect2 source, Color color, CanvasGlyph glyph)
    {
        ValidateTextureDraw(texture, rect, color); if (!source.IsFinite()) throw new ArgumentException("Glyph source bounds must be finite.", nameof(source)); glyph.Validate();
        var image = RenderingTextureRegistry.Resolve(texture.GetRID());
        RecordTexture(image, rect, source, color, tile: false, transpose: false, clipUV: false, glyph: glyph);
    }
    internal void AppendServerGlyph(Texture texture, Rect2 rect, Rect2 source, Color color, CanvasGlyph glyph)
    {
        var previous = _currentDrawingItem; var drawing = _drawing; _currentDrawingItem = this; _drawing = true;
        try { RecordGlyph(texture, rect, source, color, glyph); }
        finally { _drawing = drawing; _currentDrawingItem = previous; }
    }
}
