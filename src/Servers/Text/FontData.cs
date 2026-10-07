using System.Buffers.Binary;
using System.Runtime.InteropServices;

namespace Electron2D;

internal readonly record struct FontMetrics(float Ascent, float Descent, float Height, float UnderlinePosition, float UnderlineThickness);
internal readonly record struct FontGlyph(Texture? Texture, Vector2 Offset, Vector2 Size, bool Colored = false, Rect2? Region = null);

// Immutable source bytes and portable glyph textures; mutable native state is confined to FontThread.
internal sealed partial class FontData : IDisposable
{
    private enum Operation { Create, Dispose, Metrics, Index, Advance, Shape, Glyph, Characters, Kerning }
    private readonly object _gate = new();
    private byte[] _bytes = [];
    private readonly Action _execute;
    private readonly int _faceIndex;
    private readonly FontInstance? _instance;
    internal Dictionary<uint, FontVariationAxis> VariationAxes { get; private set; } = [];
    internal Color[][] Palettes { get; private set; } = [];
    internal string[] PaletteNames { get; private set; } = [];
    private NativeFontPrecision? _precision;
    private bool _disposed, _retired;
    private int _readers;
    private readonly Dictionary<int, FontMetrics> _metrics = [];
    private readonly Dictionary<(uint Scalar, uint Selector), uint> _indices = [];
    private readonly Dictionary<(uint Glyph, int Size, bool Vertical), float> _advances = [];
    private readonly Dictionary<(uint Glyph, int Size, int Outline, float Oversampling, FontHinting Hinting, int Phase), FontGlyph> _glyphs = [];
    private string? _characters;
    private Operation _operation;
    private int _size, _outline, _phase, _offset, _count, _textLength;
    private uint _scalar, _selector, _glyph, _script, _indexResult;
    private Vector2i _pair;
    private Vector2 _kerningResult;
    private readonly Dictionary<(Vector2i Pair, int Size), Vector2> _kernings = [];
    private bool _vertical;
    private float _oversampling, _advanceResult;
    private string _language = string.Empty;
    private uint[]? _scalars;
    private NativeFontFeature[]? _features;
    private List<NativeShapedGlyph>? _shapeOutput;
    private FontMetrics _metricsResult;
    private FontGlyph _glyphResult;

    internal object Gate => _gate;
    private readonly bool _bitmapData;
    internal bool HasData => _bytes.Length > 0 || _bitmapData;
    internal string FamilyName { get; set; } = string.Empty;
    internal string StyleName { get; set; } = string.Empty;
    internal int FontWeight { get; set; } = 400;
    internal int FontStretch { get; set; } = 100;
    internal FontStyle FontStyle { get; set; }
    internal bool ModulateColorGlyphs { get; set; }
    internal float Oversampling { get; set; }
    internal FontHinting Hinting { get; set; } = FontHinting.Light;
    internal FontSubpixelPositioning SubpixelPositioning { get; set; } = FontSubpixelPositioning.Auto;
    internal bool KeepRoundingRemainders { get; set; } = true;
    internal NativeFontFeature[] OpenTypeFeatures { get; set; } = [];
    internal int FaceCount { get; private set; }
    internal int GlyphCount { get; private set; }
    internal int UnitsPerEm { get; private set; }

