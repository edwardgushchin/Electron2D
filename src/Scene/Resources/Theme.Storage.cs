namespace Electron2D;

public partial class Theme
{
    /// <summary>Overlays another theme's stored items, variations and positive defaults in one notification batch.</summary>
    /// <param name="other">The source theme, or null for no operation.</param>
    /// <remarks>Null resource placeholders overwrite existing entries. Empty category records are not imported.
    /// Nonpositive source defaults leave this theme's defaults unchanged. Resources remain borrowed.</remarks>
    public void MergeWith(Theme? other)
    {
        ThrowIfDisposed(); if (other is null) return;
        var source = other.Capture(); ValidateResources(source);
        lock (_gate)
        {
            ThrowIfDisposed(); MergeValues(_colors, source.Colors); MergeValues(_constants, source.Constants); MergeValues(_fontSizes, source.FontSizes);
            MergeResources(_icons, source.Icons); MergeResources(_styles, source.Styles);
            foreach (var variation in source.Variations) SetVariationCore(variation.Key, variation.Value);
            if (source.BaseScale > 0) _defaultBaseScale = source.BaseScale;
            if (source.FontSize > 0) _defaultFontSize = source.FontSize;
        }
        Publish(true);
    }
    /// <summary>Removes all typed entries and variation links while retaining the configured defaults.</summary>
    /// <remarks>Releases subscriptions without disposing borrowed resources. Even an empty clear emits
    /// PropertyListChanged followed by Changed.</remarks>
    public void Clear()
    {
        lock (_gate) { ThrowIfDisposed(); ClearCore(); }
        Publish(true);
    }

    private sealed record Snapshot(float BaseScale, int FontSize,
        Dictionary<string, Dictionary<string, Color>> Colors, Dictionary<string, Dictionary<string, int>> Constants,
        Dictionary<string, Dictionary<string, int>> FontSizes, Dictionary<string, Dictionary<string, Texture?>> Icons,
        Dictionary<string, Dictionary<string, StyleBox?>> Styles, Dictionary<string, string> Variations,
        Dictionary<string, List<string>> VariationChildren);

