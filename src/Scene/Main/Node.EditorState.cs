namespace Electron2D;

public partial class Node
{
    private string _editorDescription = string.Empty;
    private bool _displayFolded, _editableInstance;
    private HashSet<string>? _pinnedSceneProperties;
    private const string StoredSceneOrigin = "__SceneInstanceOrigin";
    private const string StoredEditableInstance = "__EditableInstance";
    internal const string StoredPinnedSceneProperties = "__PinnedSceneProperties";
    private static readonly PropertyDescriptor<Node, string> SceneOriginProperty = new(StoredSceneOrigin,
        node => node.SceneFilePath, (node, value) => node.SetSceneFilePath(value), _ => string.Empty, stored: true);
    private static readonly PropertyDescriptor<Node, bool> EditableInstanceProperty = new(StoredEditableInstance,
        node => node._editableInstance, (node, value) => { node.EnsureMutable(); node._editableInstance = value; }, _ => false, stored: true);
    private static readonly PropertyDescriptor<Node, string[]> PinnedSceneProperties = new(StoredPinnedSceneProperties,
        node => node._pinnedSceneProperties?.Order(StringComparer.Ordinal).ToArray() ?? [],
        (node, value) => node._pinnedSceneProperties = new(value, StringComparer.Ordinal),
        _ => [], (_, value) => value is not null && value.All(name => !string.IsNullOrEmpty(name)), stored: true);

