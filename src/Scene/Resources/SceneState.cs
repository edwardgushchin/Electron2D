using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Provides read-only typed metadata for the current contents of a <see cref="PackedScene"/>.</summary>
/// <remarks>
/// Instances are created by <see cref="PackedScene.GetState"/>. A live state object tracks every content and path
/// transition of its source resource, while retaining its final snapshot if that resource is later disposed.
/// A returned file-backed view retains its resource graph and must be disposed when finished.
/// </remarks>
public sealed class SceneState : ElectronObject
{
    private readonly object _gate = new();
    private PackedSceneData _data;
    private string _path;
    private IDisposable? _fileLease;

    internal SceneState(PackedSceneData data, string path)
    {
        _data = data;
        _path = path;
    }

    internal void UpdateFileLease(IDisposable? lease)
    {
        IDisposable? old;
        lock (_gate) { if (IsDisposed) { old = lease; } else { old = _fileLease; _fileLease = lease; } }
        old?.Dispose();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try { if (disposing) { IDisposable? lease; lock (_gate) { lease = _fileLease; _fileLease = null; } lease?.Dispose(); } }
        finally { base.Dispose(disposing); }
    }
    /// <summary>Gets the inherited base scene state.</summary>
    /// <returns>Always <see langword="null"/> until scene inheritance is available.</returns>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public SceneState? GetBaseSceneState()
    {
        ThrowIfDisposed();
        return null;
    }

    /// <summary>Gets the number of persistent authored connections, including inactive recovery records, captured in this state.</summary>
    /// <returns>The current persistent connection-record count.</returns>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public int GetConnectionCount() => Read(data => data.Connections.Length);
    /// <summary>Gets the publisher's path relative to the packed root.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public string GetConnectionSource(int connectionIndex) => Read(data => data.Nodes[ReadConnection(data, connectionIndex).PublisherIndex].Path);
    /// <summary>Gets the receiver's path relative to the packed root.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public string GetConnectionTarget(int connectionIndex) => Read(data => data.Nodes[ReadConnection(data, connectionIndex).SubscriberIndex].Path);
    /// <summary>Gets the registered diagnostic event label.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public string GetConnectionSignal(int connectionIndex) => Read(data => ReadConnection(data, connectionIndex).Schema.Signal);
    /// <summary>Gets the registered diagnostic handler label.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public string GetConnectionMethod(int connectionIndex) => Read(data => ReadConnection(data, connectionIndex).Schema.Method);
    /// <summary>Gets persistent (2), deferred (1) and one-shot (4) delivery bits.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public int GetConnectionFlags(int connectionIndex) => Read(data => ReadConnection(data, connectionIndex).Schema.Flags);
    /// <summary>Gets the event argument count omitted by the compiled adapter.</summary>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the captured table.</exception>
    /// <exception cref="ObjectDisposedException">This state is disposed.</exception>
    public int GetConnectionUnbinds(int connectionIndex) => Read(data => ReadConnection(data, connectionIndex).Schema.Unbinds);
    /// <summary>Reads an independent snapshot of the exact typed bound payload.</summary>
    /// <typeparam name="TBindings">The payload type used by the registered binding.</typeparam>
    /// <param name="connectionIndex">The zero-based captured connection index.</param>
    /// <exception cref="InvalidOperationException">The requested payload type is incompatible.</exception>
    public TBindings GetConnectionBinds<TBindings>(int connectionIndex) => Read(data =>
        ReadConnection(data, connectionIndex).Bindings.ValueType == typeof(TBindings) &&
        ReadConnection(data, connectionIndex).Bindings.TryGetValue<TBindings>(out var value) ? value :
        throw new InvalidOperationException("Connection payload type does not match."));
    private static SceneConnectionData ReadConnection(PackedSceneData data, int index) =>
        (uint)index < (uint)data.Connections.Length ? data.Connections[index] : throw new ArgumentOutOfRangeException(nameof(index));

    /// <summary>Gets the number of stored nodes.</summary>
    /// <returns>Zero for an empty state; otherwise the captured node count.</returns>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public int GetNodeCount() => Read(data => data.Nodes.Length);

    /// <summary>Gets the persistent groups stored for a node.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>An immutable group-name snapshot in ordinal order.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public IReadOnlyList<string> GetNodeGroups(int nodeIndex) => ReadNode(nodeIndex).Groups;

