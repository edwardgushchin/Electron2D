namespace Electron2D;

public sealed partial class RenderingServer
{
    internal bool ViewportDebanding(Viewport view) => _serverViewports.TryGetValue(view, out var state) && state.Debanding is { } enabled ? enabled : view.UseDebanding;
    internal void PublishViewportDebanding(Viewport view) { if (_serverViewports.TryGetValue(view, out var state)) state.Debanding = null; }
    private void ViewportSetUseDebandingCore(RID rid, bool enabled) => ChangeViewport(rid, out _).Debanding = enabled;

    /// <summary>Selects HDR screen dithering for a live scene or caller-owned viewport.</summary>
    /// <param name="viewport">A live viewport identity belonging to this renderer.</param>
    /// <param name="enabled">Whether to dither HDR root output before byte quantization.</param>
    /// <remarks>This renderer-session override preserves the authored getter. A valid UseDebanding assignment
    /// republishes authoring, including equal values. In the 2D domain only the root HDR viewport affects
    /// screen output; linear/offscreen pixels and movie capture remain unchanged. Compatibility rejects
    /// enabled debanding at the next target preparation.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable or owner, capture or submission rules are violated.</exception>
    public static void ViewportSetUseDebanding(RID viewport, bool enabled) => RequireService().ViewportSetUseDebandingCore(viewport, enabled);
}