    /// <summary>Gets or sets the description used by scene-authoring tools.</summary>
    /// <value>An empty string initially; arbitrary nonnull text is stored with the node.</value>
    /// <remarks>A different ordinal value commits before EditorDescriptionChanged is delivered. An equal value
    /// emits nothing. The event is independent of the current edited-scene selection. Callback errors propagate
    /// after commitment. Attached writes require the scene owner; capture and physical borrowing reject writes.</remarks>
    /// <exception cref="ArgumentNullException">The assigned value is null.</exception>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or attached access is not authorized.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing or disposed.</exception>
    public string EditorDescription
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return _editorDescription; }
        set
        {
            EnsureMutable(); EnsureEditorAuthoringCommit(); ArgumentNullException.ThrowIfNull(value);
            if (string.Equals(_editorDescription, value, StringComparison.Ordinal)) return;
            _editorDescription = value;
            BeginEventEmission(_publisherEditorDescriptionChanged.Handlers)?.Invoke(this);
        }
    }

    /// <summary>Gets or sets whether authoring tools display this node's children as folded.</summary>
    /// <value>False initially. This transient presentation state is not packed with a scene.</value>
    /// <remarks>Folding changes neither runtime visibility nor processing, hierarchy or physical participation.
    /// Assignments do not emit EditorStateChanged. Attached writes require the scene owner.</remarks>
    /// <exception cref="InvalidOperationException">Mutation is unavailable or attached access is not authorized.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing or disposed.</exception>
    public bool DisplayFolded
    {
        get { ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this); return _displayFolded; }
        set { EnsureMutable(); EnsureEditorAuthoringCommit(); _displayFolded = value; }
    }

    /// <summary>Gets whether this node belongs to the tree's selected edited-scene subtree.</summary>
    /// <value>True for the current EditedSceneRoot and its descendants; false without that live selection.</value>
    /// <remarks>Selection is explicit per tree. Detached nodes have no edited-scene membership. This query
    /// creates no editor, enables no tool scripts and changes no simulation policy.</remarks>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or disposed.</exception>
    public bool IsPartOfEditedScene
    {
        get { ThrowIfDisposed(); var tree = Tree; tree?.EnsureProcessingAccess(this); return tree?.IsEditedSceneMember(this) == true; }
    }

    /// <summary>Occurs after a different EditorDescription value has been committed.</summary>
    /// <remarks>The argument is this node. Signal blocking suppresses ordinary delivery; the description still changes.</remarks>
    public event Action<Node>? EditorDescriptionChanged { add => _publisherEditorDescriptionChanged.Add(value); remove => _publisherEditorDescriptionChanged.Remove(value); }
    private EventPublisher<Action<Node>> _publisherEditorDescriptionChanged;

    /// <summary>Occurs after an authoring-relevant state change in the selected edited-scene subtree.</summary>
    /// <remarks>The argument is this node. Relevant changes include process mode, owner, unique name,
    /// persistent groups, source path, editable-instance policy, property pins and authored connections. Selection changes,
    /// folding and ordinary CLR subscriptions do not emit this event. Delivery is synchronous on the mutating
    /// thread and respects signal blocking. A callback may observe already committed state.</remarks>
    public event Action<Node>? EditorStateChanged { add => _publisherEditorStateChanged.Add(value); remove => _publisherEditorStateChanged.Remove(value); }
    private EventPublisher<Action<Node>> _publisherEditorStateChanged;

    /// <summary>Changes whether a strict descendant's owned children are available to scene-authoring tools.</summary>
    /// <param name="node">A live strict descendant whose instance flag is changed.</param>
    /// <param name="isEditable">The new target flag.</param>
    /// <remarks>The flag belongs to the target and is observable through every current strict ancestor.
    /// Disabling also unfolds this receiver. Every accepted call, including an unchanged flag, requests
    /// EditorStateChanged on the target when it belongs to the selected edited scene. The flag follows
    /// reparenting. Both nodes must permit authoring before either field changes; callback errors propagate
    /// after commitment. Scene packing includes children owned by an enabled descendant instance.</remarks>
    /// <exception cref="ArgumentNullException">The target is null.</exception>
    /// <exception cref="ArgumentException">The target is this node or is not its descendant.</exception>
    /// <exception cref="InvalidOperationException">Either node rejects authoring, including capture or physical borrowing.</exception>
    /// <exception cref="ObjectDisposedException">Either node is disposing or disposed.</exception>
    public void SetEditableInstance(Node node, bool isEditable)
    {
        EnsureMutable(); ArgumentNullException.ThrowIfNull(node); node.EnsureMutable();
        EnsureEditorAuthoringCommit(); node.EnsureEditorAuthoringCommit();
        if (!IsAncestorOf(node)) throw new ArgumentException("An editable instance must be a strict descendant.", nameof(node));
        if (!isEditable) _displayFolded = false;
        node._editableInstance = isEditable;
        node.EmitEditorStateChanged();
    }

    /// <summary>Determines whether a strict descendant has its editable-instance flag enabled.</summary>
    /// <param name="node">The target node, or null.</param>
    /// <returns>False for null; otherwise the descendant's current flag.</returns>
    /// <remarks>The query does not change ownership or require the receiver to be the capture root.</remarks>
    /// <exception cref="ArgumentException">A nonnull target is this node or is not its descendant.</exception>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">Either nonnull node is disposing on another thread or disposed.</exception>
    public bool IsEditableInstance(Node? node)
    {
        ThrowIfDisposed(); Tree?.EnsureProcessingAccess(this);
        if (node is null) return false;
        node.ThrowIfDisposed(); node.Tree?.EnsureProcessingAccess(node);
        if (!IsAncestorOf(node)) throw new ArgumentException("An editable instance must be a strict descendant.", nameof(node));
        return node._editableInstance;
    }

    /// <summary>Sets whether this scene explicitly stores a writable property even when it matches its base value.</summary>
    /// <param name="property">A currently exposed writable stored descriptor.</param>
    /// <param name="pinned">Whether to keep the property in the scene's authored layer.</param>
    /// <remarks>The pin follows the descriptor's storage name. Main-scene editing restores saved pins.
    /// A changed pin emits <see cref="EditorStateChanged"/> in the selected subtree after commitment.</remarks>
    /// <exception cref="ArgumentNullException">The descriptor is null.</exception>
    /// <exception cref="ArgumentException">The descriptor is incompatible, read-only, transient or unavailable.</exception>
    /// <exception cref="InvalidOperationException">The node rejects authoring or the caller lacks owner access.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing or disposed.</exception>
    public void SetPropertyPinned(PropertyDescriptor property, bool pinned)
    {
        EnsureMutable();
        EnsureEditorAuthoringCommit();
        ValidatePinnableProperty(property);
        if (pinned ? (_pinnedSceneProperties ??= new(StringComparer.Ordinal)).Add(property.Name) :
            _pinnedSceneProperties?.Remove(property.Name) == true)
            EmitEditorStateChanged();
    }

    /// <summary>Reports whether an exposed writable stored property is pinned in this scene's authored layer.</summary>
    /// <param name="property">A currently exposed writable stored descriptor.</param>
    /// <returns>Whether the property will be explicitly stored even when it matches the base value.</returns>
    /// <exception cref="ArgumentNullException">The descriptor is null.</exception>
    /// <exception cref="ArgumentException">The descriptor is incompatible, read-only, transient or unavailable.</exception>
    /// <exception cref="InvalidOperationException">Attached access is outside the owner/current processing group.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposing on another thread or disposed.</exception>
    public bool IsPropertyPinned(PropertyDescriptor property)
    {
        ThrowIfDisposed();
        Tree?.EnsureProcessingAccess(this);
        ValidatePinnableProperty(property);
        return HasScenePropertyPin(property.Name);
    }

    private void ValidatePinnableProperty(PropertyDescriptor property)
    {
        ArgumentNullException.ThrowIfNull(property);
        property.EnsureCompatible(this);
        if (!CanPin(property) || !GetPropertyList().Any(candidate => CanPin(candidate) &&
            candidate.Name == property.Name && candidate.ValueType == property.ValueType))
            throw new ArgumentException("A property pin requires an exposed writable stored descriptor.", nameof(property));
    }

    private bool CanPin(PropertyDescriptor property) => !property.IsReadOnly && PackedScene.CapturesProperty(this, property);

    internal bool HasScenePropertyPin(string name) => _pinnedSceneProperties?.Contains(name) == true;

    internal void SanitizeScenePropertyPins()
    {
        if (_pinnedSceneProperties is not { Count: > 0 } pins) return;
        var valid = GetPropertyList().Where(CanPin).Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
        pins.IntersectWith(valid);
    }

    private void EnsureEditorAuthoringCommit()
    {
        EnsureNotSceneCapture();
        Tree?.EnsureOwnerThread();
        EnsurePhysicsAuthoringAccess();
    }

    private void EmitEditorStateChanged()
    {
        if (!IsDisposed && Tree?.IsEditedSceneMember(this) == true)
            BeginEventEmission(_publisherEditorStateChanged.Handlers)?.Invoke(this);
    }

    internal bool OwnsEditableCapture(Node node)
    {
        var owner = node.SceneOwner;
        return ReferenceEquals(owner, this) || owner is not null && owner._editableInstance && IsAncestorOf(owner);
    }

    internal IEnumerable<PropertyDescriptor> CapturedEditorProperties(Node root)
    {
        ValidateEditorPropertyNames(GetPropertyList());
        if (_pinnedSceneProperties is { Count: > 0 }) yield return PinnedSceneProperties;
        if (ReferenceEquals(this, root)) yield break;
        if (_sceneFilePath.Length != 0) yield return SceneOriginProperty;
        if (_editableInstance && (_sceneFilePath.Length != 0 || SpawnSceneIdentity is not null)) yield return EditableInstanceProperty;
    }

    internal static void ValidateEditorPropertyNames(IEnumerable<PropertyDescriptor> descriptors)
    {
        foreach (var descriptor in descriptors)
            if (descriptor.Name is StoredSceneOrigin or StoredEditableInstance or StoredPinnedSceneProperties)
                throw new InvalidOperationException($"Property '{descriptor.Name}' is reserved for scene authoring metadata.");
    }

    internal static PropertyDescriptor? StoredEditorProperty(string name, Type type) =>
        name == StoredSceneOrigin && type == typeof(string) ? SceneOriginProperty :
        name == StoredPinnedSceneProperties && type == typeof(string[]) ? PinnedSceneProperties :
        name == StoredEditableInstance && type == typeof(bool) ? EditableInstanceProperty : null;
}
