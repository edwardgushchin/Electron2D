using SDL3;

namespace Electron2D;

internal sealed partial class WindowInputRegion : IDisposable
{
    private readonly nint _window, _native, _display;
    private readonly string _driver;
    private Vector2[] _polygon = [];
    private float _scale;
    private bool _passthrough, _ignores;

    internal WindowInputRegion(nint window, nint native, string driver, nint display)
    {
        _window = window; _native = native; _driver = driver; _display = display;
        if (driver == "x11") CheckXShape();
        else if (driver == "cocoa") PrepareCocoa();
        else if (driver == "windows") PrepareWindows();
        else throw new NotSupportedException("This video driver has no native mouse input region adapter.");
    }
    internal static void Validate(ReadOnlySpan<Vector2> polygon)
    {
        foreach (var point in polygon) if (!point.IsFinite()) throw new ArgumentOutOfRangeException(nameof(polygon), "Mouse input polygon coordinates must be finite.");
    }
    internal void Apply(bool passthrough, Vector2[] polygon, float scale)
    {
        if (_driver == "x11") ApplyX11(passthrough, polygon, scale);
        else if (_driver == "windows") ApplyWindows(polygon, scale);
        _passthrough = passthrough; _polygon = polygon; _scale = scale;
        UpdatePointer();
    }
    internal void UpdatePointer()
    {
        if (_driver != "cocoa") return;
        var ignore = _passthrough;
        if (!ignore && _polygon.Length != 0)
        {
            SDL.GetGlobalMouseState(out var x, out var y);
            if (!SDL.GetWindowPosition(_window, out var originX, out var originY)) throw new InvalidOperationException("Cannot query native input region origin.");
            ignore = !Geometry.IsPointInPolygon(new((x - originX) * _scale, (y - originY) * _scale), _polygon);
        }
        if (ignore == _ignores) return;
        SetCocoaIgnores(ignore); _ignores = ignore;
    }
    public void Dispose() => ReleaseWindowsSubclass();
}
