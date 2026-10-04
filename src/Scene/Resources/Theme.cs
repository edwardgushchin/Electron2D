using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Stores typed colors, constants, fonts, font sizes, icons and canvas styles by item and theme-type keys.</summary>
/// <remarks>Names use ASCII letters, digits and underscores; an empty type is valid, while an item name is not.
/// Resources are borrowed and shared aliases use one change subscription. Explicitly disposed resources retain
/// their identity; drawing consumers reject their use. Mutations commit under an instance lock and notify outside it.
/// Structural changes notify the property list before Changed. Existing-key writes notify even when equal.
/// Font defaults participate in the same borrowed-resource lifetime.</remarks>
public partial class Theme : Resource
{
    /// <summary>Identifies a category of theme data.</summary>
    public enum DataType
    {
        /// <summary>A color value.</summary>
        Color = 0,
        /// <summary>A signed integer constant.</summary>
        Constant = 1,
        /// <summary>A borrowed font resource.</summary>
        Font = 2,
        /// <summary>A signed font-size entry.</summary>
        FontSize = 3,
        /// <summary>A borrowed texture.</summary>
        Icon = 4,
        /// <summary>A borrowed canvas style.</summary>
        StyleBox = 5,
        /// <summary>The exclusive category upper bound.</summary>
        Max = 6
    }

    private readonly object _gate = new();
    private readonly Dictionary<string, Dictionary<string, Color>> _colors = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, int>> _constants = new(StringComparer.Ordinal), _fontSizes = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, Font?>> _fonts = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, Texture?>> _icons = new(StringComparer.Ordinal);
    private readonly Dictionary<string, Dictionary<string, StyleBox?>> _styles = new(StringComparer.Ordinal);
    private readonly Dictionary<string, string> _variations = new(StringComparer.Ordinal);
    private readonly Dictionary<string, List<string>> _variationChildren = new(StringComparer.Ordinal);
    private readonly Dictionary<Resource, int> _references = new(ReferenceEqualityComparer.Instance);
    private readonly Action<Resource> _resourceChanged;
    private readonly Action<ElectronObject> _resourceDisposed;
    private float _defaultBaseScale;
    private int _defaultFontSize = -1;
    private Font? _defaultFont;
    private static readonly PropertyDescriptor[] ThemeProperties =
    [
        new PropertyDescriptor<Theme, float>(nameof(DefaultBaseScale), theme => theme.DefaultBaseScale, (theme, value) => theme.DefaultBaseScale = value, _ => 0, stored: true),
        new PropertyDescriptor<Theme, Font?>(nameof(DefaultFont), theme => theme.DefaultFont, (theme, value) => theme.DefaultFont = value, _ => null, stored: true),
        new PropertyDescriptor<Theme, int>(nameof(DefaultFontSize), theme => theme.DefaultFontSize, (theme, value) => theme.DefaultFontSize = value, _ => -1, stored: true)
    ];

    /// <summary>Creates an empty theme with base scale zero, no default font and default font size minus one.</summary>
    public Theme() { _resourceChanged = ReferencedChanged; _resourceDisposed = ReferencedDisposed; }

