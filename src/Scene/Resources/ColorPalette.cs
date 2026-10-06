namespace Electron2D;

/// <summary>A stored ordered collection of color swatches.</summary>
/// <remarks>Arrays are copied on both sides of the property boundary. Assignment is silent;
/// callers explicitly emit Changed when using a palette as a live observable resource.</remarks>
public sealed class ColorPalette : Resource
{
    private readonly object _gate = new();
    private Color[] _colors = [];
    /// <summary>Creates an empty palette.</summary>
    public ColorPalette() { }
    /// <summary>Gets a copy of, or replaces, the ordered colors.</summary>
    /// <value>Empty initially; duplicates, alpha and HDR values are retained.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    public Color[] Colors
    {
        get { lock (_gate) { ThrowIfDisposed(); return (Color[])_colors.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = (Color[])value.Clone(); lock (_gate) { ThrowIfDisposed(); _colors = copy; } }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [new PropertyDescriptor<ColorPalette, Color[]>(nameof(Colors), p => p.Colors, (p, v) => p.Colors = v, _ => [], stored: true)]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new ColorPalette();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { ((ColorPalette)target).Colors = Colors; }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) _colors = []; base.Dispose(disposing); }
}
