namespace Electron2D;

internal sealed class ThemeOwner : IDisposable
{
    internal sealed class Store<T>(Func<Theme, string, string, bool> has, Func<Theme, string, string, T> get, Func<T> fallback, bool resources = false, Action<T>? validate = null)
    {
        internal readonly Dictionary<string, T> Overrides = new(StringComparer.Ordinal);
        internal readonly Dictionary<(string Name, string Type), T> Cache = [];
        internal readonly Func<Theme, string, string, bool> Has = has;
        internal readonly Func<Theme, string, string, T> Get = get;
        internal readonly Func<T> Fallback = fallback;
        internal readonly bool Resources = resources;
        internal readonly Action<T>? Validate = validate;
    }
    internal readonly Store<Color> Colors = new(static (t, n, k) => t.HasColor(n, k), static (t, n, k) => t.GetColor(n, k), static () => Electron2D.Colors.Black,
        validate: static value => { if (!value.IsFinite()) throw new ArgumentException("Theme colors must be finite.", nameof(value)); });
    internal readonly Store<int> Constants = new(static (t, n, k) => t.HasConstant(n, k), static (t, n, k) => t.GetConstant(n, k), static () => 0);
    internal readonly Store<int> FontSizes = new(static (t, n, k) => t.HasFontSize(n, k), static (t, n, k) => t.GetFontSize(n, k), static () => ThemeDB.Instance.FallbackFontSize);
    internal readonly Store<Texture?> Icons = new(static (t, n, k) => t.HasIcon(n, k), static (t, n, k) => t.GetIcon(n, k), static () => ThemeDB.Instance.FallbackIcon, true);
    internal readonly Store<StyleBox?> Styles = new(static (t, n, k) => t.HasStyleBox(n, k), static (t, n, k) => t.GetStyleBox(n, k), static () => ThemeDB.Instance.FallbackStyleBox, true);
    private readonly Node _owner;
    private readonly Action _check, _mutate;
    private readonly Action<Resource> _sourceChanged, _overrideChanged;
    private readonly Action<ElectronObject> _sourceDisposed, _overrideDisposed;
    private readonly Action _globalChanged;
    private readonly string _className;
    private readonly string[] _classTypes;
    private readonly List<string> _types = [];
    private readonly HashSet<string> _visited = new(StringComparer.Ordinal);
    private readonly List<ThemeOwner> _propagation = [];
    private readonly Dictionary<Resource, int> _sources = new(ReferenceEqualityComparer.Instance);
    private Theme? _theme;
    private string _variation = "";
    private bool _bulk, _disposed, _propagating, _repeat, _notifying, _notifyAgain;
    private volatile SceneTree? _tree;
    private Action? _sourceAction, _selfAction, _overrideAction;
    private int _ownerThread, _invalidated;
    private ulong _generation, _cacheGeneration;
    internal ThemeOwner(Node owner, Action check, Action mutate)
    {
        _owner = owner; _check = check; _mutate = mutate; _className = owner.ClassName; _classTypes = ThemeDB.NativeDependencies(owner.GetType());
        _sourceChanged = SourceChanged; _sourceDisposed = SourceDisposed; _overrideChanged = OverrideResourceChanged;
        _overrideDisposed = OverrideResourceDisposed; _globalChanged = GlobalChanged;
    }
    internal static ThemeOwner? From(Node? node) => node switch { Control c => c.ThemeOwner, Window w => w.ThemeOwner, _ => null };
    internal Theme? Theme
    {
        get { _check(); return _theme; }
        set
        {
            _mutate(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            if (ReferenceEquals(value, _theme)) return;
            if (_theme is { } old) { old.Changed -= _sourceChanged; old.Disposed -= _sourceDisposed; }
            _theme = value;
            if (_theme is { } current) { current.Changed += _sourceChanged; current.Disposed += _sourceDisposed; }
            Propagate();
        }
    }
    internal string Variation
    {
        get { _check(); return _variation; }
        set { _mutate(); ValidateName(value); if (_variation == value) return; _variation = value; NotifySelf(); }
    }
    internal void Membership(bool entering)
    {
        _generation++;
        if (!entering)
        {
            ThemeDB.Instance.ContextChanged -= _globalChanged; _tree = null; _sourceAction = null; _selfAction = null; _overrideAction = null; Invalidate(); return;
        }
        var tree = _owner.Tree!; var generation = _generation;
        _ownerThread = Environment.CurrentManagedThreadId; _tree = tree;
        _sourceAction = () => { if (!_disposed && generation == _generation && ReferenceEquals(_tree, tree)) Propagate(); };
        _selfAction = () => { if (!_disposed && generation == _generation && ReferenceEquals(_tree, tree)) NotifySelf(); };
        _overrideAction = () => { if (!_disposed && generation == _generation && ReferenceEquals(_tree, tree)) OverrideChanged(); };
        ThemeDB.Instance.ContextChanged += _globalChanged;
    }
    private void SourceChanged(Resource source) { if (ReferenceEquals(source, _theme)) Defer(true); }
    private void SourceDisposed(ElectronObject source) { if (ReferenceEquals(source, _theme)) Defer(true); }
    private void GlobalChanged() => Defer(false);
    private void Defer(bool propagate, bool overrideResource = false)
    {
        var tree = _tree; var action = overrideResource ? _overrideAction : propagate ? _sourceAction : _selfAction;
        if (_disposed) return;
        if (tree is null || action is null) { Interlocked.Exchange(ref _invalidated, 1); return; }
        try { tree.Defer(action); } catch (ObjectDisposedException) { }
    }
    internal void Invalidate()
    {
        _cacheGeneration++;
        Colors.Cache.Clear(); Constants.Cache.Clear(); FontSizes.Cache.Clear(); Icons.Cache.Clear(); Styles.Cache.Clear(); _types.Clear();
        Interlocked.Exchange(ref _invalidated, 0);
    }
    private void CheckQuery()
    {
        _check(); if (Interlocked.Exchange(ref _invalidated, 0) != 0) Invalidate();
    }
    internal void NotifySelf()
    {
        if (_disposed || _owner.IsDisposed) return;
        if (!_owner.IsInsideTree) { Invalidate(); return; }
        if (_notifying) { _notifyAgain = true; return; }
        _notifying = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _notifyAgain = false;
                try { _owner.DispatchNotification(_owner is Control ? Control.NotificationThemeChanged : Window.NotificationThemeChanged); }
                catch (Exception error) { Node.CollectException(ref errors, error); }
                if (!_notifyAgain || _disposed || _owner.IsDisposed || !_owner.IsInsideTree) break;
                if (pass == 63) Node.CollectException(ref errors, new InvalidOperationException("Theme notification callbacks did not settle after 64 passes."));
            }
        }
        finally { _notifying = false; _notifyAgain = false; }
        Node.ThrowCollected("Theme notification callbacks failed.", errors);
    }
    internal void ParentChanged() => Propagate();
    private void Capture(ThemeOwner owner)
    {
        if (owner._disposed || owner._owner.IsDisposed) return;
        _propagation.Add(owner);
        for (var index = 0; index < owner._owner.ChildCount; index++) if (From(owner._owner.GetChild(index)) is { } child) Capture(child);
    }
    private bool StillDescendant(ThemeOwner candidate)
    {
        for (var node = candidate._owner; node is not null && From(node) is not null; node = node.Parent) if (ReferenceEquals(node, _owner)) return true;
        return false;
    }
    private void Propagate()
    {
        if (_disposed || _owner.IsDisposed) return;
        if (_propagating) { _repeat = true; return; }
        _propagating = true; List<Exception>? errors = null;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _repeat = false; _propagation.Clear(); Capture(this);
                foreach (var owner in _propagation)
                    if (!owner._disposed && !owner._owner.IsDisposed && StillDescendant(owner))
                        try { owner.NotifySelf(); } catch (Exception error) { Node.CollectException(ref errors, error); }
                if (!_repeat) break;
                if (pass == 63) Node.CollectException(ref errors, new InvalidOperationException("Theme notification callbacks did not settle after 64 passes."));
            }
        }
        finally { _propagation.Clear(); _propagating = false; _repeat = false; }
        Node.ThrowCollected("Theme propagation callbacks failed.", errors);
    }
    private bool UsesOverride(string type) => type.Length == 0 || type == _className || type == _variation;
    private Theme? VariationSource()
    {
        for (var owner = this; owner is not null; owner = From(owner._owner.Parent))
            if (owner._theme is { IsDisposed: false } theme && theme.GetTypeVariationBase(_variation).Length > 0) return theme;
        var defaults = ThemeDB.Instance.GetDefaultTheme();
        return defaults.GetTypeVariationBase(_variation).Length > 0 ? defaults : null;
    }
    private void BuildTypes(string type)
    {
        _types.Clear(); _visited.Clear();
        if (!UsesOverride(type)) { _types.AddRange(ThemeDB.NativeDependencies(type)); return; }
        if (_variation.Length > 0 && VariationSource() is { } source)
        {
            var current = _variation;
            while (current.Length > 0)
            {
                if (!_visited.Add(current)) throw new InvalidOperationException("Theme type variations contain a lookup cycle.");
                _types.Add(current); current = source.GetTypeVariationBase(current);
                if (current == _className) break;
            }
        }
        _types.AddRange(_classTypes);
    }
    internal T Get<T>(Store<T> store, string name, string type, bool useOverrides = true)
    {
        CheckQuery(); ValidateName(name); ValidateName(type);
        if (useOverrides && UsesOverride(type) && store.Overrides.TryGetValue(name, out var local)) return local;
        T value; if (_tree is not null && store.Cache.TryGetValue((name, type), out value!)) return value;
        var cacheGeneration = _cacheGeneration; BuildTypes(type);
        for (var owner = this; owner is not null; owner = From(owner._owner.Parent))
            if (owner._theme is { IsDisposed: false } theme)
                foreach (var candidate in _types)
                    if (store.Has(theme, name, candidate)) { value = store.Get(theme, name, candidate); if (_tree is not null && cacheGeneration == _cacheGeneration) store.Cache[(name, type)] = value; return value; }
        var defaults = ThemeDB.Instance.GetDefaultTheme();
        foreach (var candidate in _types)
            if (store.Has(defaults, name, candidate)) { value = store.Get(defaults, name, candidate); if (_tree is not null && cacheGeneration == _cacheGeneration) store.Cache[(name, type)] = value; return value; }
        value = store.Fallback(); if (_tree is not null && cacheGeneration == _cacheGeneration) store.Cache[(name, type)] = value; return value;
    }
    internal bool Has<T>(Store<T> store, string name, string type)
    {
        CheckQuery(); ValidateName(name); ValidateName(type);
        if (UsesOverride(type) && store.Overrides.ContainsKey(name)) return true;
        BuildTypes(type);
        for (var owner = this; owner is not null; owner = From(owner._owner.Parent))
            if (owner._theme is { IsDisposed: false } theme)
                foreach (var candidate in _types) if (store.Has(theme, name, candidate)) return true;
        var defaults = ThemeDB.Instance.GetDefaultTheme();
        foreach (var candidate in _types) if (store.Has(defaults, name, candidate)) return true;
        return false;
    }
    internal float DefaultBaseScale()
    {
        CheckQuery();
        for (var owner = this; owner is not null; owner = From(owner._owner.Parent))
            if (owner._theme is { IsDisposed: false } theme && theme.HasDefaultBaseScale()) return theme.DefaultBaseScale;
        var defaults = ThemeDB.Instance.GetDefaultTheme(); return defaults.HasDefaultBaseScale() ? defaults.DefaultBaseScale : ThemeDB.Instance.FallbackBaseScale;
    }
    internal int DefaultFontSize()
    {
        CheckQuery();
        for (var owner = this; owner is not null; owner = From(owner._owner.Parent))
            if (owner._theme is { IsDisposed: false } theme && theme.HasDefaultFontSize()) return theme.DefaultFontSize;
        var defaults = ThemeDB.Instance.GetDefaultTheme(); return defaults.HasDefaultFontSize() ? defaults.DefaultFontSize : ThemeDB.Instance.FallbackFontSize;
    }
    internal bool HasOverride<T>(Store<T> store, string name) { CheckQuery(); ValidateName(name); return store.Overrides.ContainsKey(name); }
    internal void SetOverride<T>(Store<T> store, string name, T value)
    {
        _mutate(); ValidateName(name); store.Validate?.Invoke(value);
        if (store.Resources)
        {
            if (value is not Resource resource) throw new ArgumentNullException(nameof(value));
            if (resource.IsDisposed) throw new ObjectDisposedException(nameof(value));
            var previous = store.Overrides.TryGetValue(name, out var old) ? old as Resource : null;
            if (!ReferenceEquals(previous, resource)) { if (previous is not null) Unwatch(previous); Watch(resource); }
        }
        store.Overrides[name] = value; OverrideChanged();
    }
    internal void RemoveOverride<T>(Store<T> store, string name)
    {
        _mutate(); ValidateName(name);
        if (store.Overrides.Remove(name, out var old) && store.Resources && old is Resource resource) Unwatch(resource);
        OverrideChanged();
    }
    internal void BeginBulk() { _mutate(); _bulk = true; }
    internal void EndBulk() { _mutate(); if (!_bulk) throw new InvalidOperationException("No bulk theme override is active."); _bulk = false; OverrideChanged(); }
    private void OverrideChanged()
    {
        if (_disposed) return;
        if (_tree is null) { Interlocked.Exchange(ref _invalidated, 1); return; }
        if (_bulk) return;
        if (Environment.CurrentManagedThreadId == _ownerThread) NotifySelf(); else Defer(false, overrideResource: true);
    }
    private void OverrideResourceChanged(Resource _)
    {
        if (_disposed) return;
        try { _mutate(); } catch (ObjectDisposedException) { return; } catch (InvalidOperationException) { Defer(false, overrideResource: true); return; }
        OverrideChanged();
    }
    private void OverrideResourceDisposed(ElectronObject _) => OverrideResourceChanged((Resource)_);
    private void Watch(Resource resource)
    {
        if (_sources.TryGetValue(resource, out var count)) { _sources[resource] = count + 1; return; }
        _sources.Add(resource, 1); resource.Changed += _overrideChanged; resource.Disposed += _overrideDisposed;
    }
    private void Unwatch(Resource resource)
    {
        var count = _sources[resource]; if (count > 1) { _sources[resource] = count - 1; return; }
        _sources.Remove(resource); resource.Changed -= _overrideChanged; resource.Disposed -= _overrideDisposed;
    }
    internal static void ValidateName(string value) { ArgumentNullException.ThrowIfNull(value); if (value.Contains('\0')) throw new ArgumentException("Theme keys cannot contain a null character.", nameof(value)); }
    public void Dispose()
    {
        if (_disposed) return; _disposed = true;
        if (_tree is not null) ThemeDB.Instance.ContextChanged -= _globalChanged;
        _tree = null; _sourceAction = null; _selfAction = null; _overrideAction = null;
        if (_theme is { } theme) { theme.Changed -= _sourceChanged; theme.Disposed -= _sourceDisposed; }
        foreach (var resource in _sources.Keys) { resource.Changed -= _overrideChanged; resource.Disposed -= _overrideDisposed; }
        _sources.Clear(); _theme = null; Colors.Overrides.Clear(); Constants.Overrides.Clear(); FontSizes.Overrides.Clear(); Icons.Overrides.Clear(); Styles.Overrides.Clear(); Invalidate();
    }
    internal IEnumerable<PropertyDescriptor> Properties<TNode>() where TNode : Node
    {
        foreach (var descriptor in ScalarProperties<TNode, Color>("ThemeColorOverride/", Colors, static owner => owner.Colors)) yield return descriptor;
        foreach (var descriptor in ScalarProperties<TNode, int>("ThemeConstantOverride/", Constants, static owner => owner.Constants)) yield return descriptor;
        foreach (var descriptor in ScalarProperties<TNode, int>("ThemeFontSizeOverride/", FontSizes, static owner => owner.FontSizes)) yield return descriptor;
        foreach (var descriptor in ResourceProperties<TNode, Texture>("ThemeIconOverride/", Icons, static owner => owner.Icons)) yield return descriptor;
        foreach (var descriptor in ResourceProperties<TNode, StyleBox>("ThemeStyleBoxOverride/", Styles, static owner => owner.Styles)) yield return descriptor;
    }
    private static IEnumerable<PropertyDescriptor> ScalarProperties<TNode, T>(string prefix, Store<T> store, Func<ThemeOwner, Store<T>> select) where TNode : Node where T : struct
    {
        foreach (var name in store.Overrides.Keys)
            yield return ScalarProperty<TNode, T>(prefix, name, select);
    }
    private static IEnumerable<PropertyDescriptor> ResourceProperties<TNode, T>(string prefix, Store<T?> store, Func<ThemeOwner, Store<T?>> select) where TNode : Node where T : Resource
    {
        foreach (var name in store.Overrides.Keys)
            yield return ResourceProperty<TNode, T>(prefix, name, select);
    }
    private static PropertyDescriptor ScalarProperty<TNode, T>(string prefix, string name, Func<ThemeOwner, Store<T>> select) where TNode : Node where T : struct =>
        new PropertyDescriptor<TNode, T?>(prefix + name,
            node => select(From(node)!).Overrides.TryGetValue(name, out var value) ? value : null,
            (node, value) => { var owner = From(node)!; if (value is { } actual) owner.SetOverride(select(owner), name, actual); else owner.RemoveOverride(select(owner), name); }, _ => null, stored: true);
    private static PropertyDescriptor ResourceProperty<TNode, T>(string prefix, string name, Func<ThemeOwner, Store<T?>> select) where TNode : Node where T : Resource =>
        new PropertyDescriptor<TNode, T?>(prefix + name,
            node => select(From(node)!).Overrides.GetValueOrDefault(name),
            (node, value) => { var owner = From(node)!; if (value is not null) owner.SetOverride(select(owner), name, value); else owner.RemoveOverride(select(owner), name); }, _ => null, stored: true);
    internal static PropertyDescriptor? StoredOverride(Node node, string name, Type valueType) => node switch
    {
        Control => StoredOverride<Control>(name, valueType),
        Window => StoredOverride<Window>(name, valueType),
        _ => null
    };
    private static PropertyDescriptor? StoredOverride<TNode>(string name, Type valueType) where TNode : Node
    {
        if (valueType == typeof(Color?) && name.StartsWith("ThemeColorOverride/", StringComparison.Ordinal))
            return ScalarProperty<TNode, Color>("ThemeColorOverride/", name[19..], static owner => owner.Colors);
        if (valueType == typeof(int?) && name.StartsWith("ThemeConstantOverride/", StringComparison.Ordinal))
            return ScalarProperty<TNode, int>("ThemeConstantOverride/", name[22..], static owner => owner.Constants);
        if (valueType == typeof(int?) && name.StartsWith("ThemeFontSizeOverride/", StringComparison.Ordinal))
            return ScalarProperty<TNode, int>("ThemeFontSizeOverride/", name[22..], static owner => owner.FontSizes);
        if (valueType == typeof(Texture) && name.StartsWith("ThemeIconOverride/", StringComparison.Ordinal))
            return ResourceProperty<TNode, Texture>("ThemeIconOverride/", name[18..], static owner => owner.Icons);
        if (valueType == typeof(StyleBox) && name.StartsWith("ThemeStyleBoxOverride/", StringComparison.Ordinal))
            return ResourceProperty<TNode, StyleBox>("ThemeStyleBoxOverride/", name[22..], static owner => owner.Styles);
        return null;
    }
}
