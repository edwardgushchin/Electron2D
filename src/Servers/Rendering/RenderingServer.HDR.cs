namespace Electron2D;

public sealed partial class RenderingServer
{
    internal bool ViewportHDR(Viewport view) => view is Window { HDROutputRequested: true } || (_serverViewports.TryGetValue(view, out var state) && state.HDR is { } enabled ? enabled : view.UseHDR2D);
    internal void PublishViewportHDR(Viewport view) { if (_serverViewports.TryGetValue(view, out var state)) state.HDR = null; }
    private void ViewportSetUseHDR2DCore(RID rid, bool enabled)
    {
        var state = ChangeViewport(rid, out var view); var previous = ViewportHDR(view);
        state.HDR = enabled; if (previous != ViewportHDR(view)) view.TextureSizeChanged();
    }

    /// <summary>Selects linear HDR canvas output for a live scene or caller-owned viewport.</summary>
    /// <param name="viewport">A live viewport identity belonging to this renderer.</param>
    /// <param name="enabled">True for RGBA half-float targets; false for normalized byte targets.</param>
    /// <remarks>This renderer-session override preserves the authored getter. A valid UseHDR2D assignment
    /// republishes authored state. The next submission replaces dependent native targets and pipelines;
    /// unsupported backend capabilities reject explicitly before target publication.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or mutation violates owner, capture or submission rules.</exception>
    /// <exception cref="AggregateException">A texture observer fails after publication; every view is still notified.</exception>
    public static void ViewportSetUseHDR2D(RID viewport, bool enabled) => RequireService().ViewportSetUseHDR2DCore(viewport, enabled);
}
