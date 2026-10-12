namespace Electron2D;

public partial class FontFile
{
    private static readonly PropertyDescriptor[] RasterProperties =
    [
        new PropertyDescriptor<FontFile, bool>(nameof(DisableEmbeddedBitmaps), f => f.DisableEmbeddedBitmaps, (f, v) => f.DisableEmbeddedBitmaps = v, _ => true, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(ForceAutohinter), f => f.ForceAutohinter, (f, v) => f.ForceAutohinter = v, _ => false, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(GenerateMIPMaps), f => f.GenerateMIPMaps, (f, v) => f.GenerateMIPMaps = v, _ => false, stored: true)
    ];
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
    private void SetRasterConfiguration(Func<FontData, bool> get, Action<FontData, bool> set, bool value)
    {
        List<FontData>? retired = null;
        lock (FontGate)
        {
            ThrowIfDisposed(); if (get(_fontData!) == value) return;
            if (_deferred) { set(_fontData!, value); InvalidateFontStateLocked(); }
            else
            {
                var copies = new List<FontCache>();
                for (var i = 0; i < _caches.Count; i++) { var data = DataLocked(i); lock (data.Gate) copies.Add(_caches[i].Clone(discardGenerated: true)); }
                retired = PublishCachesLocked(copies, data => set(data, value));
            }
        }
        if (retired != null) NotifyRetired(retired); else EmitChanged();
    }
}
