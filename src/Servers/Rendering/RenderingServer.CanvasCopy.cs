namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Selects a retained screen snapshot at this canvas item's draw-order position.</summary>
    /// <param name="item">A live scene or caller-owned canvas item identity.</param>
    /// <param name="enabled">Whether this item copies already drawn pixels.</param>
    /// <param name="rect">Finite local bounds, including signed dimensions. The zero-valued rectangle copies the complete viewport.</param>
    /// <remarks>Ordinary items default to disabled. The copy precedes this item's commands, requires no drawing commands,
    /// and follows its presented transform, clipping, visibility, mask and Z/order on each viewport. Computed nonfinite
    /// bounds reject rendering before native drawing. Copies within an active
    /// group capture are ignored. Pixels outside a nonempty copied region are unspecified. Scene getters and PackedScene
    /// values remain authored; assigning either BackBufferCopy property republishes its source policy, including equal values.
    /// Clearing draw commands does not remove this policy. Overrides end with the rendering session.</remarks>
    /// <exception cref="ArgumentException">The item identity or rectangle is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner, native submission or lifetime rules reject mutation.</exception>
    public static void CanvasItemSetCopyToBackBuffer(RID item, bool enabled, Rect2 rect)
    {
        if (!rect.IsFinite()) throw new ArgumentException("Copy rectangles must be finite.", nameof(rect));
        RequireService().Change(item, out _).Copy = (enabled, rect);
    }
}