    internal FontData(byte[] immutableData, int faceIndex = 0, FontInstance? instance = null, FontCache? authored = null)
    {
        ArgumentNullException.ThrowIfNull(immutableData);
        _bitmapData = immutableData.Length == 0 && authored is { Sizes.Count: > 0 }; Authored = authored; _bytes = immutableData; _faceIndex = faceIndex; _instance = instance; _execute = Execute;
        if (HasData)
        {
            if ((!OperatingSystem.IsWindows() && !OperatingSystem.IsLinux() && !OperatingSystem.IsMacOS() && !OperatingSystem.IsAndroid() && !OperatingSystem.IsIOS() && !OperatingSystem.IsTvOS() && !OperatingSystem.IsBrowser()) ||
                RuntimeInformation.ProcessArchitecture is not (Architecture.X64 or Architecture.Arm64) && !(OperatingSystem.IsWindows() && RuntimeInformation.ProcessArchitecture == Architecture.X86) && !(OperatingSystem.IsAndroid() && RuntimeInformation.ProcessArchitecture is Architecture.X86 or Architecture.Arm) && !(OperatingSystem.IsBrowser() && RuntimeInformation.ProcessArchitecture == Architecture.Wasm))
                throw new PlatformNotSupportedException("Native font assets require a packaged desktop, mobile or browser runtime.");
            Run(Operation.Create);
        }
    }
    internal FontData CreateVariation(FontInstance instance)
    {
        lock (_gate)
        {
            Check(); var cache = Authored?.Clone();
            if (cache != null && _bytes.Length > 0 && !Authored!.Matches(instance)) cache.Sizes.Clear();
            var result = new FontData(_bytes, instance.FaceIndex, instance, cache);
            try
            {
                result.Hinting = Hinting; result.SubpixelPositioning = SubpixelPositioning; result.KeepRoundingRemainders = KeepRoundingRemainders; result.Oversampling = Oversampling; result.ModulateColorGlyphs = ModulateColorGlyphs;
                result.OpenTypeFeatures = OpenTypeFeatures; return result;
            }
            catch { result.Dispose(); throw; }
        }
    }
    private void Check() => ObjectDisposedException.ThrowIf(_disposed, this);
    internal bool TryAcquireRead()
    {
        lock (_gate) { if (_retired) return false; _readers = checked(_readers + 1); return true; }
    }
    internal void ReleaseRead()
    {
        lock (_gate)
        {
            if (_readers <= 0) throw new InvalidOperationException("Font data has no active reader.");
            _readers--;
            if (_retired && _readers == 0) DisposeNative();
        }
    }
    private void Run(Operation operation) { _operation = operation; FontThread.Invoke(_execute); }
    internal FontMetrics GetMetrics(int fontSize)
    {
        ValidateSize(fontSize);
        lock (_gate)
        {
            Check(); if (!HasData) return default;
            if (_metrics.TryGetValue(fontSize, out var metrics)) return metrics;
            _size = fontSize; Run(Operation.Metrics); _metricsResult = AuthoredMetrics(fontSize, _metricsResult); _metrics.Add(fontSize, _metricsResult); return _metricsResult;
        }
    }
    internal uint GetGlyphIndex(uint scalar, uint variationSelector = 0)
    {
        if (!System.Text.Rune.IsValid(scalar) || variationSelector != 0 && !System.Text.Rune.IsValid(variationSelector)) throw new ArgumentOutOfRangeException(nameof(scalar));
        lock (_gate)
        {
            Check(); if (!HasData) return 0;
            var key = (scalar, variationSelector); if (_indices.TryGetValue(key, out var glyph)) return glyph;
            _scalar = scalar; _selector = variationSelector; Run(Operation.Index); _indices.Add(key, _indexResult); return _indexResult;
        }
    }
    internal float GetGlyphAdvance(uint glyph, int fontSize, bool vertical = false)
    {
        ValidateSize(fontSize);
        lock (_gate)
        {
            Check(); if (!HasData) return 0;
            if (SelectAuthored(fontSize)?.Advances.TryGetValue(glyph, out var authoredAdvance) == true) return (vertical ? authoredAdvance.Y : authoredAdvance.X) * BitmapScale(fontSize);
            var key = (glyph, fontSize, vertical); if (_advances.TryGetValue(key, out var advance)) return advance;
            _glyph = glyph; _size = fontSize; _vertical = vertical; Run(Operation.Advance); _advances.Add(key, _advanceResult); return _advanceResult;
        }
    }
    internal string GetSupportedChars()
    {
        lock (_gate) { Check(); if (!HasData) return string.Empty; if (_characters is null) Run(Operation.Characters); return _characters!; }
    }
    internal void Shape(uint[] scalars, int offset, int count, int fontSize, NativeTextDirection direction, uint script, string language,
        List<NativeShapedGlyph> output, NativeFontFeature[]? features = null, int textLength = -1)
    {
        ArgumentNullException.ThrowIfNull(scalars); ArgumentNullException.ThrowIfNull(output); ArgumentNullException.ThrowIfNull(language); ValidateSize(fontSize);
        var length = textLength < 0 ? scalars.Length : textLength;
        if (length > scalars.Length || offset < 0 || count < 0 || offset > length - count) throw new ArgumentOutOfRangeException(nameof(count));
        lock (_gate)
        {
            Check(); output.Clear(); if (!HasData || count == 0) return;
            _scalars = scalars; _offset = offset; _count = count; _textLength = length; _size = fontSize; _direction = direction;
            _script = script; _language = language; _shapeOutput = output; _features = features ?? OpenTypeFeatures;
            try { Run(Operation.Shape); }
            finally { _scalars = null; _shapeOutput = null; _features = null; _language = string.Empty; }
        }
    }
    private NativeTextDirection _direction;
    internal FontGlyph GetGlyph(uint glyph, int fontSize, int outline, float oversampling) =>
        GetGlyph(glyph, fontSize, outline, oversampling, Vector2.Zero, out _);
    internal FontGlyph GetGlyph(uint glyph, int fontSize, int outline, float oversampling, Vector2 position, out Vector2 rasterPosition)
    {
        ValidateSize(fontSize);
        if (!float.IsFinite(oversampling)) throw new ArgumentOutOfRangeException(nameof(oversampling));
        if (!position.IsFinite()) throw new ArgumentException("Glyph position must be finite.", nameof(position));
        lock (_gate)
        {
            Check();
            if (TryAuthoredGlyph(glyph, fontSize, outline, out var authoredGlyph)) { rasterPosition = position; return authoredGlyph; }
            if (_bytes.Length == 0) { rasterPosition = position; return default; }
            var requested = oversampling > 0 ? oversampling : Oversampling > 0 ? Oversampling : 1;
            var factor = (int)(Math.Clamp(requested, .1f, 100f) * 64) / 64f;
            var border = Math.Max(0, outline);
            if (fontSize * (double)factor > 16384 || border * (double)factor > 16384)
                throw new ArgumentOutOfRangeException(nameof(oversampling), "Font raster dimensions exceed the canvas texture range.");
            var pixels = fontSize * factor;
            var divisions = SubpixelPositioning switch
            {
                FontSubpixelPositioning.Quarter => 4,
                FontSubpixelPositioning.Half => 2,
                FontSubpixelPositioning.Auto when pixels <= (int)FontSubpixelPositioning.QuarterMaxSize => 4,
                FontSubpixelPositioning.Auto when pixels <= (int)FontSubpixelPositioning.HalfMaxSize => 2,
                _ => 1
            };
            var x = (double)position.X + (divisions == 1 ? 0 : .5 / divisions);
            var phase = (int)(Math.Floor(divisions * x) - divisions * Math.Floor(x)) * (64 / divisions);
            rasterPosition = factor == 1 ? new((float)Math.Floor(x), MathF.Floor(position.Y)) : new((float)x, position.Y);
            if (!HasData) return default;
            var key = (glyph, fontSize, border, factor, Hinting, phase); if (_glyphs.TryGetValue(key, out var result)) return result;
            _glyph = glyph; _size = fontSize; _outline = border; _oversampling = factor; _phase = phase; Run(Operation.Glyph);
            _glyphs.Add(key, _glyphResult); return _glyphResult;
        }
    }
    private static void ValidateSize(int size) { if (size <= 0) throw new ArgumentOutOfRangeException(nameof(size)); }

