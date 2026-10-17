namespace Electron2D;

/// <summary>Retains a scene path and typed pending values without constructing that scene.</summary>
/// <remarks>This is a transform-free Node. Existing children use the ordinary nonspatial parent semantics.
/// Creation is an explicit cold operation on the scene owner.</remarks>
public sealed partial class InstancePlaceholder : Node
{
    private string _instancePath = string.Empty;
    private readonly MissingResourceData _fields = new();
    private bool _creating;

    /// <summary>Creates an empty placeholder. A custom scene can supply its later instance.</summary>
    public InstancePlaceholder() { }

    /// <summary>Gets the scene path used by default during creation.</summary>
    /// <returns>The stored path, or empty for a manually constructed placeholder.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off the scene owner.</exception>
    public string GetInstancePath() { EnsurePlaceholderAccess(); return _instancePath; }

    /// <summary>Gets pending property names in the order in which they will be applied.</summary>
    /// <returns>An independent ordered array. Ordinary Node properties are applied directly to this node.</returns>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off the scene owner.</exception>
    public string[] GetStoredPropertyNames() { EnsurePlaceholderAccess(); return _fields.Fields.Keys.ToArray(); }

    /// <summary>Reads a pending value through a compatible requested type.</summary>
    /// <typeparam name="TValue">The requested stored type.</typeparam>
    /// <param name="name">The pending property name.</param>
    /// <returns>The typed snapshot; arrays are copied. Node references are queried as string paths.</returns>
    /// <exception cref="ArgumentException">The name is blank.</exception>
    /// <exception cref="KeyNotFoundException">The property is not pending.</exception>
    /// <exception cref="InvalidCastException">The value is incompatible with the requested type.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    /// <exception cref="InvalidOperationException">Attached access is off the scene owner.</exception>
    public TValue GetStoredValue<TValue>(string name)
    {
        EnsurePlaceholderAccess(); ArgumentException.ThrowIfNullOrWhiteSpace(name);
        if (!_fields.Fields.TryGetValue(name, out var field)) throw new KeyNotFoundException("No pending property has that name.");
        if (new DeferredPropertyValue(field).TryGetValue<TValue>(out var value)) return value;
        throw new InvalidCastException($"Pending property '{name}' is incompatible with {typeof(TValue).Name}.");
    }

    /// <summary>Loads and creates a sibling from the stored path or supplied scene.</summary>
    /// <param name="replace">Whether to remove and queue this placeholder after preparation.</param>
    /// <param name="customScene">An optional borrowed template replacing path lookup.</param>
    /// <returns>The created sibling; null when this node is detached or has no parent.</returns>
    /// <remarks>The sibling occupies this node's former position. Name and multiplayer authority follow this
    /// node. Existing children remain here and are not transferred. Template notification precedes pending
    /// overrides; active-tree entry follows them. Load/property failures preserve this placeholder. Callback
    /// failures after structural commitment propagate with completed progress retained.</remarks>
    /// <exception cref="ObjectDisposedException">This node or a required resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Creation is reentered or hierarchy mutation is unavailable.</exception>
    /// <exception cref="Exception">Loading, typed restoration, lifecycle delivery or rollback fails.</exception>
    public Node? CreateInstance(bool replace = false, PackedScene? customScene = null)
    {
        EnsurePlaceholderCreation();
        if (_creating) throw new InvalidOperationException("Placeholder creation cannot be reentered.");
        if (Tree is null || Parent is null) return null;
        var parent = Parent; var index = GetIndex(); var name = Name; var authority = GetMultiplayerAuthority();
        var captured = BeginSceneCapture(); Node? instance = null; _creating = true;
        try
        {
            var scene = customScene; IDisposable? retention = null;
            if (scene is null) scene = ResourceLoader.LoadSceneTemplate(_instancePath, out retention);
            try { instance = scene.Instantiate(); }
            finally { retention?.Dispose(); }
            CopySceneFileLeasesTo(instance); instance.Name = name; instance.SetMultiplayerAuthority(authority);
            PackedScene.ApplyPlaceholderProperties(instance, _fields.Fields.Values.ToArray(), this);
            EndSceneCapture(captured); captured = [];
            EnsurePlaceholderCreation();
            if (Parent != parent || Tree != parent.Tree) throw new InvalidOperationException("The placeholder hierarchy changed during creation.");
            AttachPlaceholderInstance(instance, parent, index, replace);
            return instance;
        }
        catch (Exception error)
        {
            if (captured.Length != 0) { EndSceneCapture(captured); captured = []; }
            if (instance is { IsDisposed: false, Parent: null, Tree: null })
                try { instance.Dispose(); } catch (Exception cleanup) { throw new AggregateException(error, cleanup); }
            throw;
        }
        finally { if (captured.Length != 0) EndSceneCapture(captured); _creating = false; }
    }

    internal MissingResourceData PendingFields => _fields;
    internal void InitializePending(string path, IEnumerable<MissingResourceData.Field> fields)
    {
        _instancePath = path; _fields.Recording = true;
        try { foreach (var field in fields) _fields.Put(field); }
        finally { _fields.Recording = false; }
    }
    internal void SetPendingField(string name, MissingResourceData.Field field)
    {
        EnsurePendingPropertyMutation();
        if (field.Name != name) throw new InvalidOperationException("Pending property identity cannot change.");
        _fields.Put(field);
    }
    private static Node CreatePlaceholder() => new InstancePlaceholder();
    /// <summary>Provides the compiled factory used for standalone placeholder capture.</summary>
    /// <returns>A static factory creating an empty placeholder.</returns>
    protected override Func<Node> CreateSceneInstanceFactory() => CreatePlaceholder;
}
