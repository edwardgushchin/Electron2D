namespace Electron2D;

public sealed partial class RenderingServer
{
    internal ScreenSpaceAAMode ViewportScreenSpaceAA(Viewport view) =>
        _serverViewports.TryGetValue(view, out var state) && state.ScreenSpaceAA is { } mode ? mode : view.ScreenSpaceAA;

    internal static void ValidateViewportScreenSpaceAA(Viewport view, ScreenSpaceAAMode mode)
    {
        var renderer = view.RenderingOwner;
        // Ready runs before the first frame registers scene viewports with their renderer.
        if (renderer is null && Service is { } active && view.IsInsideTree && ReferenceEquals(view.Tree, active._window.Tree))
            renderer = active;
        renderer?.EnsureServerViewportChange();
        renderer?._backend.ValidateScreenSpaceAA(mode);
    }

    internal void PublishViewportScreenSpaceAA(Viewport view)
    {
        if (_serverViewports.TryGetValue(view, out var state)) state.ScreenSpaceAA = null;
    }

    /// <summary>Sets screen-space antialiasing for the next frame of a live scene or caller-owned viewport.</summary>
    /// <param name="viewport">The live viewport identity in this rendering session.</param>
    /// <param name="mode">Disabled, FXAA or SMAA.</param>
    /// <remarks>The renderer override preserves authored and packed scene values. A scene property assignment republishes its value.</remarks>
    /// <exception cref="ArgumentException">The viewport identity is invalid or retired.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported modes.</exception>
    /// <exception cref="NotSupportedException">The renderer cannot execute the selected mode.</exception>
    /// <exception cref="InvalidOperationException">The renderer is unavailable, ownership differs or mutation is guarded.</exception>
    public static void ViewportSetScreenSpaceAA(RID viewport, ScreenSpaceAAMode mode)
    {
        var service = RequireService();
        if (!Viewport.IsScreenSpaceAAMode(mode)) throw new ArgumentOutOfRangeException(nameof(mode));
        service.EnsureServerViewportChange();
        service.ServerViewport(viewport);
        service._backend.ValidateScreenSpaceAA(mode);
        service.ChangeViewport(viewport, out _).ScreenSpaceAA = mode;
    }
}
