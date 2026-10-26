using SDL3;

namespace Electron2D;

public sealed partial class DisplayServer
{
    private WindowInputRegion? _mouseInputRegion;
    private Vector2[] _mouseInputPolygon = [];
    private bool _mousePassthrough;

    /// <summary>Sets the native client-coordinate polygon accepting mouse events.</summary>
    /// <param name="region">Copied vertices using the even-odd fill rule; empty restores the ordinary input region.</param>
    /// <param name="windowId">A live native window identity, or zero for the main window.</param>
    /// <remarks>The MousePassthrough flag overrides input from the retained polygon. X11 preserves rendering;
    /// Windows also clips the visible window to the polygon. macOS evaluates polygon membership during the owner event pump.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or exceeds the native adapter's integer range.</exception>
    /// <exception cref="NotSupportedException">This native video driver lacks an input-region adapter.</exception>
    /// <exception cref="InvalidOperationException">There is no active display, access is off-owner, or a native request fails.</exception>
    /// <exception cref="ArgumentException">The native window identity is absent.</exception>
    public static void WindowSetMousePassthrough(ReadOnlySpan<Vector2> region, int windowId = MainWindowId) => RequireService().WindowSetMousePassthroughCore(region, windowId);

    internal void WindowSetMousePassthroughCore(ReadOnlySpan<Vector2> region, int windowId = MainWindowId)
    {
        if (windowId != MainWindowId) { ResolveWindow(windowId).WindowSetMousePassthroughCore(region); return; }
        EnsureOwner(); _ = GetWindow(windowId);
        if (region.SequenceEqual(_mouseInputPolygon)) return;
        var copy = region.ToArray(); WindowInputRegion.Validate(copy);
        ApplyMouseInputRegion(_mousePassthrough, copy); _mouseInputPolygon = copy;
    }
    private void SetMousePassthrough(bool enabled)
    {
        if (_mousePassthrough == enabled) return;
        ApplyMouseInputRegion(enabled, _mouseInputPolygon); _mousePassthrough = enabled;
    }
    private void ApplyMouseInputRegion(bool passthrough, Vector2[] polygon)
    {
        if (_mouseInputRegion is null)
        {
            if (!passthrough && polygon.Length == 0) return;
            var driver = SDL.GetCurrentVideoDriver() ?? "";
            if (driver is not ("x11" or "windows" or "cocoa")) throw new NotSupportedException("Native mouse input regions require X11, Windows or macOS.");
            var display = driver == "x11" ? WindowGetNativeHandleCore(HandleType.DisplayHandle) : 0;
            _mouseInputRegion = new WindowInputRegion(_window.DangerousGetHandle(), WindowGetNativeHandleCore(HandleType.WindowHandle), driver, display);
        }
        try { _mouseInputRegion.Apply(passthrough, polygon, GetMousePixelScale()); }
        catch (Exception error)
        {
            try { _mouseInputRegion.Apply(_mousePassthrough, _mouseInputPolygon, GetMousePixelScale()); }
            catch (Exception rollback) { throw new AggregateException(error, rollback); }
            throw;
        }
    }
    private void RefreshMouseInputRegion()
    { if (_mouseInputRegion is not null) _mouseInputRegion.Apply(_mousePassthrough, _mouseInputPolygon, GetMousePixelScale()); }
    private void UpdateMouseInputRegions()
    {
        _mouseInputRegion?.UpdatePointer();
        foreach (var child in _childWindows.Values) child._mouseInputRegion?.UpdatePointer();
    }
}
