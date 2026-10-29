namespace Electron2D;

public partial class SystemFont
{
    private bool _disableEmbeddedBitmaps = true, _forceAutohinter, _generateMipmaps;
    private FontAntialiasing _antialiasing = FontAntialiasing.Gray;
    private bool _msdf;
    private int _msdfRange = 16, _msdfSize = 48;
    /// <summary>Gets or sets native multichannel signed distance generation.</summary>
    /// <value>false initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Preference archives rematch the host;
    /// ResetState restores defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool MultichannelSignedDistanceField { get { lock (FontGate) { ThrowIfDisposed(); return _msdf; } } set => Set(ref _msdf, value); }
    /// <summary>Gets or sets the distance range encoded into MSDF glyph pixels.</summary>
    /// <value>16 initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Preference archives rematch the host;
    /// ResetState restores defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int MSDFPixelRange { get { lock (FontGate) { ThrowIfDisposed(); return _msdfRange; } } set => Set(ref _msdfRange, value); }
    /// <summary>Gets or sets the source size used for a shared MSDF glyph image.</summary>
    /// <value>48 initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Preference archives rematch the host;
    /// ResetState restores defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int MSDFSize { get { lock (FontGate) { ThrowIfDisposed(); return _msdfSize; } } set => Set(ref _msdfSize, value); }
    /// <summary>Gets or sets coverage generation for realized primary and automatic fallback font sources.</summary>
    /// <value>Gray initially. Unknown values are stored and use grayscale coverage.</value>
    /// <remarks>Changes invalidate realized faces, dependent text and variations after active readers.
    /// LCD glyph drawing requires the GPU canvas profile unless the captured subpixel layout is None.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontAntialiasing Antialiasing { get { lock (FontGate) { ThrowIfDisposed(); return _antialiasing; } } set => Set(ref _antialiasing, value); }
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
        new PropertyDescriptor<SystemFont, bool>(nameof(MultichannelSignedDistanceField), f => f.MultichannelSignedDistanceField, (f, v) => f.MultichannelSignedDistanceField = v, _ => false, stored: true),
        new PropertyDescriptor<SystemFont, int>(nameof(MSDFPixelRange), f => f.MSDFPixelRange, (f, v) => f.MSDFPixelRange = v, _ => 16, stored: true),
        new PropertyDescriptor<SystemFont, int>(nameof(MSDFSize), f => f.MSDFSize, (f, v) => f.MSDFSize = v, _ => 48, stored: true),
        new PropertyDescriptor<SystemFont, FontAntialiasing>(nameof(Antialiasing), f => f.Antialiasing, (f, v) => f.Antialiasing = v, _ => FontAntialiasing.Gray, stored: true),
        new PropertyDescriptor<SystemFont, bool>(nameof(DisableEmbeddedBitmaps), f => f.DisableEmbeddedBitmaps, (f, v) => f.DisableEmbeddedBitmaps = v, _ => true, stored: true),
        new PropertyDescriptor<SystemFont, bool>(nameof(ForceAutohinter), f => f.ForceAutohinter, (f, v) => f.ForceAutohinter = v, _ => false, stored: true),
        new PropertyDescriptor<SystemFont, bool>(nameof(GenerateMIPMaps), f => f.GenerateMIPMaps, (f, v) => f.GenerateMIPMaps = v, _ => false, stored: true)
    ];
}