    /// <summary>Gets the stored sibling index used by an instanced subscene override.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The captured position for additions or overrides inside an instance; <c>-1</c> for ordinary local nodes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public int GetNodeIndex(int nodeIndex) => ReadNode(nodeIndex).SiblingIndex;

    /// <summary>Gets the nested packed scene associated with a node.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The borrowed linked template, or <see langword="null"/> for a local node or reused child override.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public PackedScene? GetNodeInstance(int nodeIndex) => ReadNode(nodeIndex).Instance;

    /// <summary>Gets the resource path represented by a node placeholder.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The late scene path for a deferred root, or empty for other records.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodeInstancePlaceholder(int nodeIndex) => ReadNode(nodeIndex).InstancePlaceholder;

    /// <summary>Gets a stored node's name.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The captured name.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodeName(int nodeIndex) => ReadNode(nodeIndex).Name;

    /// <summary>Gets the path of a stored node's owner.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns><c>.</c> for nodes owned by the root, or an empty string when no owner is stored.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodeOwnerPath(int nodeIndex) => ReadNode(nodeIndex).OwnerPath;

    /// <summary>Gets a stored node path or its parent's path.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <param name="forParent"><see langword="true"/> to return the stored parent path.</param>
    /// <returns>A relative scene path; the root is represented by <c>.</c>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodePath(int nodeIndex, bool forParent = false)
    {
        var node = ReadNode(nodeIndex);
        return forParent ? node.ParentPath : node.Path;
    }

    /// <summary>Gets the number of stored properties for a node.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The property count.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public int GetNodePropertyCount(int nodeIndex) => ReadNode(nodeIndex).StoredPropertyCount;

    /// <summary>Gets the name of a stored node property.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <param name="propertyIndex">The zero-based property index.</param>
    /// <returns>The property name.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodePropertyName(int nodeIndex, int propertyIndex) =>
        ReadProperty(nodeIndex, propertyIndex).Name;

    /// <summary>Gets the declared type of a stored node property.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <param name="propertyIndex">The zero-based property index.</param>
    /// <returns>The exact declared property type.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    internal Type GetNodePropertyType(int nodeIndex, int propertyIndex) =>
        ReadProperty(nodeIndex, propertyIndex).Value.ValueType;

    /// <summary>Gets a stored node property through a requested compatible type.</summary>
    /// <typeparam name="TValue">The requested result type.</typeparam>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <param name="propertyIndex">The zero-based property index.</param>
    /// <returns>The captured property value; stored arrays are copied on each read. For a stored node reference, request
    /// string to obtain its relative path, or an empty string for null. No original or instantiated node is returned.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Either index is outside the state.</exception>
    /// <exception cref="InvalidCastException">The captured value is not compatible with <typeparamref name="TValue"/>.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public TValue GetNodePropertyValue<TValue>(int nodeIndex, int propertyIndex)
    {
        var property = ReadProperty(nodeIndex, propertyIndex);
        if (property.Value.TryGetValue<TValue>(out var value))
            return value;

        throw new InvalidCastException(
            $"Stored property '{property.Name}' has type {property.Value.ValueType.Name}, not {typeof(TValue).Name}.");
    }

    /// <summary>Gets a stored node's runtime type name.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The unqualified runtime type name for a fresh local node; empty for a linked root or reused override.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodeType(int nodeIndex)
    {
        var node = ReadNode(nodeIndex);
        return node.Instance is not null || node.InstanceRootIndex >= 0 || node.InstancePlaceholder.Length != 0 ? string.Empty : node.Factory.RuntimeType.Name;
    }

    /// <summary>Gets the resource path associated with this state.</summary>
    /// <returns>The current path of its live packed scene, or the last path observed before that resource was disposed.</returns>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetPath()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return _path;
        }
    }

    /// <summary>Gets whether a stored node is an instance placeholder.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>True when this record stores a deferred scene path.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public bool IsNodeInstancePlaceholder(int nodeIndex) => ReadNode(nodeIndex).InstancePlaceholder.Length != 0;

    internal void Replace(PackedSceneData data, string path)
    {
        ArgumentNullException.ThrowIfNull(data);
        ArgumentNullException.ThrowIfNull(path);
        ThrowIfDisposed();

        lock (_gate)
        {
            ThrowIfDisposed();
            _data = data;
            _path = path;
        }
    }

    internal void UpdatePath(string path)
    {
        ArgumentNullException.ThrowIfNull(path);
        ThrowIfDisposed();

        lock (_gate)
        {
            ThrowIfDisposed();
            _path = path;
        }
    }

    private TResult Read<TResult>(Func<PackedSceneData, TResult> read)
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return read(_data);
        }
    }

    private SceneNodeData ReadNode(int nodeIndex) => Read(
        data => (uint)nodeIndex < (uint)data.Nodes.Length
            ? data.Nodes[nodeIndex]
            : throw new ArgumentOutOfRangeException(nameof(nodeIndex), nodeIndex, "Node index is outside the scene state."));

    private ScenePropertyData ReadProperty(int nodeIndex, int propertyIndex)
    {
        var node = ReadNode(nodeIndex);
        return (uint)propertyIndex < (uint)node.StoredPropertyCount
            ? node.ReadStoredProperty(propertyIndex)
            : throw new ArgumentOutOfRangeException(
                nameof(propertyIndex), propertyIndex, "Property index is outside the stored node.");
    }
}

