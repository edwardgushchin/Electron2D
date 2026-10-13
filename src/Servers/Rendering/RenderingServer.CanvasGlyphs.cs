namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Appends a retained RGB subpixel-coverage texture region to a live canvas item.</summary>
    /// <param name="item">Live scene or caller-owned item.</param><param name="rect">Finite signed destination bounds.</param><param name="texture">Live borrowed coverage texture.</param>
    /// <param name="sourceRect">Finite signed texture-pixel bounds.</param><param name="modulate">Finite command tint.</param>
    /// <exception cref="ArgumentException">An identity, geometry or tint is invalid.</exception><exception cref="InvalidOperationException">Renderer owner, submission or lifetime rules reject the call.</exception>
    public static void CanvasItemAddLCDTextureRectRegion(RID item, Rect2 rect, RID texture, Rect2 sourceRect, Color modulate) => RequireService().CanvasItemAddGlyphCore(item, rect, texture, sourceRect, modulate, new(CanvasGlyphMode.LCD));
    /// <summary>Appends a retained RGB signed-distance texture region with range and outline controls.</summary>
    /// <param name="item">Live canvas item.</param><param name="rect">Finite signed destination bounds.</param><param name="texture">Live encoded distance texture.</param>
    /// <param name="sourceRect">Finite signed texture-pixel bounds.</param><param name="modulate">Finite tint, or null for white.</param><param name="outlineSize">Source outline size; nonpositive selects ordinary coverage.</param>
    /// <param name="pixelRange">Positive finite encoded range.</param><param name="scale">Positive finite source scale.</param>
    /// <exception cref="ArgumentException">Identity, geometry, tint or resulting parameters are invalid.</exception><exception cref="ArgumentOutOfRangeException">Range or scale is invalid.</exception>
    /// <exception cref="InvalidOperationException">Renderer owner, submission or lifetime rules reject the call.</exception>
    public static void CanvasItemAddMSDFTextureRectRegion(RID item, Rect2 rect, RID texture, Rect2 sourceRect, Color? modulate = null, int outlineSize = 0, float pixelRange = 1, float scale = 1) => RequireService().CanvasItemAddGlyphCore(item, rect, texture, sourceRect, modulate ?? Colors.White, new(CanvasGlyphMode.MSDF, outlineSize, pixelRange, scale));
    private void CanvasItemAddGlyphCore(RID item, Rect2 rect, RID texture, Rect2 source, Color color, CanvasGlyph glyph)
    {
        if (!rect.IsFinite() || !source.IsFinite() || !color.IsFinite()) throw new ArgumentException("Glyph geometry and colors must be finite."); glyph.Validate();
        var state = Change(item, out var node); var image = RenderingTextureRegistry.Resolve(texture); node.ServerCommandTarget(state).AppendServerGlyph(image, rect, source, color, glyph);
    }
}
