using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Controls glyph raster hinting independently of shaping advances.</summary>
public enum FontHinting
{
    /// <summary>Does not hint glyph outlines.</summary>
    None = 0,
    /// <summary>Uses light vertical hinting.</summary>
    Light = 1,
    /// <summary>Uses normal grid fitting.</summary>
    Normal = 2
}

/// <summary>Controls horizontal subpixel positioning and advance rounding.</summary>
public enum FontSubpixelPositioning
{
    /// <summary>Uses whole-pixel positions.</summary>
    Disabled = 0,
    /// <summary>Uses quarter pixels through size sixteen, half pixels through twenty and whole pixels above twenty.</summary>
    Auto = 1,
    /// <summary>Uses half-pixel positions.</summary>
    Half = 2,
    /// <summary>Uses quarter-pixel positions.</summary>
    Quarter = 3,
    /// <summary>Defines the largest size that automatic positioning renders at half-pixel precision; this is a threshold, not a positioning mode.</summary>
    HalfMaxSize = 20,
    /// <summary>Defines the largest size that automatic positioning renders at quarter-pixel precision; this is a threshold, not a positioning mode.</summary>
    QuarterMaxSize = 16
}

/// <summary>Owns scalable font bytes, shaping configuration and independent native glyph caches.</summary>
/// <remarks>Data and file replacement validate a new face before committing it. Successful data or OpenType
/// replacement invalidates cached text and emits Changed; metadata-only assignments are silent.
/// Fallback resources remain borrowed. A newly constructed empty font does not initialize native libraries.</remarks>
public class FontFile : Font
{
    private const int MaximumFontBytes = 64 * 1024 * 1024;
    private byte[] _bytes = [];
    private FontData? _fontData = new([]);
    private bool _deferred;
    private Dictionary<string, int> _featureOverrides = new(StringComparer.Ordinal);
    internal override FontData? PrimaryData
    {
        get { lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); return _fontData; } }
    }
    private static readonly PropertyDescriptor[] FileProperties =
    [
        new PropertyDescriptor<FontFile, byte[]>(nameof(Data), font => font.Data, (font, value) => font.Data = value, _ => [], stored: true),
        new PropertyDescriptor<FontFile, string>(nameof(FontName), font => font.FontName, (font, value) => font.FontName = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<FontFile, string>(nameof(StyleName), font => font.StyleName, (font, value) => font.StyleName = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<FontFile, FontStyle>(nameof(FontStyle), font => font.FontStyle, (font, value) => font.FontStyle = value, _ => global::Electron2D.FontStyle.None, stored: true),
        new PropertyDescriptor<FontFile, int>(nameof(FontWeight), font => font.FontWeight, (font, value) => font.FontWeight = value, _ => 400, stored: true),
        new PropertyDescriptor<FontFile, int>(nameof(FontStretch), font => font.FontStretch, (font, value) => font.FontStretch = value, _ => 100, stored: true),
        new PropertyDescriptor<FontFile, FontHinting>(nameof(Hinting), font => font.Hinting, (font, value) => font.Hinting = value, _ => FontHinting.Light, stored: true),
        new PropertyDescriptor<FontFile, FontSubpixelPositioning>(nameof(SubpixelPositioning), font => font.SubpixelPositioning, (font, value) => font.SubpixelPositioning = value, _ => FontSubpixelPositioning.Auto, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(KeepRoundingRemainders), font => font.KeepRoundingRemainders, (font, value) => font.KeepRoundingRemainders = value, _ => true, stored: true),
        new PropertyDescriptor<FontFile, float>(nameof(Oversampling), font => font.Oversampling, (font, value) => font.Oversampling = value, _ => 0, stored: true),
        new PropertyDescriptor<FontFile, bool>(nameof(ModulateColorGlyphs), font => font.ModulateColorGlyphs, (font, value) => font.ModulateColorGlyphs = value, _ => false, stored: true),
        new PropertyDescriptor<FontFile, Dictionary<string, int>>(nameof(OpenTypeFeatureOverrides), font => font.OpenTypeFeatureOverrides, (font, value) => font.OpenTypeFeatureOverrides = value, _ => new(StringComparer.Ordinal), stored: true)
    ];

    /// <summary>Creates an empty font with light hinting, automatic subpixel positioning and remainder preservation.</summary>
    public FontFile() { }

    internal FontFile(byte[] immutableEmbeddedData)
    {
        ArgumentNullException.ThrowIfNull(immutableEmbeddedData);
        if (immutableEmbeddedData.Length > MaximumFontBytes) throw new InvalidDataException("Font data exceeds sixty-four MiB.");
        _bytes = (byte[])immutableEmbeddedData.Clone(); _deferred = _bytes.Length != 0;
    }

    private void EnsureLoadedLocked()
    {
        if (!_deferred) return;
        var replacement = CreateData(_bytes);
        try { CopyConfiguration(_fontData!, replacement); }
        catch { replacement.Dispose(); throw; }
        var previous = _fontData;
        _fontData = replacement; _deferred = false;
        previous!.Dispose(); // Deferred state owns an empty, non-native FontData.
    }

    /// <summary>Gets a copied font source or atomically replaces it with copied, validated scalable font data.</summary>
    /// <value>Empty initially. At most sixty-four MiB; an empty assignment clears the face while preserving configuration.</value>
    /// <remarks>Nonempty bytes must contain a supported SFNT font, including decompressed TTF/OTF/TTC or WOFF/WOFF2.
    /// Replacement refreshes intrinsic metadata. Failure preserves all previous state; success emits Changed even for equal bytes.
    /// Active readers retain the old native face until they finish; recorded glyph pixels remain independent canvas snapshots.</remarks>
    /// <exception cref="ArgumentNullException">The supplied array is null.</exception>
    /// <exception cref="InvalidDataException">The source is malformed, unsupported or exceeds the size limit.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public byte[] Data
    {
        get { lock (FontGate) { ThrowIfDisposed(); return (byte[])_bytes.Clone(); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value); ThrowIfDisposed();
            if (value.Length > MaximumFontBytes) throw new InvalidDataException("Font data exceeds sixty-four MiB.");
            ReplaceData((byte[])value.Clone(), false);
        }
    }

    /// <summary>Gets or overrides the face family name without changing glyph shapes.</summary>
    /// <value>The native family name after loading, or an empty name before loading. Assignments are silent.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string FontName
    {
        get => Read(static data => data.FamilyName, true);
        set { ArgumentNullException.ThrowIfNull(value); lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); _fontData!.FamilyName = value; } }
    }
    /// <summary>Gets or overrides the face style name without changing glyph shapes.</summary>
    /// <value>The native style name after loading; initially empty. Assignments are silent.</value>
    /// <exception cref="ArgumentNullException">The name is null.</exception><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string StyleName
    {
        get => Read(static data => data.StyleName, true);
        set { ArgumentNullException.ThrowIfNull(value); lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); _fontData!.StyleName = value; } }
    }
    /// <summary>Gets or overrides descriptive style flags without synthesizing bold or italic glyphs.</summary>
    /// <value>Intrinsic flags after loading; initially None. Numeric flag values are preserved and writes are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontStyle FontStyle
    {
        get => Read(static data => data.FontStyle, true);
        set { lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); _fontData!.FontStyle = value; } }
    }
    /// <summary>Gets or overrides the descriptive weight class.</summary>
    /// <value>Intrinsic weight after loading; initially four hundred. Assignments clamp to one hundred through nine hundred ninety-nine and are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int FontWeight
    {
        get => Read(static data => data.FontWeight, true);
        set { lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); _fontData!.FontWeight = Math.Clamp(value, 100, 999); } }
    }
    /// <summary>Gets or overrides the descriptive width percentage.</summary>
    /// <value>Intrinsic stretch after loading; initially one hundred. Assignments clamp to fifty through two hundred and are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int FontStretch
    {
        get => Read(static data => data.FontStretch, true);
        set { lock (FontGate) { ThrowIfDisposed(); EnsureLoadedLocked(); _fontData!.FontStretch = Math.Clamp(value, 50, 200); } }
    }
    /// <summary>Gets or sets glyph raster hinting.</summary>
    /// <value>Light initially. Unknown numeric values are retained and use normal hinting; equal assignments are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontHinting Hinting
    {
        get => Read(static data => data.Hinting);
        set => SetConfiguration(static data => data.Hinting, static (data, setting) => data.Hinting = setting, value);
    }
    /// <summary>Gets or sets horizontal subpixel positioning.</summary>
    /// <value>Auto initially, Disabled after successful file loading or ResetState. Unknown numeric values are retained and use whole pixels; equal assignments are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontSubpixelPositioning SubpixelPositioning
    {
        get => Read(static data => data.SubpixelPositioning);
        set => SetConfiguration(static data => data.SubpixelPositioning, static (data, setting) => data.SubpixelPositioning = setting, value);
    }
    /// <summary>Gets or sets whether whole-pixel advance rounding carries the residual error across glyphs.</summary>
    /// <value>True initially. Equal assignments are silent.</value>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool KeepRoundingRemainders
    {
        get => Read(static data => data.KeepRoundingRemainders);
        set => SetConfiguration(static data => data.KeepRoundingRemainders, static (data, setting) => data.KeepRoundingRemainders = setting, value);
    }
    /// <summary>Gets or sets the finite font raster oversampling override.</summary>
    /// <value>Zero initially. A positive value supplies automatic draw requests; an explicit positive per-draw factor takes precedence. Nonpositive values retain the default policy.</value>
    /// <exception cref="ArgumentOutOfRangeException">The factor is nonfinite.</exception><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float Oversampling
    {
        get => Read(static data => data.Oversampling);
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            SetConfiguration(static data => data.Oversampling, static (data, setting) => data.Oversampling = setting, value);
        }
    }
    /// <summary>Gets or sets whether intrinsic color glyphs also receive the requested text RGB modulation.</summary>
    /// <value>False initially. Alpha modulation always applies; equal assignments are silent.</value>
    /// <remarks>Outline drawing always preserves intrinsic glyph RGB, regardless of this setting.</remarks>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool ModulateColorGlyphs
    {
        get => Read(static data => data.ModulateColorGlyphs);
        set => SetConfiguration(static data => data.ModulateColorGlyphs, static (data, setting) => data.ModulateColorGlyphs = setting, value);
    }
    /// <summary>Gets or replaces typed OpenType feature overrides.</summary>
    /// <value>An independent dictionary snapshot, initially empty. Keys accept registered readable aliases or custom tag strings. Negative values are stored but omitted from shaping; zero disables boolean features.</value>
    /// <remarks>Assignments copy the dictionary, invalidate shaped text and emit Changed. Feature settings survive file loading and ResetState.
    /// Unknown names have every custom_ segment removed, use their first four ASCII scalars and pad short tags with spaces; non-ASCII scalars become spaces and NUL terminates the tag.</remarks>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public Dictionary<string, int> OpenTypeFeatureOverrides
    {
        get { lock (FontGate) { ThrowIfDisposed(); return new(_featureOverrides, StringComparer.Ordinal); } }
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            var snapshot = new Dictionary<string, int>(value, StringComparer.Ordinal);
            var compiled = CompileFeatures(snapshot);
            lock (FontGate)
            {
                ThrowIfDisposed();
                lock (_fontData!.Gate) { _featureOverrides = snapshot; _fontData.OpenTypeFeatures = compiled; InvalidateFontStateLocked(); }
            }
            EmitChanged();
        }
    }

    /// <summary>Atomically loads a scalable font from an operating-system, res:// or user:// file.</summary>
    /// <param name="path">A supported dynamic-font source file.</param>
    /// <remarks>Input is bounded to sixty-four MiB. Success resets hinting to Light, subpixel positioning to Disabled,
    /// remainder preservation to true, modulation to false and oversampling to zero; feature overrides and fallbacks remain.
    /// Read or decode failures preserve the previous font. Success emits Changed after the replacement is committed.</remarks>
    /// <exception cref="InvalidDataException">The file is empty, malformed, unsupported or exceeds the size limit.</exception>
    /// <exception cref="IOException">The source cannot be read or changes size while being read.</exception>
    /// <exception cref="ObjectDisposedException">The font is disposed.</exception>
    public void LoadDynamicFont(string path)
    {
        ThrowIfDisposed();
        using var file = FileAccess.Open(path, FileAccessModeFlags.Read);
        var length = file.Length;
        if (length is <= 0 or > MaximumFontBytes) throw new InvalidDataException("Font files must contain between one byte and sixty-four MiB.");
        var bytes = file.ReadBytes((int)length);
        if (bytes.Length != length || file.Length != length) throw new IOException("The font file changed while being read.");
        ReplaceData(bytes, true);
    }

    private T Read<T>(Func<FontData, T> getter, bool metadata = false)
    {
        lock (FontGate) { ThrowIfDisposed(); if (metadata) EnsureLoadedLocked(); return getter(_fontData!); }
    }
    private void SetConfiguration<T>(Func<FontData, T> getter, Action<FontData, T> setter, T value)
    {
        lock (FontGate)
        {
            ThrowIfDisposed();
            lock (_fontData!.Gate)
            {
                if (EqualityComparer<T>.Default.Equals(getter(_fontData), value)) return;
                setter(_fontData, value); InvalidateFontStateLocked();
            }
        }
        EmitChanged();
    }
    private static NativeFontFeature[] CompileFeatures(Dictionary<string, int> features)
    {
        var result = new NativeFontFeature[features.Count(static pair => pair.Value >= 0)]; var index = 0;
        foreach (var pair in features)
            if (pair.Value >= 0) result[index++] = new(OpenTypeFeatureTags.Resolve(pair.Key), (uint)pair.Value);
        return result;
    }

    private static FontData CreateData(byte[] bytes)
    {
        try { return new FontData(bytes); }
        catch (Exception error) when (error is InvalidOperationException or ArgumentException || error is NotSupportedException and not PlatformNotSupportedException)
        { throw new InvalidDataException("The font source could not be decoded.", error); }
    }
    private void ReplaceData(byte[] bytes, bool reset)
    {
        var replacement = CreateData(bytes); FontData? previous = null;
        try
        {
            lock (FontGate)
            {
                ThrowIfDisposed();
                if (!reset) CopyConfiguration(_fontData!, replacement);
                else replacement.SubpixelPositioning = FontSubpixelPositioning.Disabled;
                replacement.OpenTypeFeatures = _fontData!.OpenTypeFeatures;
                previous = _fontData; _fontData = replacement; _bytes = bytes; _deferred = false; InvalidateFontStateLocked();
            }
        }
        catch { replacement.Dispose(); throw; }
        FinishReplacement(previous);
    }
    private static void CopyConfiguration(FontData source, FontData target)
    {
        target.Hinting = source.Hinting; target.SubpixelPositioning = source.SubpixelPositioning;
        target.KeepRoundingRemainders = source.KeepRoundingRemainders; target.Oversampling = source.Oversampling;
        target.ModulateColorGlyphs = source.ModulateColorGlyphs; target.OpenTypeFeatures = source.OpenTypeFeatures;
    }
    private void FinishReplacement(FontData? previous)
    {
        Exception? failure = null;
        try { EmitChanged(); } catch (Exception error) { failure = error; }
        try { previous?.Dispose(); }
        catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(FileProperties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(FontFile) ? new FontFile() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        byte[] bytes; Dictionary<string, int> features; bool deferred;
        (FontHinting Hinting, FontSubpixelPositioning Subpixel, bool Remainders, float Oversampling, bool Modulate,
            string Name, string Style, FontStyle Flags, int Weight, int Stretch) settings;
        lock (FontGate)
        {
            ThrowIfDisposed(); bytes = (byte[])_bytes.Clone(); features = new(_featureOverrides, StringComparer.Ordinal); deferred = _deferred;
            var data = _fontData!;
            settings = (data.Hinting, data.SubpixelPositioning, data.KeepRoundingRemainders, data.Oversampling,
                data.ModulateColorGlyphs, data.FamilyName, data.StyleName, data.FontStyle, data.FontWeight, data.FontStretch);
        }
        var compiledFeatures = CompileFeatures(features);
        var replacement = deferred ? new FontData([]) : CreateData(bytes);
        var copy = (FontFile)target; FontData? previous;
        try
        {
            replacement.Hinting = settings.Hinting; replacement.SubpixelPositioning = settings.Subpixel;
            replacement.KeepRoundingRemainders = settings.Remainders; replacement.Oversampling = settings.Oversampling;
            replacement.ModulateColorGlyphs = settings.Modulate; replacement.OpenTypeFeatures = compiledFeatures;
            replacement.FamilyName = settings.Name; replacement.StyleName = settings.Style; replacement.FontStyle = settings.Flags;
            replacement.FontWeight = settings.Weight; replacement.FontStretch = settings.Stretch;
            base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
            lock (copy.FontGate)
            {
                copy.ThrowIfDisposed(); previous = copy._fontData; copy._fontData = replacement; copy._bytes = bytes;
                copy._featureOverrides = features; copy._deferred = deferred; copy.InvalidateFontStateLocked();
            }
        }
        catch { replacement.Dispose(); throw; }
        copy.FinishReplacement(previous);
    }
    /// <inheritdoc />
    protected override void OnResetState()
    {
        ReplaceData([], true);
        base.OnResetState();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        FontData? data;
        lock (FontGate) { data = _fontData; _fontData = null; _bytes = []; _deferred = false; _featureOverrides.Clear(); }
        try { data?.Dispose(); }
        finally { base.Dispose(disposing); }
    }
}
