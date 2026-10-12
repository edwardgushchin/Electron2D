namespace Electron2D;

public partial class SystemFont
{
    private bool _disableEmbeddedBitmaps = true, _forceAutohinter, _generateMipmaps;
    /// <summary>Gets or sets whether selected native sources bypass monochrome embedded bitmap strikes.</summary>
    /// <value>True initially. Color strikes remain available and strokes use outlines.</value>
    /// <remarks>Changes retire realized sources after active readers and invalidate dependent text/variations.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool DisableEmbeddedBitmaps { get { lock (FontGate) { ThrowIfDisposed(); return _disableEmbeddedBitmaps; } } set => Set(ref _disableEmbeddedBitmaps, value); }
    /// <summary>Gets or sets forced automatic hinting for selected native glyph rasters.</summary>
    /// <value>False initially; unhinted shaping advances remain independent.</value>
    /// <remarks>Equal assignments are silent; observers run after committing outside storage locks.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool ForceAutohinter { get { lock (FontGate) { ThrowIfDisposed(); return _forceAutohinter; } } set => Set(ref _forceAutohinter, value); }
    /// <summary>Gets or sets mipmap generation for selected glyph textures and inherited indexed pages.</summary>
    /// <value>False initially. Sampling mip levels requires a canvas mipmap filter.</value>
    /// <remarks>Changes retire prior sources and textures after active reads; preference storage rematches the host.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool GenerateMIPMaps { get { lock (FontGate) { ThrowIfDisposed(); return _generateMipmaps; } } set => Set(ref _generateMipmaps, value); }
    private static readonly PropertyDescriptor[] RasterProperties =
    [
        new PropertyDescriptor<SystemFont, bool>(nameof(DisableEmbeddedBitmaps), f => f.DisableEmbeddedBitmaps, (f, v) => f.DisableEmbeddedBitmaps = v, _ => true, stored: true),
        new PropertyDescriptor<SystemFont, bool>(nameof(ForceAutohinter), f => f.ForceAutohinter, (f, v) => f.ForceAutohinter = v, _ => false, stored: true),
        new PropertyDescriptor<SystemFont, bool>(nameof(GenerateMIPMaps), f => f.GenerateMIPMaps, (f, v) => f.GenerateMIPMaps = v, _ => false, stored: true)
    ];
}
