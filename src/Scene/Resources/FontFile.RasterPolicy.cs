namespace Electron2D;

public partial class FontFile
{
    private static readonly PropertyDescriptor[] RasterProperties =
    [
        new PropertyDescriptor<FontFile, bool>(nameof(MultichannelSignedDistanceField), f => f.MultichannelSignedDistanceField, (f, v) => f.MultichannelSignedDistanceField = v, _ => false, stored: true),
        new PropertyDescriptor<FontFile, int>(nameof(MSDFPixelRange), f => f.MSDFPixelRange, (f, v) => f.MSDFPixelRange = v, _ => 16, stored: true),
        new PropertyDescriptor<FontFile, int>(nameof(MSDFSize), f => f.MSDFSize, (f, v) => f.MSDFSize = v, _ => 48, stored: true),
        new PropertyDescriptor<FontFile, FontAntialiasing>(nameof(Antialiasing), f => f.Antialiasing, (f, v) => f.Antialiasing = v, _ => FontAntialiasing.Gray, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(DisableEmbeddedBitmaps), f => f.DisableEmbeddedBitmaps, (f, v) => f.DisableEmbeddedBitmaps = v, _ => true, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(ForceAutohinter), f => f.ForceAutohinter, (f, v) => f.ForceAutohinter = v, _ => false, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(GenerateMIPMaps), f => f.GenerateMIPMaps, (f, v) => f.GenerateMIPMaps = v, _ => false, stored: true)
    ];
    /// <summary>Gets or sets native multichannel signed distance generation.</summary>
    /// <value>false initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Data assignment preserves preferences;
    /// file imports and ResetState restore defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool MultichannelSignedDistanceField { get => Read(static d => d.MultichannelSignedDistanceField); set => SetRasterConfiguration(static d => d.MultichannelSignedDistanceField, static (d, v) => d.MultichannelSignedDistanceField = v, value); }
    /// <summary>Gets or sets the distance range encoded into MSDF glyph pixels.</summary>
    /// <value>16 initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Data assignment preserves preferences;
    /// file imports and ResetState restore defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int MSDFPixelRange { get => Read(static d => d.MSDFPixelRange); set => SetRasterConfiguration(static d => d.MSDFPixelRange, static (d, v) => d.MSDFPixelRange = v, value); }
    /// <summary>Gets or sets the source size used for a shared MSDF glyph image.</summary>
    /// <value>48 initially. Values are stored preferences; generation requires positive bounded size and range.</value>
    /// <remarks>Generating a new native image owns temporary storage until return; allocation failure throws OutOfMemoryException.
    /// Changes retire generated images after active readers, preserve authored entries and invalidate
    /// dependent text and variations. MSDF shares source pixels across drawn sizes, ignores oversampling and
    /// subpixel phases, and requires the GPU canvas profile. Data assignment preserves preferences;
    /// file imports and ResetState restore defaults. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int MSDFSize { get => Read(static d => d.MSDFSize); set => SetRasterConfiguration(static d => d.MSDFSize, static (d, v) => d.MSDFSize = v, value); }
    /// <summary>Gets or sets native glyph coverage generation.</summary>
    /// <value>Gray initially. Unknown numeric values are retained and use grayscale rendering.</value>
    /// <remarks>Changes retire native glyph caches after active reads, preserve authored bitmap entries and invalidate
    /// dependent text and variations. Shaping metrics remain unhinted. LCD drawing requires the GPU canvas profile;
    /// a None subpixel layout selects grayscale instead. Equal assignments are silent.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontAntialiasing Antialiasing { get => Read(static d => d.Antialiasing); set => SetRasterConfiguration(static d => d.Antialiasing, static (d, v) => d.Antialiasing = v, value); }
    /// <summary>Gets or sets whether native monochrome bitmap strikes are bypassed.</summary>
    /// <value>True initially. Color strikes remain available; stroke rasterization always uses outlines.</value>
    /// <remarks>Changes preserve authored bitmap records, replace native raster data and invalidate dependent layouts.
    /// Data assignment preserves policy; dynamic/bitmap file loading and ResetState restore defaults.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool DisableEmbeddedBitmaps { get => Read(static d => d.DisableEmbeddedBitmaps); set => SetRasterConfiguration(static d => d.DisableEmbeddedBitmaps, static (d, v) => d.DisableEmbeddedBitmaps = v, value); }
    /// <summary>Gets or sets forced FreeType automatic hinting for native glyph rasterization.</summary>
    /// <value>False initially. Unhinted shaping advances remain independent of raster hinting.</value>
    /// <remarks>Changes replace native raster data while preserving authored records and active old readers.
    /// Equal assignments are silent; Changed observers run outside locks after committing.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool ForceAutohinter { get => Read(static d => d.ForceAutohinter); set => SetRasterConfiguration(static d => d.ForceAutohinter, static (d, v) => d.ForceAutohinter = v, value); }
    /// <summary>Gets or sets mipmap generation for glyph textures and indexed image pages.</summary>
    /// <value>False initially. Mipmap sampling requires a canvas filter with mipmaps.</value>
    /// <remarks>Both ordinary glyph images and indexed pages publish complete mip chains. Changes invalidate
    /// dependent layouts and retire previous texture residency after active source reads finish. Recorded pixels remain valid.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool GenerateMIPMaps { get => Read(static d => d.GenerateMIPMaps); set => SetRasterConfiguration(static d => d.GenerateMIPMaps, static (d, v) => d.GenerateMIPMaps = v, value); }
    private void SetRasterConfiguration<T>(Func<FontData, T> get, Action<FontData, T> set, T value)
    {
        List<FontData>? retired = null;
        lock (FontGate)
        {
            ThrowIfDisposed(); if (EqualityComparer<T>.Default.Equals(get(_fontData!), value)) return;
            if (_deferred) { set(_fontData!, value); InvalidateFontStateLocked(); }
            else
            {
                var copies = new List<FontCache>();
                for (var i = 0; i < _caches.Count; i++) { var data = DataLocked(i); lock (data.Gate) copies.Add(_caches[i].Clone(discardGenerated: true)); }
                retired = PublishCachesLocked(copies, data => set(data, value));
            }
        }
        if (retired != null) NotifyRetired(retired); else EmitFontChanged();
    }
}
