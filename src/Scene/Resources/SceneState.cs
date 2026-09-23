using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Provides read-only typed metadata for the current contents of a <see cref="PackedScene"/>.</summary>
/// <remarks>
/// Instances are created by <see cref="PackedScene.GetState"/>. A live state object tracks every content and path
/// transition of its source resource, while retaining its final snapshot if that resource is later disposed.
/// </remarks>
public sealed class SceneState : ElectronObject
{
    private readonly object _gate = new();
    private PackedSceneData _data;
    private string _path;

    internal SceneState(PackedSceneData data, string path)
    {
        _data = data;
        _path = path;
    }

    /// <summary>Gets the inherited base scene state.</summary>
    /// <returns>Always <see langword="null"/> until scene inheritance is available.</returns>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public SceneState? GetBaseSceneState()
    {
        ThrowIfDisposed();
        return null;
    }

    /// <summary>Gets the number of persistent connections stored in this state.</summary>
    /// <returns>Zero because typed persistent connection endpoints are not yet part of the runtime contract.</returns>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public int GetConnectionCount()
    {
        ThrowIfDisposed();
        return 0;
    }

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
    /// <returns><c>-1</c> for ordinary locally captured nodes.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public int GetNodeIndex(int nodeIndex) => ReadNode(nodeIndex).SiblingIndex;

    /// <summary>Gets the nested packed scene associated with a node.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>The nested scene, or <see langword="null"/> for a locally captured node.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public PackedScene? GetNodeInstance(int nodeIndex) => ReadNode(nodeIndex).Instance;

    /// <summary>Gets the resource path represented by a node placeholder.</summary>
    /// <param name="nodeIndex">The zero-based node index.</param>
    /// <returns>An empty string because runtime-authored placeholders are not supported.</returns>
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
    public int GetNodePropertyCount(int nodeIndex) => ReadNode(nodeIndex).Properties.Length;

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
    /// <returns>The captured property value; stored vector, color and contour-index arrays are copied on each read.</returns>
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
    /// <returns>The unqualified runtime type name captured by the scene.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="nodeIndex"/> is outside the state.</exception>
    /// <exception cref="ObjectDisposedException">This state has been disposed.</exception>
    public string GetNodeType(int nodeIndex) => ReadNode(nodeIndex).Factory.RuntimeType.Name;

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
    /// <returns><see langword="false"/> for every runtime-authored node.</returns>
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
        return (uint)propertyIndex < (uint)node.Properties.Length
            ? node.Properties[propertyIndex]
            : throw new ArgumentOutOfRangeException(
                nameof(propertyIndex), propertyIndex, "Property index is outside the stored node.");
    }
}

internal sealed record PackedSceneData(SceneNodeData[] Nodes)
{
    internal static readonly PackedSceneData Empty = new([]);

    internal PackedSceneData TransformResources(Func<Resource, Resource> transform)
    {
        ArgumentNullException.ThrowIfNull(transform);
        return new PackedSceneData(
            Nodes.Select(node => node with
            {
                Properties = node.Properties
                    .Select(property => property with { Value = property.Value.TransformResources(transform) })
                    .ToArray()
            }).ToArray());
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
    string InstancePlaceholder);

internal sealed record ScenePropertyData(string Name, StoredPropertyValue Value);

internal sealed class SceneFactoryData(Func<Node> create, Type runtimeType, ulong sourceInstanceId)
{
    private readonly ConditionalWeakTable<Node, object> _issued = new();

    internal Type RuntimeType { get; } = runtimeType;

    internal Node Create()
    {
        var node = Node.InvokeSceneInstanceFactory(create) ??
                   throw new InvalidOperationException("A scene instance factory returned null.");

        if (node.InstanceID == sourceInstanceId || !_issued.TryAdd(node, new object()))
            throw new InvalidOperationException("A scene instance factory returned a source or previously issued node.");

        return node;
    }
}