internal sealed record PackedSceneData(SceneNodeData[] Nodes, SceneConnectionData[] Connections)
{
    internal PackedSceneData(SceneNodeData[] nodes) : this(nodes, []) { }
    internal static readonly PackedSceneData Empty = new([]);

    internal PackedSceneData TransformResources(Func<Resource, Resource> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return new PackedSceneData(
            Nodes.Select(node => node with
            {
                Factory = node.Factory.TransformResources(transform),
                Instance = node.Instance is { } instance ? transform(instance) as PackedScene ?? throw new InvalidOperationException("A linked scene reference must remain a PackedScene.") : null,
                DeferredProperties = node.DeferredProperties?.Copy(resource => resource is null ? null : transform(resource)),
                Properties = node.Properties
                    .Select(property => property with { Value = property.Value.TransformResources(transform) })
                    .ToArray()
            }).ToArray(), Connections.Select(c => c with { Bindings = c.Bindings.TransformResources(transform) }).ToArray());
    }
}

internal sealed record SceneNodeData(
    SceneFactoryData Factory,
    string Name,
    int ParentIndex,
    int OwnerIndex,
    int SiblingIndex,
    string Path,
    string ParentPath,
    string OwnerPath,
    IReadOnlyList<string> Groups,
    ScenePropertyData[] Properties,
    PackedScene? Instance,
    string InstancePlaceholder,
    int InstanceRootIndex = -1,
    string InstancePath = "",
    MissingResourceData? DeferredProperties = null)
{
    internal int StoredPropertyCount => Properties.Length + (DeferredProperties?.Fields.Count ?? 0);
    internal ScenePropertyData ReadStoredProperty(int index)
    {
        if ((uint)index >= (uint)StoredPropertyCount) throw new ArgumentOutOfRangeException(nameof(index));
        if (index < Properties.Length) return Properties[index];
        var field = DeferredProperties!.Fields.Values.ElementAt(index - Properties.Length);
        return new(field.Name, new DeferredPropertyValue(field));
    }
}

internal sealed record ScenePropertyData(string Name, StoredPropertyValue Value);

internal sealed class SceneFactoryData(Func<Node> create, Type runtimeType, ulong sourceInstanceId, string originalClass = "", string originalScene = "", Script? script = null)
{
    private readonly ConditionalWeakTable<Node, object> _issued = new();

    internal Type RuntimeType { get; } = runtimeType;
    internal string OriginalClass { get; } = originalClass;
    internal string OriginalScene { get; } = originalScene;
    internal Script? Script { get; } = script;
    internal ulong SourceInstanceID => sourceInstanceId;

    internal SceneFactoryData TransformResources(Func<Resource, Resource> transform) => Script is null ? this :
        new(create, RuntimeType, sourceInstanceId, OriginalClass, OriginalScene,
            transform(Script) as Script ?? throw new InvalidOperationException("A compiled source reference must remain a Script."));

    internal Node Create(Resource.SceneDuplicationScope resources)
    {
        using var extensionConstruction = ExtensionBindings.BeginConstruction(RuntimeType);
        var source = Script is null ? null : (Script)resources.Resolve(Script);
        var node = Node.InvokeSceneInstanceFactory(create, source is null ? null : (source, RuntimeType)) ??
                   throw new InvalidOperationException("A scene instance factory returned null.");

        if (node.InstanceID == sourceInstanceId || !_issued.TryAdd(node, new object()))
            throw new InvalidOperationException("A scene instance factory returned a source or previously issued node.");

        if (node is MissingNode missing) missing.SetRecoveryIdentity(OriginalClass, OriginalScene);
        return node;
    }
}
