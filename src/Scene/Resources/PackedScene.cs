using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Stores a reusable in-memory node hierarchy and creates independent runtime instances from it.</summary>
/// <remarks>
/// Runtime packing is typed and uses storage-enabled <see cref="PropertyDescriptor"/> instances.
/// <see cref="ResourceSaver"/> persists registered factories/properties as typed scene archives;
/// loaded instances retain their owned file graphs. Editor metadata, inheritance authoring, placeholders
/// and persistent event endpoints belong to later domains.
/// An inherited node translation domain is omitted from storage so it continues to track its parent after instantiation.
/// </remarks>
public sealed class PackedScene : Resource
{
    private readonly object _gate = new();
    private PackedSceneData _data = PackedSceneData.Empty;
    private SceneState _state;
    private bool _stateExported;
    private bool _packing;

    /// <summary>Initializes an empty packed scene.</summary>
    public PackedScene()
    {
        _state = new SceneState(_data, string.Empty);
    }

    /// <summary>Gets whether this resource contains a scene that can be instantiated.</summary>
    /// <returns><see langword="true"/> after a successful nonempty pack or state copy; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public bool CanInstantiate()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            return _data.Nodes.Length != 0;
        }
    }

    /// <summary>Gets the live read-only metadata object for this resource.</summary>
    /// <returns>A state object that observes later content and path transitions until either object is disposed.</returns>
    /// <remarks>If a caller disposes a previously returned state, the next call creates a replacement.
    /// A returned file-backed view retains its graph after template disposal; dispose that view when finished.</remarks>
    /// <exception cref="ObjectDisposedException">The resource is disposing on another thread or has finished disposing.</exception>
    public SceneState GetState()
    {
        ThrowIfDisposed();
        lock (_gate)
        {
            ThrowIfDisposed();
            var state = EnsureStateUnderLock();
            if (!_stateExported) { state.UpdateFileLease(RetainFileResources()); _stateExported = true; }
            return state;
        }
    }

    /// <summary>Creates an independent detached node hierarchy from the stored scene.</summary>
    /// <param name="editState">The editor-state policy. Runtime instantiation accepts only <see cref="PackedSceneEditState.Disabled"/>.</param>
    /// <returns>The live, detached root node. It has not entered a <see cref="SceneTree"/>.</returns>
    /// <remarks>
    /// Nodes are constructed parent-first. Stored properties and persistent groups are restored before parenting;
    /// owners and typed node references are resolved after the hierarchy is complete, followed by scene-local resources. Only the root receives
    /// <see cref="Node.NotificationSceneInstantiated"/>.
    /// </remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="editState"/> is not defined.</exception>
    /// <exception cref="NotSupportedException"><paramref name="editState"/> requests editor-only behavior.</exception>
    /// <exception cref="InvalidOperationException">The scene is empty or stored node schema cannot be reconstructed safely.</exception>
    /// <exception cref="ObjectDisposedException">The resource or a referenced stored resource is disposing or disposed.</exception>
    /// <exception cref="Exception">A factory, property setter, setup callback, notification, or cleanup operation fails.</exception>
    public Node Instantiate(PackedSceneEditState editState = PackedSceneEditState.Disabled)
    {
        ThrowIfDisposed();

        if (!Enum.IsDefined(editState))
            throw new ArgumentOutOfRangeException(nameof(editState), editState, "The packed-scene edit state is not defined.");

        if (editState != PackedSceneEditState.Disabled)
            throw new NotSupportedException("Editor scene-instancing metadata is not available in the runtime library.");

        PackedSceneData data;
        string resourcePath;
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_data.Nodes.Length == 0)
                throw new InvalidOperationException("An empty packed scene cannot be instantiated.");

            data = _data;
            resourcePath = ResourcePath;
        }

        IDisposable? fileLease = RetainFileResources();
        var nodes = new Node[data.Nodes.Length];
        var createdCount = 0;
        var resources = CreateSceneDuplicationScope();

        try
        {
            for (var index = 0; index < data.Nodes.Length; index++)
            {
                var stored = data.Nodes[index];
                var node = stored.Factory.Create();
                nodes[index] = node;
                createdCount = index + 1;
                node.BeginSceneInstantiation();
                ValidateFactoryResult(stored, node);

                node.Name = stored.Name;
                RestoreProperties(node, stored, resources);

                foreach (var group in stored.Groups)
                    node.AddToGroup(group, persistent: true);

                if (stored.ParentIndex >= 0)
                    nodes[stored.ParentIndex].AddChild(node);
            }

            for (var index = 0; index < data.Nodes.Length; index++)
            {
                var ownerIndex = data.Nodes[index].OwnerIndex;
                if (ownerIndex >= 0)
                    nodes[index].Owner = nodes[ownerIndex];
            }

            for (var index = 0; index < data.Nodes.Length; index++)
                RestoreProperties(nodes[index], data.Nodes[index], resources, nodeReferences: true);

            var root = nodes[0];
            if (!IsBuiltInPath(resourcePath))
                root.SetSceneFilePath(resourcePath);

            resources.AssignLocalScene(root);
            resources.SetupLocalResources();
            root.AdoptSceneResources(resources.ReleaseCreated());
            root.AdoptSceneFileLease(fileLease); fileLease = null;
            ValidateInstantiatedHierarchy(data, nodes);
            root.Notify(Node.NotificationSceneInstantiated);
            ValidateInstantiatedHierarchy(data, nodes);
            EndSceneInstantiation(nodes, createdCount);
            root.SpawnSceneIdentity = this;
            return root;
        }
        catch (Exception instantiationError)
        {
            EndSceneInstantiation(nodes, createdCount);
            var cleanupErrors = CleanupFailedInstance(nodes, createdCount, resources);
            try { fileLease?.Dispose(); } catch (Exception cleanup) { cleanupErrors.Add(cleanup); }
            if (cleanupErrors.Count == 0)
                ExceptionDispatchInfo.Capture(instantiationError).Throw();

            cleanupErrors.Insert(0, instantiationError);
            throw new AggregateException("Scene instantiation and rollback failed.", cleanupErrors);
        }

        throw new InvalidOperationException("Unreachable scene-instantiation state.");
    }

    /// <summary>Replaces this resource's contents with a typed snapshot of a node hierarchy.</summary>
    /// <param name="root">The live root to capture. A <see langword="null"/> argument is rejected without changing existing state.</param>
    /// <remarks>
    /// The root is always captured. A descendant branch is captured only when its first node is owned by
    /// <paramref name="root"/>; rejected branches are pruned. After capture starts, any failure leaves this resource empty.
    /// </remarks>
    /// <exception cref="ArgumentNullException"><paramref name="root"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">Capture is re-entered, the hierarchy changes, or a node factory is unsafe.</exception>
    /// <exception cref="NotSupportedException">A stored property uses an unsupported typed representation.</exception>
    /// <exception cref="ObjectDisposedException">This resource, a captured node, or a captured resource is disposing or disposed.</exception>
    /// <exception cref="Exception">Property discovery, capture, change notification, or cleanup fails.</exception>
    public void Pack(Node root)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(root);
        ObjectDisposedException.ThrowIf(root.IsDisposed, root);

        Exception? captureError = null;

        lock (_gate)
        {
            ThrowIfDisposed();
            if (_packing)
                throw new InvalidOperationException("Packed-scene capture cannot be re-entered.");

            _packing = true;
            ReplaceDataUnderLock(PackedSceneData.Empty);
            Node[]? captured = null;

            try
            {
                captured = root.BeginSceneCapture();
                ReplaceDataUnderLock(Capture(root));
            }
            catch (Exception error)
            {
                captureError = error;
            }
            finally
            {
                if (captured is not null)
                    Node.EndSceneCapture(captured);
                _packing = false;
            }
        }

        Exception? changedError = null;
        try
        {
            EmitChanged();
        }
        catch (Exception error)
        {
            changedError = error;
        }

        if (captureError is not null && changedError is not null)
            throw new AggregateException("Scene capture and change notification failed.", captureError, changedError);
        if (captureError is not null)
            ExceptionDispatchInfo.Capture(captureError).Throw();
        if (changedError is not null)
            ExceptionDispatchInfo.Capture(changedError).Throw();
    }

    internal override void OnFileOwnershipChanged()
    {
        lock (_gate) if (_stateExported && !_state.IsDisposed) _state.UpdateFileLease(RetainFileResources());
    }
    internal PackedSceneData FileData { get { ThrowIfDisposed(); lock (_gate) return _data; } }
    internal void LoadFileData(PackedSceneData data) { ThrowIfDisposed(); lock (_gate) ReplaceDataUnderLock(data); }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PackedScene();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(
        Resource target,
        bool deep,
        DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource,
        Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var packedTarget = (PackedScene)target;
        PackedSceneData data;

        lock (_gate)
        {
            ThrowIfDisposed();
            data = _data;
        }

        if (deep)
        {
            data = data.TransformResources(resource => duplicateSubresource(resource) ??
                throw new InvalidOperationException("A scene resource duplicate cannot be null."));
        }

        lock (packedTarget._gate)
            packedTarget.ReplaceDataUnderLock(data);

        packedTarget.EmitChanged();
    }

    /// <inheritdoc />
    protected override void OnResetState()
    {
        lock (_gate)
            ReplaceDataUnderLock(PackedSceneData.Empty);

        EmitChanged();
    }

    /// <inheritdoc />
    protected override void OnResourcePathChanged(string path)
    {
        lock (_gate)
        {
            path = ResourcePath;
            var state = _state;
            if (state.IsDisposed)
            {
                _state = new SceneState(_data, path); _stateExported = false;
                return;
            }

            try
            {
                state.UpdatePath(path);
            }
            catch (ObjectDisposedException) when (state.IsDisposed && ReferenceEquals(_state, state))
            {
                _state = new SceneState(_data, path); _stateExported = false;
            }
        }
    }

    private static PackedSceneData Capture(Node root)
    {
        var selected = new List<Node> { root };
        AddOwnedChildren(root, root, selected);
        var indices = new Dictionary<Node, int>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < selected.Count; index++)
            indices.Add(selected[index], index);
        var nodes = new SceneNodeData[selected.Count];

        for (var index = 0; index < selected.Count; index++)
        {
            var node = selected[index];
            var parentIndex = node.Parent is not null && indices.TryGetValue(node.Parent, out var storedParent)
                ? storedParent
                : -1;
            var ownerIndex = node.Owner is not null && indices.TryGetValue(node.Owner, out var storedOwner)
                ? storedOwner
                : -1;
            var path = index == 0
                ? "."
                : parentIndex == 0 ? node.Name : $"{nodes[parentIndex].Path}/{node.Name}";
            var parentPath = parentIndex < 0 ? "." : nodes[parentIndex].Path;
            var ownerPath = ownerIndex < 0 ? string.Empty : ownerIndex == 0 ? "." : nodes[ownerIndex].Path;
            var properties = node.GetPropertyList()
                .Where(property => property.IsStored && property.Name != nameof(Node.Name) &&
                    !(property.Name == nameof(Node.TranslationDomain) && node.IsTranslationDomainInherited))
                .Select(property => new ScenePropertyData(property.Name, property.CaptureStoredValue(node)))
                .ToArray();

            nodes[index] = new SceneNodeData(
                new SceneFactoryData(node.CaptureSceneInstanceFactory(), node.GetType(), node.InstanceID),
                node.Name,
                parentIndex,
                ownerIndex,
                -1,
                path,
                parentPath,
                ownerPath,
                node.GetPersistentGroups(),
                properties,
                null,
                string.Empty);
        }

        return new PackedSceneData(nodes);
    }

    private static void AddOwnedChildren(Node parent, Node root, List<Node> selected)
    {
        foreach (var child in parent.Children)
        {
            if (!ReferenceEquals(child.Owner, root))
                continue;

            selected.Add(child);
            AddOwnedChildren(child, root, selected);
        }
    }

    private static bool IsBuiltInPath(string path) =>
        path.Length == 0 || path.Contains("::", StringComparison.Ordinal) || path.StartsWith("local://", StringComparison.Ordinal);

    private static void ValidateFactoryResult(SceneNodeData stored, Node? node)
    {
        if (node is null)
            throw new InvalidOperationException($"Factory for {stored.Factory.RuntimeType.Name} returned null.");
        if (node.GetType() != stored.Factory.RuntimeType)
            throw new InvalidOperationException($"Factory for {stored.Factory.RuntimeType.Name} returned {node.GetType().Name}.");
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        if (node.Parent is not null || node.Tree is not null || node.Children.Count != 0 || node.Owner is not null ||
            node.IsQueuedForDeletion || node.SceneFilePath.Length != 0)
        {
            throw new InvalidOperationException(
                $"Factory for {stored.Factory.RuntimeType.Name} must return a live default detached node.");
        }
    }

    private static void RestoreProperties(
        Node node,
        SceneNodeData stored,
        Resource.SceneDuplicationScope resources,
        bool nodeReferences = false)
    {
        var descriptors = node.GetPropertyList().ToDictionary(property => property.Name, StringComparer.Ordinal);
        foreach (var property in stored.Properties)
        {
            if ((property.Value is StoredNodeReferenceValue) != nodeReferences) continue;
            if (!descriptors.TryGetValue(property.Name, out var descriptor))
            {
                // A preceding stored count can expose indexed typed properties on this same node.
                descriptors = node.GetPropertyList().ToDictionary(candidate => candidate.Name, StringComparer.Ordinal);
                descriptors.TryGetValue(property.Name, out descriptor);
            }
            if (descriptor is null)
                descriptor = ThemeOwner.StoredOverride(node, property.Name, property.Value.ValueType);
            if (descriptor is null || !descriptor.IsStored ||
                descriptor.ValueType != property.Value.ValueType)
            {
                throw new InvalidOperationException(
                    $"Stored property '{property.Name}' is not available with the captured schema on {stored.Factory.RuntimeType.Name}.");
            }

            descriptor.RestoreStoredValue(node, property.Value, descriptor.AlwaysDuplicateResource ? resources.ResolveForced : resources.Resolve);
        }
    }

    private static List<Exception> CleanupFailedInstance(
        Node[] nodes,
        int createdCount,
        Resource.SceneDuplicationScope resources)
    {
        var errors = resources.DisposeCreated();
        var created = new HashSet<Node>(ReferenceEqualityComparer.Instance);
        for (var index = 0; index < createdCount; index++)
        {
            if (nodes[index] is not null)
                created.Add(nodes[index]);
        }

        foreach (var node in created)
        {
            if (node.IsDisposed)
                continue;

            foreach (var child in node.Children.Where(child => !created.Contains(child)).ToArray())
            {
                try
                {
                    node.RemoveChild(child);
                }
                catch (Exception error)
                {
                    errors.Add(error);
                }
            }
        }

        for (var index = createdCount - 1; index >= 0; index--)
        {
            var node = nodes[index];
            if (node is null || node.IsDisposed)
                continue;

            try
            {
                node.Dispose();
            }
            catch (Exception error)
            {
                errors.Add(error);
            }
        }

        return errors;
    }

    private static void ValidateInstantiatedHierarchy(PackedSceneData data, Node[] nodes)
    {
        var childCursors = new int[nodes.Length];

        for (var index = 0; index < nodes.Length; index++)
        {
            var node = nodes[index];
            ObjectDisposedException.ThrowIf(node.IsDisposed, node);
            var stored = data.Nodes[index];
            var expectedParent = stored.ParentIndex < 0 ? null : nodes[stored.ParentIndex];
            var expectedOwner = stored.OwnerIndex < 0 ? null : nodes[stored.OwnerIndex];

            if (!ReferenceEquals(node.Parent, expectedParent) || !ReferenceEquals(node.Owner, expectedOwner) ||
                node.Tree is not null || node.IsQueuedForDeletion)
                throw new InvalidOperationException("A callback moved a node outside the hierarchy during scene instantiation.");

            if (stored.ParentIndex >= 0)
            {
                var cursor = childCursors[stored.ParentIndex]++;
                if ((uint)cursor >= (uint)expectedParent!.Children.Count ||
                    !ReferenceEquals(expectedParent.Children[cursor], node))
                {
                    throw new InvalidOperationException("A callback changed child order during scene instantiation.");
                }
            }
        }

        for (var index = 0; index < nodes.Length; index++)
        {
            if (childCursors[index] != nodes[index].Children.Count)
                throw new InvalidOperationException("A callback changed the hierarchy during scene instantiation.");
        }
    }

    private static void EndSceneInstantiation(Node[] nodes, int createdCount)
    {
        for (var index = 0; index < createdCount; index++)
            nodes[index]?.EndSceneInstantiation();
    }

    private SceneState EnsureStateUnderLock()
    {
        if (_state.IsDisposed) { _state = new SceneState(_data, ResourcePath); _stateExported = false; }
        return _state;
    }

    private void ReplaceDataUnderLock(PackedSceneData data)
    {
        _data = data;
        var path = ResourcePath;

        var state = _state;
        if (state.IsDisposed)
        {
            _state = new SceneState(data, path); _stateExported = false;
            return;
        }

        try
        {
            state.Replace(data, path);
        }
        catch (ObjectDisposedException) when (state.IsDisposed && ReferenceEquals(_state, state))
        {
            _state = new SceneState(data, path); _stateExported = false;
        }
    }
}