    private void Execute()
    {
        switch (_operation)
        {
            case Operation.Create:
                try
                {
                    _precision = _bytes.Length > 0 ? new NativeFontPrecision(_bytes, _faceIndex) : new NativeFontPrecision(Authored!);
                    if (_instance != null && _bytes.Length > 0) _precision.ConfigureInstance(_instance);
                    VariationAxes = _precision.VariationAxes; Palettes = _precision.Palettes; PaletteNames = _precision.PaletteNames;
                    FamilyName = _precision.FamilyName; StyleName = _precision.StyleName; FaceCount = _precision.FaceCount; GlyphCount = _precision.GlyphCount; UnitsPerEm = _precision.UnitsPerEm;
                    FontStyle = (_precision.FaceStyleFlags & 2) != 0 ? FontStyle.Bold : 0;
                    if ((_precision.FaceStyleFlags & 1) != 0) FontStyle |= FontStyle.Italic;
                    if ((_precision.FaceFlags & 4) != 0) FontStyle |= FontStyle.FixedWidth;
                    var os2 = _precision.GetSFNTTable(0x4F532F32);
                    if (os2.Length >= 8)
                    {
                        FontWeight = BinaryPrimitives.ReadUInt16BigEndian(os2.AsSpan(4));
                        FontStretch = BinaryPrimitives.ReadUInt16BigEndian(os2.AsSpan(6)) switch { 1 => 50, 2 => 62, 3 => 75, 4 => 87, 6 => 112, 7 => 125, 8 => 150, 9 => 200, _ => 100 };
                    }
                }
                catch { ReleaseNative(); throw; }
                break;
            case Operation.Dispose: ReleaseNative(); break;
            case Operation.Metrics:
                _precision!.SetSize(_size); _metricsResult = new(_precision.Ascent, _precision.Descent, _precision.Height, _precision.UnderlinePosition, _precision.UnderlineThickness); break;
            case Operation.Index: _indexResult = _precision!.GetGlyphIndex(_scalar, _selector); break;
            case Operation.Advance:
                _precision!.SetSize(_size); _advanceResult = _precision.GetGlyphAdvance(_glyph, _vertical) / (_vertical ? -64f : 64f);
                if (!_vertical) _advanceResult += (_bytes.Length > 0 ? _instance?.Embolden ?? 0 : 0) * _size / 64f; break;
            case Operation.Kerning: _precision!.SetSize(_size); _kerningResult = _precision.GetKerning((uint)_pair.X, (uint)_pair.Y); break;
            case Operation.Characters: _characters = _precision!.GetSupportedChars(); break;
            case Operation.Shape:
                _precision!.SetSize(_size);
                var shaped = _precision.Shape(_scalars.AsSpan(0, _textLength), _offset, _count, _direction, _script, _language, _features);
                _shapeOutput!.EnsureCapacity(shaped.Length);
                var horizontal = _direction is NativeTextDirection.LTR or NativeTextDirection.RTL;
                var subpixel = horizontal && (SubpixelPositioning is FontSubpixelPositioning.Half or FontSubpixelPositioning.Quarter || SubpixelPositioning == FontSubpixelPositioning.Auto && _size <= (int)FontSubpixelPositioning.HalfMaxSize);
                double remainder = 0;
                foreach (var glyph in shaped)
                {
                    var scalar = _scalars![(int)glyph.Cluster];
                    if (scalar is 9 or 10 or 11 or 12 or 13 or 0x85 or 0x2028 or 0x2029 || System.Text.Rune.GetUnicodeCategory(new((int)scalar)) == System.Globalization.UnicodeCategory.SpaceSeparator) remainder = 0;
                    if (glyph.GlyphIndex == 0) { _shapeOutput.Add(glyph); continue; }
                    var x = subpixel ? glyph.XOffset : Round26(glyph.XOffset / 64d + (horizontal ? remainder : 0));
                    var y = Round26(glyph.YOffset / 64d + (horizontal ? 0 : remainder));
                    var advance = (horizontal ? glyph.XAdvance : glyph.YAdvance) / 64d + (horizontal ? (_bytes.Length > 0 ? _instance?.Embolden ?? 0 : 0) * _size / 64d : 0);
                    if (SelectAuthored(_size) is { } cache)
                    {
                        if (_bytes.Length > 0 && cache.Advances.TryGetValue(glyph.GlyphIndex, out var authoredAdvance)) advance = horizontal ? authoredAdvance.X : -authoredAdvance.Y;
                        if (_shapeOutput.Count > 0 && cache.Kerning.TryGetValue(new((int)_shapeOutput[^1].GlyphIndex, (int)glyph.GlyphIndex), out var kerning))
                        {
                            var previous = _shapeOutput[^1]; var delta = checked((int)Math.Round((horizontal ? kerning.X : -kerning.Y) * BitmapScale(_size) * 64));
                            _shapeOutput[^1] = horizontal ? previous with { XAdvance = previous.XAdvance + delta } : previous with { YAdvance = previous.YAdvance + delta };
                        }
                    }
                    var adjusted = subpixel ? checked((int)Math.Round(advance * 64)) : Round26(remainder + advance);
                    if (!subpixel && KeepRoundingRemainders) remainder += advance - adjusted / 64d;
                    var baseline = _instance?.BaselineOffset ?? 0; var shift = checked((int)Math.Round(baseline * _precision.Height * 64d));
                    _shapeOutput.Add(glyph with { XOffset = x + (horizontal ? 0 : shift), YOffset = y - (horizontal ? shift : 0), XAdvance = horizontal ? adjusted : glyph.XAdvance, YAdvance = horizontal ? glyph.YAdvance : adjusted });
                }
                break;
            case Operation.Glyph: _glyphResult = RasterGlyph(); break;
        }
    }
    private FontGlyph RasterGlyph()
    {
        _precision!.SetSize(_size * _oversampling);
        var raster = _precision.Rasterize(_glyph, Hinting, _phase, checked((int)(_outline * _oversampling) * 16));
        if (raster.Width == 0 || raster.Height == 0) return default;
        if (Authored != null && _oversampling == 1 && _phase == 0) return CacheRaster(_glyph, new(_size, _outline), raster);
        using var image = Image.CreateFromData(raster.Width, raster.Height, false, Image.Format.Rgba8, raster.Pixels);
        var texture = ImageTexture.CreateFromImage(image); texture.RetainRendererCache = true;
        var result = new FontGlyph(texture, new Vector2(raster.Left, -raster.Top) / _oversampling,
            new Vector2(raster.Width, raster.Height) / _oversampling, raster.Colored);
        return result;
    }
    private static int Round26(double value) => checked((int)Math.Round(value, MidpointRounding.AwayFromZero) * 64);
    private void ReleaseNative()
    {
        List<Exception>? errors = null;
        // Recorded canvas commands retain immutable CPU pixels independently of the native face.
        // Retired snapshots follow ordinary renderer eviction and need no native font ownership.
        foreach (var glyph in _glyphs.Values) if (glyph.Texture is { } texture) texture.RetainRendererCache = false;
        Authored?.RetireTextures(); _glyphs.Clear();
        try { _precision?.Dispose(); }
        catch (Exception error) { (errors ??= []).Add(error); }
        finally
        {
            _precision = null; _bytes = []; _characters = null; _glyphResult = default;
            _metrics.Clear(); _indices.Clear(); _advances.Clear();
        }
        if (errors is not null) throw new AggregateException("Font native cleanup failed.", errors);
    }
    public void Dispose()
    {
        lock (_gate) { if (_retired) return; _retired = true; if (_readers == 0) DisposeNative(); }
        GC.SuppressFinalize(this);
    }
    private void DisposeNative()
    {
        if (_disposed) return;
        _disposed = true;
        if (HasData) Run(Operation.Dispose);
    }
    ~FontData() { if (!_disposed && HasData && !Environment.HasShutdownStarted) try { FontThread.Invoke(ExecuteDispose); } catch { } }
    private void ExecuteDispose() => ReleaseNative();
}