    /// <summary>Gets or sets the finite base-scale override. Only positive values provide a default.</summary>
    /// <value>Zero initially. Equal writes are silent; signed finite values are retained.</value>
    public float DefaultBaseScale
    {
        get { lock (_gate) { ThrowIfDisposed(); return _defaultBaseScale; } }
        set
        {
            lock (_gate) { ThrowIfDisposed(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_defaultBaseScale == value) return; _defaultBaseScale = value; }
            Publish(false);
        }
    }
    /// <summary>Gets or sets the borrowed default font used when no nonnull font entry exists.</summary>
    /// <value>Null initially. Equal assignments are silent; changes emit Changed.</value>
    /// <remarks>The font remains borrowed and forwards its changes and disposal. Clearing theme items retains this default.</remarks>
    /// <exception cref="ObjectDisposedException">This theme or the assigned font is disposed.</exception>
    public Font? DefaultFont
    {
        get { lock (_gate) { ThrowIfDisposed(); return _defaultFont; } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed(); ValidateResource(value); if (ReferenceEquals(_defaultFont, value)) return;
                Retain(value); Release(_defaultFont); _defaultFont = value;
            }
            Publish(false);
        }
    }
    /// <summary>Tests whether this theme stores a default font identity.</summary>
    /// <returns>True when DefaultFont is nonnull.</returns>
    public bool HasDefaultFont() => DefaultFont is not null;

    /// <summary>Gets or sets the default font size. Only positive values supply a fallback.</summary>
    /// <value>Minus one initially. Equal writes are silent; other writes emit Changed.</value>
    public int DefaultFontSize
    {
        get { lock (_gate) { ThrowIfDisposed(); return _defaultFontSize; } }
        set { lock (_gate) { ThrowIfDisposed(); if (_defaultFontSize == value) return; _defaultFontSize = value; } Publish(false); }
    }
    /// <summary>Tests whether this theme supplies a positive base-scale override.</summary>
    /// <returns>True when DefaultBaseScale is greater than zero.</returns>
    public bool HasDefaultBaseScale() => DefaultBaseScale > 0;
    /// <summary>Tests whether this theme supplies a positive default font size.</summary>
    /// <returns>True when DefaultFontSize is greater than zero.</returns>
    public bool HasDefaultFontSize() => DefaultFontSize > 0;

    /// <summary>Stores a finite color, notifying even for an equal existing value.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="color">The finite color.</param>
    public void SetColor(string name, string themeType, Color color)
    {
        ThrowIfDisposed();
        if (!color.IsFinite()) throw new ArgumentException("Theme colors must be finite.", nameof(color));
        SetValue(_colors, name, themeType, color);
    }
    /// <summary>Gets a stored color or opaque black when absent.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The stored color or opaque black.</returns>
    public virtual Color GetColor(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return TryItem(_colors, name, themeType, out var value) ? value : Colors.Black; } }
    /// <summary>Tests whether a color entry exists.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when present.</returns>
    public bool HasColor(string name, string themeType) => HasEntry(_colors, name, themeType);
    /// <summary>Renames an existing color; a missing source or occupied destination throws.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameColor(string oldName, string name, string themeType) => RenameEntry(_colors, oldName, name, themeType);
    /// <summary>Removes an existing color; a missing entry throws.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearColor(string name, string themeType) => ClearValue(_colors, name, themeType);
    /// <summary>Lists the stored color keys for one type.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>A snapshot, including no items for an absent type.</returns>
    public string[] GetColorList(string themeType) => ItemList(_colors, themeType);
    /// <summary>Lists types with a color-category record, including empty records.</summary><returns>A type-name snapshot.</returns>
    public string[] GetColorTypeList() => TypeList(_colors);

    /// <summary>Stores a signed integer constant, notifying even for an equal value.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="constant">The signed value.</param>
    public void SetConstant(string name, string themeType, int constant) => SetValue(_constants, name, themeType, constant);
    /// <summary>Gets a stored constant or zero when absent.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The stored value or zero.</returns>
    public virtual int GetConstant(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return TryItem(_constants, name, themeType, out var value) ? value : 0; } }
    /// <summary>Tests whether a constant entry exists.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when present.</returns>
    public bool HasConstant(string name, string themeType) => HasEntry(_constants, name, themeType);
    /// <summary>Renames an existing constant, rejecting a missing source or occupied destination.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameConstant(string oldName, string name, string themeType) => RenameEntry(_constants, oldName, name, themeType);
    /// <summary>Removes an existing constant; a missing entry throws.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearConstant(string name, string themeType) => ClearValue(_constants, name, themeType);
    /// <summary>Lists stored constant keys for one type.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetConstantList(string themeType) => ItemList(_constants, themeType);
    /// <summary>Lists types with a constant-category record.</summary><returns>A type-name snapshot.</returns>
    public string[] GetConstantTypeList() => TypeList(_constants);

    /// <summary>Stores a signed font size; nonpositive entries retain their slot but select a fallback.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="fontSize">The signed value.</param>
    public void SetFontSize(string name, string themeType, int fontSize) => SetValue(_fontSizes, name, themeType, fontSize);
    /// <summary>Gets a positive entry, the positive local default, or the theme-database fallback.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The resolved font size.</returns>
    public virtual int GetFontSize(string name, string themeType)
    {
        lock (_gate) { CheckQuery(name, themeType); if (TryItem(_fontSizes, name, themeType, out var value) && value > 0) return value; if (_defaultFontSize > 0) return _defaultFontSize; }
        return ThemeDB.FallbackFontSize;
    }
    /// <summary>Tests for a positive entry or a positive local default, without using the global fallback.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when this theme supplies a usable size.</returns>
    public bool HasFontSize(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return _defaultFontSize > 0 || TryItem(_fontSizes, name, themeType, out var value) && value > 0; } }
    /// <summary>Renames an existing font-size slot, including a nonpositive placeholder.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameFontSize(string oldName, string name, string themeType) => RenameEntry(_fontSizes, oldName, name, themeType);
    /// <summary>Removes an existing font-size slot.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearFontSize(string name, string themeType) => ClearValue(_fontSizes, name, themeType);
    /// <summary>Lists all stored font-size keys, including nonpositive entries.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetFontSizeList(string themeType) => ItemList(_fontSizes, themeType);
    /// <summary>Lists types with a font-size-category record.</summary><returns>A type-name snapshot.</returns>
    public string[] GetFontSizeTypeList() => TypeList(_fontSizes);

    /// <summary>Stores a borrowed font or an explicit null placeholder.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="font">A live font or null.</param>
    public void SetFont(string name, string themeType, Font? font) => SetResource(_fonts, name, themeType, font);
    /// <summary>Gets a nonnull font entry, the local default font, or the theme-database fallback.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The borrowed font or fallback, which may be null.</returns>
    public virtual Font? GetFont(string name, string themeType)
    {
        lock (_gate) { CheckQuery(name, themeType); if (TryItem(_fonts, name, themeType, out var value) && value is not null) return value; if (_defaultFont is not null) return _defaultFont; }
        return ThemeDB.FallbackFont;
    }
    /// <summary>Tests for a nonnull font entry or local default; universal fallback fonts do not count.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when a nonnull identity is stored.</returns>
    public bool HasFont(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return _defaultFont is not null || TryItem(_fonts, name, themeType, out var value) && value is not null; } }
    /// <summary>Renames a font slot without changing borrowed-resource subscriptions.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameFont(string oldName, string name, string themeType) => RenameEntry(_fonts, oldName, name, themeType);
    /// <summary>Removes an existing font slot and releases its change subscription.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearFont(string name, string themeType) => ClearResource(_fonts, name, themeType);
    /// <summary>Lists font keys, including explicit null placeholders.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetFontList(string themeType) => ItemList(_fonts, themeType);
    /// <summary>Lists types with a font-category record.</summary><returns>A type-name snapshot.</returns>
    public string[] GetFontTypeList() => TypeList(_fonts);

    /// <summary>Stores a borrowed icon or an explicit null placeholder.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="texture">A live texture or null.</param>
    public void SetIcon(string name, string themeType, Texture? texture) => SetResource(_icons, name, themeType, texture);
    /// <summary>Gets a nonnull icon entry or the theme-database fallback.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The borrowed icon or fallback, which may be null.</returns>
    public virtual Texture? GetIcon(string name, string themeType)
    {
        lock (_gate) { CheckQuery(name, themeType); if (TryItem(_icons, name, themeType, out var value) && value is not null) return value; }
        return ThemeDB.FallbackIcon;
    }
    /// <summary>Tests for a nonnull icon entry; placeholders and fallback icons do not count.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when a nonnull identity is stored.</returns>
    public bool HasIcon(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return TryItem(_icons, name, themeType, out var value) && value is not null; } }
    /// <summary>Renames an icon slot without changing borrowed-resource subscriptions.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameIcon(string oldName, string name, string themeType) => RenameEntry(_icons, oldName, name, themeType);
    /// <summary>Removes an existing icon slot and releases its change subscription.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearIcon(string name, string themeType) => ClearResource(_icons, name, themeType);
    /// <summary>Lists icon keys, including explicit null placeholders.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetIconList(string themeType) => ItemList(_icons, themeType);
    /// <summary>Lists types with an icon-category record.</summary><returns>A type-name snapshot.</returns>
    public string[] GetIconTypeList() => TypeList(_icons);

    /// <summary>Stores a borrowed canvas style or an explicit null placeholder.</summary>
    /// <param name="name">The nonempty item key.</param><param name="themeType">The theme-type key.</param><param name="styleBox">A live style or null.</param>
    public void SetStyleBox(string name, string themeType, StyleBox? styleBox) => SetResource(_styles, name, themeType, styleBox);
    /// <summary>Gets a nonnull style entry or the theme-database fallback.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>The borrowed style or fallback, which may be null.</returns>
    public virtual StyleBox? GetStyleBox(string name, string themeType)
    {
        lock (_gate) { CheckQuery(name, themeType); if (TryItem(_styles, name, themeType, out var value) && value is not null) return value; }
        return ThemeDB.FallbackStyleBox;
    }
    /// <summary>Tests for a nonnull style entry; placeholders and fallback styles do not count.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param><returns>True when a nonnull identity is stored.</returns>
    public bool HasStyleBox(string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return TryItem(_styles, name, themeType, out var value) && value is not null; } }
    /// <summary>Renames a style slot without changing borrowed-resource subscriptions.</summary>
    /// <param name="oldName">The existing key.</param><param name="name">The new key.</param><param name="themeType">The exact theme-type key.</param>
    public void RenameStyleBox(string oldName, string name, string themeType) => RenameEntry(_styles, oldName, name, themeType);
    /// <summary>Removes an existing style slot and releases its change subscription.</summary>
    /// <param name="name">The item key.</param><param name="themeType">The exact theme-type key.</param>
    public void ClearStyleBox(string name, string themeType) => ClearResource(_styles, name, themeType);
    /// <summary>Lists style keys, including explicit null placeholders.</summary>
    /// <param name="themeType">The exact theme-type key.</param><returns>An item-name snapshot.</returns>
    public string[] GetStyleBoxList(string themeType) => ItemList(_styles, themeType);
    /// <summary>Lists types with a style-category record.</summary><returns>A type-name snapshot.</returns>
    public string[] GetStyleBoxTypeList() => TypeList(_styles);

    private static void ValidateName(string name, bool allowEmpty)
    {
        ArgumentNullException.ThrowIfNull(name);
        if (!allowEmpty && name.Length == 0) throw new ArgumentException("An item or variation name must not be empty.", nameof(name));
        foreach (var character in name) if (!char.IsAsciiLetterOrDigit(character) && character != '_') throw new ArgumentException("Theme keys contain only ASCII letters, digits and underscores.", nameof(name));
    }
    private void CheckQuery(string name, string themeType) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); ArgumentNullException.ThrowIfNull(themeType); }
    private static bool TryItem<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType, out T value)
    {
        if (map.TryGetValue(themeType, out var items) && items.TryGetValue(name, out value!)) return true;
        value = default!; return false;
    }
    private static Dictionary<string, T> EnsureType<T>(Dictionary<string, Dictionary<string, T>> map, string themeType)
    {
        if (!map.TryGetValue(themeType, out var items)) map.Add(themeType, items = new(StringComparer.Ordinal));
        return items;
    }
    private void SetValue<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType, T value)
    {
        bool existing;
        lock (_gate) { ThrowIfDisposed(); ValidateName(name, false); ValidateName(themeType, true); var items = EnsureType(map, themeType); existing = items.ContainsKey(name); items[name] = value; }
        Publish(!existing);
    }
    private void SetResource<T>(Dictionary<string, Dictionary<string, T?>> map, string name, string themeType, T? value) where T : Resource
    {
        bool existing;
        lock (_gate)
        {
            ThrowIfDisposed(); ValidateName(name, false); ValidateName(themeType, true); ValidateResource(value);
            var items = EnsureType(map, themeType); items.TryGetValue(name, out var previous); existing = previous is not null;
            if (!ReferenceEquals(previous, value)) { Retain(value); Release(previous); }
            items[name] = value;
        }
        Publish(!existing);
    }
    private static void ValidateResource(Resource? resource) { if (resource is { IsDisposed: true }) throw new ObjectDisposedException(resource.GetType().Name); }
    private bool HasEntry<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType) { lock (_gate) { CheckQuery(name, themeType); return TryItem(map, name, themeType, out _); } }
    private string[] ItemList<T>(Dictionary<string, Dictionary<string, T>> map, string themeType)
    {
        lock (_gate) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(themeType); return map.TryGetValue(themeType, out var items) ? [.. items.Keys] : []; }
    }
    private string[] TypeList<T>(Dictionary<string, Dictionary<string, T>> map) { lock (_gate) { ThrowIfDisposed(); return [.. map.Keys]; } }
    private void RenameEntry<T>(Dictionary<string, Dictionary<string, T>> map, string oldName, string name, string themeType)
    {
        lock (_gate)
        {
            CheckQuery(oldName, themeType); ValidateName(name, false); ValidateName(themeType, true);
            if (!map.TryGetValue(themeType, out var items) || items.ContainsKey(name) || !items.TryGetValue(oldName, out var value)) throw new ArgumentException("Renaming requires an existing source and an unused destination.");
            items.Add(name, value); items.Remove(oldName);
        }
        Publish(true);
    }
    private T RemoveEntry<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType)
    {
        CheckQuery(name, themeType);
        if (!map.TryGetValue(themeType, out var items) || !items.Remove(name, out var value)) throw new ArgumentException("The requested theme item does not exist.", nameof(name));
        return value;
    }
    private void ClearValue<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType) { lock (_gate) RemoveEntry(map, name, themeType); Publish(true); }
    private void ClearResource<T>(Dictionary<string, Dictionary<string, T?>> map, string name, string themeType) where T : Resource { lock (_gate) Release(RemoveEntry(map, name, themeType)); Publish(true); }
    private void Retain(Resource? resource)
    {
        if (resource is null) return;
        if (_references.TryGetValue(resource, out var count)) _references[resource] = checked(count + 1);
        else { _references.Add(resource, 1); resource.Changed += _resourceChanged; resource.Disposed += _resourceDisposed; }
    }
    private void Release(Resource? resource)
    {
        if (resource is null) return;
        var count = _references[resource];
        if (count > 1) _references[resource] = count - 1;
        else { _references.Remove(resource); resource.Changed -= _resourceChanged; resource.Disposed -= _resourceDisposed; }
    }
    private void ReferencedChanged(Resource resource) { lock (_gate) { if (IsDisposed || !_references.ContainsKey(resource)) return; } Publish(false); }
    private void ReferencedDisposed(ElectronObject resource) => ReferencedChanged((Resource)resource);
    private void Publish(bool propertyList)
    {
        Exception? failure = null;
        if (propertyList) try { NotifyPropertyListChanged(); } catch (Exception error) { failure = error; }
        try { EmitChanged(); } catch (Exception error) { if (failure is not null) throw new AggregateException(failure, error); throw; }
        if (failure is not null) ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
