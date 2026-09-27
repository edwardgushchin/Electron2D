namespace Electron2D;

/// <summary>Stores shared font, spacing, outline and shadow settings for text labels.</summary>
/// <remarks>Scalar and layer-value assignments suppress equal writes. Structural layer changes notify the
/// property list before Changed. Font references are borrowed; their changes and disposal are forwarded without
/// replacing the stored identity. State is protected by an instance lock and observers run outside that lock.
/// Signed sizes and spacing are retained; rendering consumers apply their own font and geometry requirements.</remarks>
public class LabelSettings : Resource
{
    private readonly object _gate = new();
    private long _revision;
    internal long ContentRevision => Volatile.Read(ref _revision);
    private Font? _font;
    private float _lineSpacing = 3, _paragraphSpacing;
    private int _fontSize = 16, _outlineSize, _shadowSize = 1;
    private Color _fontColor = Colors.White, _outlineColor = Colors.White, _shadowColor = Colors.Transparent;
    private Vector2 _shadowOffset = Vector2.One;
    internal readonly record struct Outline(int Size, Color Color);
    internal readonly record struct Shadow(Vector2 Offset, Color Color, int OutlineSize);
    private static readonly Outline DefaultOutline = new(0, Colors.Black);
    private static readonly Shadow DefaultShadow = new(Vector2.One, Colors.Black, 0);
    private readonly List<Outline> _outlines = [];
    private readonly List<Shadow> _shadows = [];
    private readonly Action<Resource> _fontChanged;
    private readonly Action<ElectronObject> _fontDisposed;
    private static readonly PropertyDescriptor[] SettingsProperties =
    [
        new PropertyDescriptor<LabelSettings, float>(nameof(LineSpacing), settings => settings.LineSpacing, (settings, value) => settings.LineSpacing = value, _ => 3, stored: true),
        new PropertyDescriptor<LabelSettings, float>(nameof(ParagraphSpacing), settings => settings.ParagraphSpacing, (settings, value) => settings.ParagraphSpacing = value, _ => 0, stored: true),
        new PropertyDescriptor<LabelSettings, Font?>(nameof(Font), settings => settings.Font, (settings, value) => settings.Font = value, _ => null, stored: true),
        new PropertyDescriptor<LabelSettings, int>(nameof(FontSize), settings => settings.FontSize, (settings, value) => settings.FontSize = value, _ => 16, stored: true),
        new PropertyDescriptor<LabelSettings, Color>(nameof(FontColor), settings => settings.FontColor, (settings, value) => settings.FontColor = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<LabelSettings, int>(nameof(OutlineSize), settings => settings.OutlineSize, (settings, value) => settings.OutlineSize = value, _ => 0, stored: true),
        new PropertyDescriptor<LabelSettings, Color>(nameof(OutlineColor), settings => settings.OutlineColor, (settings, value) => settings.OutlineColor = value, _ => Colors.White, stored: true),
        new PropertyDescriptor<LabelSettings, int>(nameof(ShadowSize), settings => settings.ShadowSize, (settings, value) => settings.ShadowSize = value, _ => 1, stored: true),
        new PropertyDescriptor<LabelSettings, Color>(nameof(ShadowColor), settings => settings.ShadowColor, (settings, value) => settings.ShadowColor = value, _ => Colors.Transparent, stored: true),
        new PropertyDescriptor<LabelSettings, Vector2>(nameof(ShadowOffset), settings => settings.ShadowOffset, (settings, value) => settings.ShadowOffset = value, _ => Vector2.One, stored: true),
        new PropertyDescriptor<LabelSettings, int>(nameof(StackedOutlineCount), settings => settings.StackedOutlineCount, (settings, value) => settings.StackedOutlineCount = value, _ => 0, stored: true),
        new PropertyDescriptor<LabelSettings, int>(nameof(StackedShadowCount), settings => settings.StackedShadowCount, (settings, value) => settings.StackedShadowCount = value, _ => 0, stored: true)
    ];

    /// <summary>Creates default sixteen-unit white text settings with no stacked effects.</summary>
    public LabelSettings() { _fontChanged = FontChanged; _fontDisposed = FontDisposed; }
    /// <summary>Gets or sets the finite signed gap added between lines.</summary>
    /// <value>Three initially; equal writes are silent.</value>
    /// <exception cref="ArgumentOutOfRangeException">The spacing is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public float LineSpacing { get => Read(ref _lineSpacing); set => Set(ref _lineSpacing, value, ValidateFloat); }
    /// <summary>Gets or sets the finite signed additional paragraph gap.</summary>
    /// <value>Zero initially; equal writes are silent.</value>
    /// <exception cref="ArgumentOutOfRangeException">The spacing is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public float ParagraphSpacing { get => Read(ref _paragraphSpacing); set => Set(ref _paragraphSpacing, value, ValidateFloat); }
    /// <summary>Gets or sets the borrowed font override.</summary>
    /// <value>Null initially. Equal identities are silent; replacing the font updates subscriptions before Changed.</value>
    /// <exception cref="ObjectDisposedException">These settings or the assigned font are disposed.</exception>
    public Font? Font
    {
        get => Read(ref _font);
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed(); ValidateFont(value); if (ReferenceEquals(_font, value)) return; ReplaceFont(value);
            }
            PublishChanged();
        }
    }
    /// <summary>Gets or sets the stored signed font size.</summary><value>Sixteen initially; equal writes are silent.</value>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int FontSize { get => Read(ref _fontSize); set => Set(ref _fontSize, value); }
    /// <summary>Gets or sets the finite text color.</summary><value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Color FontColor { get => Read(ref _fontColor); set => Set(ref _fontColor, value, ValidateColor); }
    /// <summary>Gets or sets the stored signed primary outline size.</summary><value>Zero initially.</value>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int OutlineSize { get => Read(ref _outlineSize); set => Set(ref _outlineSize, value); }
    /// <summary>Gets or sets the finite primary outline color.</summary><value>Opaque white initially.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Color OutlineColor { get => Read(ref _outlineColor); set => Set(ref _outlineColor, value, ValidateColor); }
    /// <summary>Gets or sets the stored signed primary shadow outline size.</summary><value>One initially.</value>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int ShadowSize { get => Read(ref _shadowSize); set => Set(ref _shadowSize, value); }
    /// <summary>Gets or sets the finite primary shadow color.</summary><value>Transparent black initially.</value>
    /// <exception cref="ArgumentException">A color component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Color ShadowColor { get => Read(ref _shadowColor); set => Set(ref _shadowColor, value, ValidateColor); }
    /// <summary>Gets or sets the finite signed primary shadow offset.</summary><value>One unit on each axis initially.</value>
    /// <exception cref="ArgumentException">An offset component is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Vector2 ShadowOffset { get => Read(ref _shadowOffset); set => Set(ref _shadowOffset, value, ValidateVector); }
    /// <summary>Gets or sets the number of stacked outlines, initializing new layers to size zero and opaque black.</summary>
    /// <value>Zero initially; equal counts are silent. Shrinking discards removed layers.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int StackedOutlineCount { get { lock (_gate) { ThrowIfDisposed(); return _outlines.Count; } } set => Resize(_outlines, value, DefaultOutline); }
    /// <summary>Gets or sets the number of stacked shadows, initializing new layers to offset (1,1), opaque black and outline size zero.</summary>
    /// <value>Zero initially; equal counts are silent. Shrinking discards removed layers.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int StackedShadowCount { get { lock (_gate) { ThrowIfDisposed(); return _shadows.Count; } } set => Resize(_shadows, value, DefaultShadow); }

    /// <summary>Inserts a default outline layer and emits structural notifications.</summary>
    /// <param name="index">An insertion position from zero through Count; any negative value appends.</param>
    /// <exception cref="ArgumentOutOfRangeException">The insertion position exceeds Count.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void AddStackedOutline(int index = -1) => Insert(_outlines, index, DefaultOutline);
    /// <summary>Inserts a default shadow layer and emits structural notifications.</summary>
    /// <param name="index">An insertion position from zero through Count; any negative value appends.</param>
    /// <exception cref="ArgumentOutOfRangeException">The insertion position exceeds Count.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void AddStackedShadow(int index = -1) => Insert(_shadows, index, DefaultShadow);
    /// <summary>Moves an outline to an insertion position and notifies even when the resulting order is unchanged.</summary>
    /// <param name="fromIndex">The existing layer index.</param><param name="toPosition">An insertion position from zero through Count, measured before removal.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside its allowed range.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void MoveStackedOutline(int fromIndex, int toPosition) => Move(_outlines, fromIndex, toPosition);
    /// <summary>Moves a shadow to an insertion position and notifies even when the resulting order is unchanged.</summary>
    /// <param name="fromIndex">The existing layer index.</param><param name="toPosition">An insertion position from zero through Count, measured before removal.</param>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside its allowed range.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void MoveStackedShadow(int fromIndex, int toPosition) => Move(_shadows, fromIndex, toPosition);
    /// <summary>Removes an outline layer and emits structural notifications.</summary>
    /// <param name="index">The existing layer index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void RemoveStackedOutline(int index) => Remove(_outlines, index);
    /// <summary>Removes a shadow layer and emits structural notifications.</summary>
    /// <param name="index">The existing layer index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception>
    /// <exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void RemoveStackedShadow(int index) => Remove(_shadows, index);
    /// <summary>Gets a stacked outline's signed size.</summary><param name="index">The existing layer index.</param><returns>The stored size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int GetStackedOutlineSize(int index) { lock (_gate) { ThrowIfDisposed(); return _outlines[LayerIndex(index, _outlines.Count)].Size; } }
    /// <summary>Sets a stacked outline's signed size, suppressing equal writes.</summary><param name="index">The existing layer index.</param><param name="size">The stored signed size.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void SetStackedOutlineSize(int index, int size)
    {
        lock (_gate) { ThrowIfDisposed(); var layer = _outlines[LayerIndex(index, _outlines.Count)]; if (layer.Size == size) return; _outlines[index] = layer with { Size = size }; }
        PublishChanged();
    }
    /// <summary>Gets a stacked outline's color.</summary><param name="index">The existing layer index.</param><returns>The stored color.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Color GetStackedOutlineColor(int index) { lock (_gate) { ThrowIfDisposed(); return _outlines[LayerIndex(index, _outlines.Count)].Color; } }
    /// <summary>Sets a stacked outline's finite color, suppressing equal writes.</summary><param name="index">The existing layer index.</param><param name="color">The finite color.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ArgumentException">The color is nonfinite.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void SetStackedOutlineColor(int index, Color color)
    {
        lock (_gate) { ThrowIfDisposed(); var layer = _outlines[LayerIndex(index, _outlines.Count)]; ValidateColor(color); if (layer.Color == color) return; _outlines[index] = layer with { Color = color }; }
        PublishChanged();
    }
    /// <summary>Gets a stacked shadow's finite offset.</summary><param name="index">The existing layer index.</param><returns>The stored offset.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Vector2 GetStackedShadowOffset(int index) { lock (_gate) { ThrowIfDisposed(); return _shadows[LayerIndex(index, _shadows.Count)].Offset; } }
    /// <summary>Sets a stacked shadow's finite offset, suppressing equal writes.</summary><param name="index">The existing layer index.</param><param name="offset">The finite signed offset.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ArgumentException">The offset is nonfinite.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void SetStackedShadowOffset(int index, Vector2 offset)
    {
        lock (_gate) { ThrowIfDisposed(); var layer = _shadows[LayerIndex(index, _shadows.Count)]; ValidateVector(offset); if (layer.Offset == offset) return; _shadows[index] = layer with { Offset = offset }; }
        PublishChanged();
    }
    /// <summary>Gets a stacked shadow's color.</summary><param name="index">The existing layer index.</param><returns>The stored color.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public Color GetStackedShadowColor(int index) { lock (_gate) { ThrowIfDisposed(); return _shadows[LayerIndex(index, _shadows.Count)].Color; } }
    /// <summary>Sets a stacked shadow's finite color, suppressing equal writes.</summary><param name="index">The existing layer index.</param><param name="color">The finite color.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ArgumentException">The color is nonfinite.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void SetStackedShadowColor(int index, Color color)
    {
        lock (_gate) { ThrowIfDisposed(); var layer = _shadows[LayerIndex(index, _shadows.Count)]; ValidateColor(color); if (layer.Color == color) return; _shadows[index] = layer with { Color = color }; }
        PublishChanged();
    }
    /// <summary>Gets a stacked shadow's signed outline size.</summary><param name="index">The existing layer index.</param><returns>The stored signed size.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public int GetStackedShadowOutlineSize(int index) { lock (_gate) { ThrowIfDisposed(); return _shadows[LayerIndex(index, _shadows.Count)].OutlineSize; } }
    /// <summary>Sets a stacked shadow's signed outline size, suppressing equal writes.</summary><param name="index">The existing layer index.</param><param name="size">The signed size.</param>
    /// <exception cref="ArgumentOutOfRangeException">The layer does not exist.</exception><exception cref="ObjectDisposedException">The settings are disposed.</exception>
    public void SetStackedShadowOutlineSize(int index, int size)
    {
        lock (_gate) { ThrowIfDisposed(); var layer = _shadows[LayerIndex(index, _shadows.Count)]; if (layer.OutlineSize == size) return; _shadows[index] = layer with { OutlineSize = size }; }
        PublishChanged();
    }

    private T Read<T>(ref T field) { lock (_gate) { ThrowIfDisposed(); return field; } }
    private void Set<T>(ref T field, T value, Action<T>? validate = null)
    {
        lock (_gate) { ThrowIfDisposed(); validate?.Invoke(value); if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; }
        PublishChanged();
    }
    private static void ValidateFloat(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value), "Spacing must be finite."); }
    private static void ValidateColor(Color value) { if (!value.IsFinite()) throw new ArgumentException("Colors must be finite.", nameof(value)); }
    private static void ValidateVector(Vector2 value) { if (!value.IsFinite()) throw new ArgumentException("Offsets must be finite.", nameof(value)); }
    private static void ValidateFont(Font? value) { if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(Font)); }
    private static int LayerIndex(int index, int count) => (uint)index < (uint)count ? index : throw new ArgumentOutOfRangeException(nameof(index));
    private void Resize<T>(List<T> layers, int count, T initial)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); ArgumentOutOfRangeException.ThrowIfNegative(count); if (layers.Count == count) return;
            if (count < layers.Count) layers.RemoveRange(count, layers.Count - count);
            else { layers.EnsureCapacity(count); while (layers.Count < count) layers.Add(initial); }
        }
        PublishStructure();
    }
    private void Insert<T>(List<T> layers, int index, T initial)
    {
        lock (_gate) { ThrowIfDisposed(); if (index < 0) index = layers.Count; if (index > layers.Count) throw new ArgumentOutOfRangeException(nameof(index)); layers.Insert(index, initial); }
        PublishStructure();
    }
    private void Move<T>(List<T> layers, int fromIndex, int toPosition)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); LayerIndex(fromIndex, layers.Count); if ((uint)toPosition > (uint)layers.Count) throw new ArgumentOutOfRangeException(nameof(toPosition));
            var layer = layers[fromIndex]; var target = toPosition > fromIndex ? toPosition - 1 : toPosition;
            if (target != fromIndex) { layers.RemoveAt(fromIndex); layers.Insert(target, layer); }
        }
        PublishStructure();
    }
    private void Remove<T>(List<T> layers, int index) { lock (_gate) { ThrowIfDisposed(); layers.RemoveAt(LayerIndex(index, layers.Count)); } PublishStructure(); }
    private void PublishStructure()
    {
        Exception? listError = null, changeError = null;
        try { NotifyPropertyListChanged(); } catch (Exception error) { listError = error; }
        try { PublishChanged(); } catch (Exception error) { changeError = error; }
        ThrowCombined(listError, changeError);
    }
    private void ReplaceFont(Font? font)
    {
        if (_font is not null) { _font.Changed -= _fontChanged; _font.Disposed -= _fontDisposed; }
        _font = font;
        if (_font is not null) { _font.Changed += _fontChanged; _font.Disposed += _fontDisposed; }
    }
    private void FontChanged(Resource font) { lock (_gate) { if (IsDisposed || !ReferenceEquals(_font, font)) return; } PublishChanged(); }
    private void FontDisposed(ElectronObject font) => FontChanged((Resource)font);

    internal readonly record struct Snapshot(Color FontColor, int OutlineSize, Color OutlineColor, int ShadowSize, Color ShadowColor, Vector2 ShadowOffset);
    internal Snapshot Capture(List<Outline> outlines, List<Shadow> shadows)
    {
        lock (_gate)
        {
            ThrowIfDisposed(); outlines.Clear(); outlines.AddRange(_outlines); shadows.Clear(); shadows.AddRange(_shadows);
            return new(_fontColor, _outlineSize, _outlineColor, _shadowSize, _shadowColor, _shadowOffset);
        }
    }

    private void PublishChanged() { Interlocked.Increment(ref _revision); EmitChanged(); }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        foreach (var property in SettingsProperties) yield return property;
        int outlines, shadows; lock (_gate) { ThrowIfDisposed(); outlines = _outlines.Count; shadows = _shadows.Count; }
        for (var item = 0; item < outlines; item++)
        {
            var index = item;
            yield return new PropertyDescriptor<LabelSettings, int>($"StackedOutline[{index}].Size", settings => settings.GetStackedOutlineSize(index), (settings, value) => settings.SetStackedOutlineSize(index, value), _ => 0, stored: true);
            yield return new PropertyDescriptor<LabelSettings, Color>($"StackedOutline[{index}].Color", settings => settings.GetStackedOutlineColor(index), (settings, value) => settings.SetStackedOutlineColor(index, value), _ => Colors.Black, stored: true);
        }
        for (var item = 0; item < shadows; item++)
        {
            var index = item;
            yield return new PropertyDescriptor<LabelSettings, Vector2>($"StackedShadow[{index}].Offset", settings => settings.GetStackedShadowOffset(index), (settings, value) => settings.SetStackedShadowOffset(index, value), _ => Vector2.One, stored: true);
            yield return new PropertyDescriptor<LabelSettings, Color>($"StackedShadow[{index}].Color", settings => settings.GetStackedShadowColor(index), (settings, value) => settings.SetStackedShadowColor(index, value), _ => Colors.Black, stored: true);
            yield return new PropertyDescriptor<LabelSettings, int>($"StackedShadow[{index}].OutlineSize", settings => settings.GetStackedShadowOutlineSize(index), (settings, value) => settings.SetStackedShadowOutlineSize(index, value), _ => 0, stored: true);
        }
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(LabelSettings) ? new LabelSettings() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        Font? font; float lineSpacing, paragraphSpacing; int fontSize, outlineSize, shadowSize; Color fontColor, outlineColor, shadowColor; Vector2 shadowOffset;
        Outline[] outlines; Shadow[] shadows;
        lock (_gate)
        {
            ThrowIfDisposed(); font = _font; lineSpacing = _lineSpacing; paragraphSpacing = _paragraphSpacing; fontSize = _fontSize; outlineSize = _outlineSize; shadowSize = _shadowSize;
            fontColor = _fontColor; outlineColor = _outlineColor; shadowColor = _shadowColor; shadowOffset = _shadowOffset; outlines = [.. _outlines]; shadows = [.. _shadows];
        }
        if (deep) font = (Font?)duplicateSubresource(font); ValidateFont(font);
        var copy = (LabelSettings)target; bool structure;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed(); structure = copy._outlines.Count != outlines.Length || copy._shadows.Count != shadows.Length;
            copy._outlines.EnsureCapacity(outlines.Length); copy._shadows.EnsureCapacity(shadows.Length);
            copy.ReplaceFont(font); copy._lineSpacing = lineSpacing; copy._paragraphSpacing = paragraphSpacing; copy._fontSize = fontSize;
            copy._outlineSize = outlineSize; copy._shadowSize = shadowSize; copy._fontColor = fontColor; copy._outlineColor = outlineColor; copy._shadowColor = shadowColor; copy._shadowOffset = shadowOffset;
            copy._outlines.Clear(); copy._outlines.AddRange(outlines); copy._shadows.Clear(); copy._shadows.AddRange(shadows);
        }
        if (structure) copy.PublishStructure(); else copy.PublishChanged();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) { ReplaceFont(null); _outlines.Clear(); _shadows.Clear(); }
        base.Dispose(disposing);
    }
}
