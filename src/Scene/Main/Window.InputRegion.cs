namespace Electron2D;

public partial class Window
{
    private Vector2[] _mousePassthroughPolygon = [];
    /// <summary>Gets or sets whether native mouse input passes through this window.</summary>
    /// <value>False by default.</value>
    /// <remarks>Overrides MousePassthroughPolygon for input. Embedded windows retain the policy for a later native host and do not apply it to GUI routing.</remarks>
    public bool MousePassthrough { get => GetFlag(WindowFlag.MousePassthrough); set => SetFlag(WindowFlag.MousePassthrough, value); }

    /// <summary>Gets or sets the native client-coordinate polygon accepting mouse input.</summary>
    /// <value>Copied finite vertices; empty restores the ordinary input region.</value>
    /// <remarks>Uses the even-odd fill rule. A nonempty degenerate polygon accepts no area. The native MousePassthrough flag overrides input from this polygon.
    /// X11, Windows and macOS provide native adapters; other drivers reject effective requests. Windows also clips drawing; X11 and macOS preserve drawing. Embedded windows retain the polygon without changing GUI routing.</remarks>
    /// <exception cref="ArgumentNullException">The vertex array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or exceeds the active native adapter's integer range.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner, forbidden during capture, or the native request fails.</exception>
    /// <exception cref="NotSupportedException">The native host has no input-region adapter.</exception>
    /// <exception cref="ObjectDisposedException">This window is disposed.</exception>
    public Vector2[] MousePassthroughPolygon
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return (Vector2[])_mousePassthroughPolygon.Clone(); }
        set
        {
            EnsureMutable(); ArgumentNullException.ThrowIfNull(value);
            if (value.AsSpan().SequenceEqual(_mousePassthroughPolygon)) return;
            var copy = (Vector2[])value.Clone(); WindowInputRegion.Validate(copy);
            _display?.WindowSetMousePassthroughCore(copy); _mousePassthroughPolygon = copy;
        }
    }
}