    private Snapshot Capture()
    {
        lock (_gate)
        {
            ThrowIfDisposed(); var children = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            foreach (var entry in _variationChildren) children.Add(entry.Key, [.. entry.Value]);
            return new(_defaultBaseScale, _defaultFontSize, CloneMap(_colors), CloneMap(_constants), CloneMap(_fontSizes), CloneMap(_icons), CloneMap(_styles), new(_variations, StringComparer.Ordinal), children);
        }
    }
    private static Dictionary<string, Dictionary<string, T>> CloneMap<T>(Dictionary<string, Dictionary<string, T>> source)
    {
        var result = new Dictionary<string, Dictionary<string, T>>(StringComparer.Ordinal);
        foreach (var type in source) result.Add(type.Key, new(type.Value, StringComparer.Ordinal));
        return result;
    }
    private static void ValidateResources(Snapshot snapshot)
    {
        foreach (var type in snapshot.Icons.Values) foreach (var value in type.Values) ValidateResource(value);
        foreach (var type in snapshot.Styles.Values) foreach (var value in type.Values) ValidateResource(value);
    }
    private static void MergeValues<T>(Dictionary<string, Dictionary<string, T>> target, Dictionary<string, Dictionary<string, T>> source)
    {
        foreach (var type in source) foreach (var item in type.Value) EnsureType(target, type.Key)[item.Key] = item.Value;
    }
    private void MergeResources<T>(Dictionary<string, Dictionary<string, T?>> target, Dictionary<string, Dictionary<string, T?>> source) where T : Resource
    {
        foreach (var type in source) foreach (var item in type.Value)
            {
                var items = EnsureType(target, type.Key); items.TryGetValue(item.Key, out var previous);
                if (!ReferenceEquals(previous, item.Value)) { Retain(item.Value); Release(previous); }
                items[item.Key] = item.Value;
            }
    }
    private void ClearCore()
    {
        foreach (var resource in _references.Keys) { resource.Changed -= _resourceChanged; resource.Disposed -= _resourceDisposed; }
        _references.Clear(); _colors.Clear(); _constants.Clear(); _fontSizes.Clear(); _icons.Clear(); _styles.Clear(); _variations.Clear(); _variationChildren.Clear();
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(Theme) ? new Theme() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var snapshot = Capture(); ValidateResources(snapshot);
        if (deep)
        {
            snapshot = snapshot with { Icons = DuplicateResources(snapshot.Icons, duplicateSubresource), Styles = DuplicateResources(snapshot.Styles, duplicateSubresource) };
        }
        var copy = (Theme)target;
        lock (copy._gate)
        {
            copy.ThrowIfDisposed(); copy.ClearCore(); copy._defaultBaseScale = snapshot.BaseScale; copy._defaultFontSize = snapshot.FontSize;
            foreach (var type in snapshot.Colors) copy._colors.Add(type.Key, type.Value);
            foreach (var type in snapshot.Constants) copy._constants.Add(type.Key, type.Value);
            foreach (var type in snapshot.FontSizes) copy._fontSizes.Add(type.Key, type.Value);
            foreach (var type in snapshot.Icons) { copy._icons.Add(type.Key, type.Value); foreach (var resource in type.Value.Values) copy.Retain(resource); }
            foreach (var type in snapshot.Styles) { copy._styles.Add(type.Key, type.Value); foreach (var resource in type.Value.Values) copy.Retain(resource); }
            foreach (var variation in snapshot.Variations) copy._variations.Add(variation.Key, variation.Value);
            foreach (var children in snapshot.VariationChildren) copy._variationChildren.Add(children.Key, children.Value);
        }
        copy.Publish(true);
    }
    private static Dictionary<string, Dictionary<string, T?>> DuplicateResources<T>(Dictionary<string, Dictionary<string, T?>> source, Func<Resource?, Resource?> duplicate) where T : Resource
    {
        var result = new Dictionary<string, Dictionary<string, T?>>(StringComparer.Ordinal);
        foreach (var type in source)
        {
            var items = new Dictionary<string, T?>(StringComparer.Ordinal);
            foreach (var item in type.Value) items.Add(item.Key, (T?)duplicate(item.Value));
            result.Add(type.Key, items);
        }
        return result;
    }
    /// <inheritdoc />
    protected override void OnResetState() => Clear();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var entries = new List<PropertyDescriptor>();
        lock (_gate)
        {
            ThrowIfDisposed();
            AddDescriptors(_colors, "colors", entries, static (theme, name, type) => theme.GetColor(name, type), static (theme, name, type, value) => theme.SetColor(name, type, value));
            AddDescriptors(_constants, "constants", entries, static (theme, name, type) => theme.GetConstant(name, type), static (theme, name, type, value) => theme.SetConstant(name, type, value));
            AddDescriptors(_fontSizes, "font_sizes", entries, static (theme, name, type) => theme.StoredValue(theme._fontSizes, name, type), static (theme, name, type, value) => theme.SetFontSize(name, type, value));
            AddDescriptors(_icons, "icons", entries, static (theme, name, type) => theme.StoredValue(theme._icons, name, type), static (theme, name, type, value) => theme.SetIcon(name, type, value));
            AddDescriptors(_styles, "styles", entries, static (theme, name, type) => theme.StoredValue(theme._styles, name, type), static (theme, name, type, value) => theme.SetStyleBox(name, type, value));
            foreach (var variation in _variations.Keys)
            {
                var name = variation;
                entries.Add(new PropertyDescriptor<Theme, string>($"{name}/base_type", theme => theme.GetTypeVariationBase(name), (theme, value) => theme.SetTypeVariation(name, value), stored: true));
            }
        }
        entries.Sort(static (left, right) => StringComparer.Ordinal.Compare(left.Name, right.Name));
        return base.GetPropertyDescriptors().Concat(ThemeProperties).Concat(entries);
    }
    private T StoredValue<T>(Dictionary<string, Dictionary<string, T>> map, string name, string themeType)
    {
        lock (_gate) { CheckQuery(name, themeType); return TryItem(map, name, themeType, out var value) ? value : default!; }
    }
    private static void AddDescriptors<T>(Dictionary<string, Dictionary<string, T>> map, string category, List<PropertyDescriptor> descriptors,
        Func<Theme, string, string, T> getter, Action<Theme, string, string, T> setter)
    {
        foreach (var type in map) foreach (var item in type.Value.Keys)
            {
                var themeType = type.Key; var name = item;
                descriptors.Add(new PropertyDescriptor<Theme, T>($"{themeType}/{category}/{name}", theme => getter(theme, name, themeType), (theme, value) => setter(theme, name, themeType, value), stored: true));
            }
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (_gate) ClearCore();
        base.Dispose(disposing);
    }
}
