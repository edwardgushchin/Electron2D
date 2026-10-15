using System.Text;

namespace Electron2D;

/// <summary>A scalable SVG texture with retained viewport raster variants.</summary>
/// <remarks>Authoring state is copied and synchronized. Rasterization uses the native SVG codec on first use.
/// Scale variants are borrowed and retire on settings changes, last matching viewport release or disposal.
/// Coordinate multi-call editing, drawing and resource disposal through the owning application.</remarks>
public sealed partial class DPITexture : Texture
{
    private readonly object _gate = new();
    private string _source = "";
    private string? _paintSource;
    private float _baseScale = 1, _saturation = 1;
    private bool _fixAlphaBorder, _premultAlpha;
    private Vector2i _baseSize, _sizeOverride;
    private Dictionary<Color, Color> _colorMap = [];
    private readonly Dictionary<int, RasterVariant> _variants = [];

    /// <summary>Creates an empty SVG texture with unit scale and saturation.</summary>
    public DPITexture() { }
    /// <summary>Creates a texture from SVG authoring data without loading a native decoder yet.</summary>
    /// <param name="source">A valid bounded SVG document.</param>
    /// <param name="scale">Finite positive base scale.</param>
    /// <param name="saturation">Finite saturation multiplier.</param>
    /// <param name="colorMap">Copied paint replacements, or null for none.</param>
    /// <returns>A caller-owned resource with independent raster caches.</returns>
    /// <exception cref="ArgumentException">An argument is null or contains nonfinite data.</exception>
    /// <exception cref="InvalidDataException">SVG dimensions or contents exceed image limits.</exception>
    public static DPITexture CreateFromString(string source, float scale = 1, float saturation = 1, Dictionary<Color, Color>? colorMap = null)
    {
        var result = new DPITexture();
        try { result.BaseScale = scale; result.Saturation = saturation; if (colorMap is not null) result.ColorMap = colorMap; result.SetSource(source); return result; }
        catch { result.Dispose(); throw; }
    }
    /// <summary>Returns the original SVG authoring text.</summary>
    /// <returns>The source, or an empty string for an empty resource.</returns>
    public string GetSource() { lock (_gate) { ThrowIfDisposed(); return _source; } }
    /// <summary>Replaces SVG source, invalidating raster variants after validation.</summary>
    /// <param name="source">A bounded SVG document, or empty to clear the texture.</param>
    /// <exception cref="ArgumentNullException">Source is null.</exception>
    /// <exception cref="InvalidDataException">Source or raster dimensions are invalid.</exception>
    public void SetSource(string source)
    {
        ArgumentNullException.ThrowIfNull(source);
        lock (_gate)
        {
            ThrowIfDisposed(); if (_source == source) return;
            var size = SourceSize(source, _baseScale); _source = source; _baseSize = size; InvalidateLocked();
        }
        EmitChanged();
    }
    /// <summary>Gets or sets the finite positive SVG base raster scale.</summary>
    /// <value>One initially.</value>
    public float BaseScale
    {
        get { lock (_gate) { ThrowIfDisposed(); return _baseScale; } }
        set
        {
            if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_baseScale == value) return; var size = SourceSize(_source, value); _baseScale = value; _baseSize = size; InvalidateLocked(); }
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the finite saturation multiplier applied after rasterization.</summary>
    /// <value>One initially; values outside zero through one are allowed.</value>
    public float Saturation { get { lock (_gate) { ThrowIfDisposed(); return _saturation; } } set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); Change(ref _saturation, value); } }
    /// <summary>Gets or sets whether transparent edge RGB is repaired before optional premultiplication.</summary>
    /// <value>False initially.</value>
    public bool FixAlphaBorder { get { lock (_gate) { ThrowIfDisposed(); return _fixAlphaBorder; } } set => Change(ref _fixAlphaBorder, value); }
    /// <summary>Gets or sets whether generated RGB is multiplied by alpha.</summary>
    /// <value>False initially; drawing such data requires a premultiplied-alpha material.</value>
    public bool PremultAlpha { get { lock (_gate) { ThrowIfDisposed(); return _premultAlpha; } } set => Change(ref _premultAlpha, value); }
    /// <summary>Gets or sets copied SVG paint replacements.</summary>
    /// <value>An empty map initially. Double-quoted fill, stroke and stop-color attributes are replaced before decoding;
    /// replacement RGB is independent of the SVG opacity attributes.</value>
    /// <exception cref="ArgumentException">The map is null or contains nonfinite colors.</exception>
    public Dictionary<Color, Color> ColorMap
    {
        get { lock (_gate) { ThrowIfDisposed(); return new(_colorMap); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); var copy = new Dictionary<Color, Color>(value);
            foreach (var pair in copy) if (!pair.Key.IsFinite() || !pair.Value.IsFinite()) throw new ArgumentException("Paint colors must be finite.", nameof(value));
            lock (_gate) { ThrowIfDisposed(); if (_colorMap.Count == copy.Count && copy.All(p => _colorMap.TryGetValue(p.Key, out var old) && old == p.Value)) return; _colorMap = copy; InvalidateLocked(); }
            EmitChanged();
        }
    }
    /// <summary>Sets independent logical size overrides without resampling; zero restores an intrinsic axis.</summary>
    /// <param name="size">Nonnegative logical dimensions.</param>
    /// <exception cref="ArgumentOutOfRangeException">A dimension is negative.</exception>
    public void SetSizeOverride(Vector2i size)
    {
        if (size.X < 0 || size.Y < 0) throw new ArgumentOutOfRangeException(nameof(size));
        lock (_gate) { ThrowIfDisposed(); if (_sizeOverride == size) return; _sizeOverride = size; var logical = SizeLocked; foreach (var texture in _variants.Values) texture.Texture.SetSizeOverride(logical); }
        EmitChanged();
    }
    /// <summary>Returns a borrowed rendering identity for the currently recording item's viewport scale.</summary>
    /// <returns>The stable base identity outside drawing, otherwise the matching retained scale identity.</returns>
    /// <remarks>Scaled identities retire on authoring changes or release of their last viewport scale.</remarks>
    public RID GetScaledRID()
    {
        var viewport = CanvasItem.CurrentDrawingItem?.CanvasViewport;
        var key = viewport?.DPIScaleKey ?? 64;
        return key == 64 ? GetRID() : GetVariant(key)?.GetRID() ?? GetRID();
    }
    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return SizeLocked.X; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return SizeLocked.Y; } }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (_gate) { ThrowIfDisposed(); var size = SizeLocked; return new(size.X, size.Y); } }
    /// <inheritdoc />
    public override bool HasAlpha { get { ThrowIfDisposed(); return true; } }
    /// <inheritdoc />
    public override Image? GetImage() => GetVariant(64)?.GetImage();
    internal override TexturePixels? CapturePixels() => GetVariant(64)?.CapturePixels();
    internal Texture? ForViewport(Viewport? viewport)
    {
        lock (_gate) { ThrowIfDisposed(); if (_source.Length == 0) return null; }
        var key = viewport?.DPIScaleKey ?? 64; return key == 64 ? this : GetVariant(key);
    }
    private Vector2i SizeLocked => new(_sizeOverride.X == 0 ? _baseSize.X : _sizeOverride.X, _sizeOverride.Y == 0 ? _baseSize.Y : _sizeOverride.Y);
    private void Change<T>(ref T field, T value) where T : IEquatable<T>
    {
        lock (_gate) { ThrowIfDisposed(); if (field.Equals(value)) return; field = value; InvalidateLocked(); }
        EmitChanged();
    }
    private static Vector2i SourceSize(string source, float scale)
    {
        if (source.Length == 0) return default;
        if (source.Length > 67108864) throw new InvalidDataException("SVG source exceeds 64 MiB.");
        try
        {
            var utf8 = new UTF8Encoding(false, true);
            if (utf8.GetByteCount(source) > 67108864) throw new InvalidDataException("SVG source exceeds 64 MiB.");
            return Image.SVGSize(utf8.GetBytes(source), scale);
        }
        catch (EncoderFallbackException error) { throw new InvalidDataException("SVG source must contain valid text.", error); }
    }
    private ImageTexture? GetVariant(int key)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (_source.Length == 0) return null;
            var level = CurrentLevel(key);
            if (_variants.TryGetValue(key, out var cached))
            {
                if (ReferenceEquals(cached.Level, level)) return cached.Texture;
                cached.Texture.Dispose(); _variants.Remove(key);
            }
            _paintSource ??= RemapPaint(_source, _colorMap);
            using var image = new Image(); image.LoadSVGFromString(_paintSource, _baseScale * (key / 64f));
            if (_saturation != 1) image.AdjustBCS(1, 1, _saturation);
            if (_fixAlphaBorder) image.FixAlphaEdges();
            if (_premultAlpha) image.PremultiplyAlpha();
            var texture = ImageTexture.CreateFromImage(image);
            try { texture.SetSizeOverride(SizeLocked); if (level is not null) TrackScale(key, level, this); _variants.Add(key, new(texture, level)); return texture; }
            catch { texture.Dispose(); throw; }
        }
    }
    private static string RemapPaint(string source, Dictionary<Color, Color> colors)
    {
        if (colors.Count == 0) return source;
        foreach (var prefix in new[] { "stop-color=\"", "fill=\"", "stroke=\"" })
        {
            var start = 0; StringBuilder? output = null; var copied = 0;
            while ((start = source.IndexOf(prefix, start, StringComparison.Ordinal)) >= 0)
            {
                start += prefix.Length; var end = source.IndexOf('"', start);
                if (end < 0) throw new InvalidDataException("Malformed SVG paint attribute.");
                var text = source[start..end];
                if (text != "none" && !text.StartsWith("url(", StringComparison.Ordinal) && colors.TryGetValue(Color.FromString(text, Colors.Black), out var replacement))
                {
                    output ??= new StringBuilder(source.Length); output.Append(source, copied, start - copied).Append('#').Append(replacement.ToHTML(false)); copied = end;
                }
                start = end + 1;
            }
            if (output is not null) { output.Append(source, copied, source.Length - copied); source = output.ToString(); }
        }
        return source;
    }
    private void InvalidateLocked() { _paintSource = null; foreach (var texture in _variants.Values) texture.Texture.Dispose(); _variants.Clear(); }
    private void RetireScale(int key, ScaleLevel level) { lock (_gate) { if (_variants.TryGetValue(key, out var variant) && ReferenceEquals(variant.Level, level)) { _variants.Remove(key); variant.Texture.Dispose(); } } }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new DPITexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var copy = (DPITexture)target;
            lock (copy._gate) { copy._source = _source; copy._baseScale = _baseScale; copy._saturation = _saturation; copy._baseSize = _baseSize; copy._sizeOverride = default; copy._fixAlphaBorder = _fixAlphaBorder; copy._premultAlpha = _premultAlpha; copy._colorMap = new(_colorMap); }
        }
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat([
        new PropertyDescriptor<DPITexture, string>("Source", t => t.GetSource(), (t,v) => t.SetSource(v), _ => "", stored: true),
        new PropertyDescriptor<DPITexture, float>(nameof(BaseScale), t => t.BaseScale, (t,v) => t.BaseScale=v, _ => 1, stored: true),
        new PropertyDescriptor<DPITexture, float>(nameof(Saturation), t => t.Saturation, (t,v) => t.Saturation=v, _ => 1, stored: true),
        new PropertyDescriptor<DPITexture, bool>(nameof(FixAlphaBorder), t => t.FixAlphaBorder, (t,v) => t.FixAlphaBorder=v, _ => false, stored: true),
        new PropertyDescriptor<DPITexture, bool>(nameof(PremultAlpha), t => t.PremultAlpha, (t,v) => t.PremultAlpha=v, _ => false, stored: true),
        new PropertyDescriptor<DPITexture, Dictionary<Color,Color>>(nameof(ColorMap), t => t.ColorMap, (t,v) => t.ColorMap=v, _ => [], stored: true)]);
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_gate) InvalidateLocked(); base.Dispose(disposing); }
}
