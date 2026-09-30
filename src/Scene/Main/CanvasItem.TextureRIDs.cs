namespace Electron2D;

public abstract partial class CanvasItem
{
    /// <summary>Draws a rendering texture RID during this item's canvas recording scope.</summary>
    /// <param name="texture">A live resource-owned or server-owned texture identity.</param>
    /// <param name="position">Finite local top-left position.</param>
    /// <param name="modulate">Finite multiplier, or null for white.</param>
    /// <exception cref="ArgumentException">The RID is stale/wrong-kind or input is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">The item is off-owner or outside canvas recording.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawTexture(RID texture, Vector2 position, Color? modulate = null) =>
        DrawTexture(RenderingTextureRegistry.Resolve(texture), position, modulate);

    /// <summary>Stretches or tiles a rendering texture identity over a local rectangle.</summary>
    /// <param name="texture">A live texture RID.</param>
    /// <param name="rect">Finite local destination; negative sizes flip axes.</param>
    /// <param name="tile">Whether to repeat at logical texture size.</param>
    /// <param name="modulate">Finite multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes.</param>
    /// <exception cref="ArgumentException">The RID or geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The item is off-owner or outside recording.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawTextureRect(RID texture, Rect2 rect, bool tile, Color? modulate = null, bool transpose = false) =>
        DrawTextureRect(RenderingTextureRegistry.Resolve(texture), rect, tile, modulate, transpose);

    /// <summary>Draws a source region from a rendering texture identity.</summary>
    /// <param name="texture">A live texture RID.</param>
    /// <param name="rect">Finite local destination.</param>
    /// <param name="sourceRect">Finite logical source region; negative sizes flip axes.</param>
    /// <param name="modulate">Finite multiplier, or null for white.</param>
    /// <param name="transpose">Whether to exchange texture axes.</param>
    /// <param name="clipUV">Whether to constrain samples to source texel centers.</param>
    /// <exception cref="ArgumentException">The RID or geometry is invalid.</exception>
    /// <exception cref="InvalidOperationException">The item is off-owner or outside recording.</exception>
    /// <exception cref="ObjectDisposedException">The item is disposed.</exception>
    public void DrawTextureRectRegion(RID texture, Rect2 rect, Rect2 sourceRect, Color? modulate = null, bool transpose = false, bool clipUV = true) =>
        DrawTextureRectRegion(RenderingTextureRegistry.Resolve(texture), rect, sourceRect, modulate, transpose, clipUV);
}
