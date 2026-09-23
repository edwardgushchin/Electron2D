namespace Electron2D;

/// <summary>Copies selected local or global transform components to another spatial node.</summary>
/// <remarks>The target is resolved when this node enters a tree, when <see cref="RemotePath"/> changes, or when
/// <see cref="ForceUpdateCache"/> is called. The target is borrowed and is never retained strongly.</remarks>
public class RemoteTransform : Entity
{
    private static readonly PropertyDescriptor[] RemoteProperties =
    [
        new PropertyDescriptor<RemoteTransform, string>(nameof(RemotePath), node => node.RemotePath, (node, value) => node.RemotePath = value, _ => string.Empty, stored: true),
        new PropertyDescriptor<RemoteTransform, bool>(nameof(UseGlobalCoordinates), node => node.UseGlobalCoordinates, (node, value) => node.UseGlobalCoordinates = value, _ => true, stored: true),
        new PropertyDescriptor<RemoteTransform, bool>(nameof(UpdatePosition), node => node.UpdatePosition, (node, value) => node.UpdatePosition = value, _ => true, stored: true),
        new PropertyDescriptor<RemoteTransform, bool>(nameof(UpdateRotation), node => node.UpdateRotation, (node, value) => node.UpdateRotation = value, _ => true, stored: true),
        new PropertyDescriptor<RemoteTransform, bool>(nameof(UpdateScale), node => node.UpdateScale, (node, value) => node.UpdateScale = value, _ => true, stored: true),
    ];

    private string _remotePath = string.Empty;
    private WeakReference<Entity>? _target;
    private bool _useGlobalCoordinates = true;
    private bool _updatePosition = true;
    private bool _updateRotation = true;
    private bool _updateScale = true;
    private bool _updating;
    private bool _updatePending;

    /// <summary>Creates a node that initially copies all global transform components.</summary>
    public RemoteTransform() => NotifyTransformChanges = true;

    /// <summary>Gets or sets the path to a target spatial node.</summary>
    /// <value>An empty path by default; empty or unresolved paths have no target.</value>
    /// <remarks>Changing the path while attached refreshes the target and immediately copies the selected components.
    /// A path to this node or a relative ancestor or descendant is ignored to prevent hierarchy feedback.</remarks>
    /// <exception cref="ArgumentNullException">The assigned path is null.</exception>
    /// <exception cref="InvalidOperationException">Scene mutation is unavailable or the target transform cannot be assigned.</exception>
    public string RemotePath
    {
        get { ThrowIfDisposed(); return _remotePath; }
        set
        {
            EnsureMutable();
            ArgumentNullException.ThrowIfNull(value);
            if (_remotePath == value) return;
            _remotePath = value;
            if (IsInsideTree) { UpdateCache(); UpdateRemote(); }
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets whether source and destination transforms use global coordinates.</summary>
    /// <value>True by default; false uses each node's local coordinates.</value>
    /// <remarks>Switching mode changes the active transform notification and immediately updates the target.</remarks>
    public bool UseGlobalCoordinates
    {
        get { ThrowIfDisposed(); return _useGlobalCoordinates; }
        set
        {
            EnsureMutable();
            if (_useGlobalCoordinates == value) return;
            _useGlobalCoordinates = value;
            NotifyTransformChanges = value;
            NotifyLocalTransformChanges = !value;
            UpdateRemote();
        }
    }

    /// <summary>Gets or sets whether the target receives the source position.</summary>
    /// <value>True by default.</value>
    public bool UpdatePosition
    {
        get { ThrowIfDisposed(); return _updatePosition; }
        set { EnsureMutable(); if (_updatePosition == value) return; _updatePosition = value; UpdateRemote(); }
    }

    /// <summary>Gets or sets whether the target receives the source rotation and skew basis.</summary>
    /// <value>True by default.</value>
    public bool UpdateRotation
    {
        get { ThrowIfDisposed(); return _updateRotation; }
        set { EnsureMutable(); if (_updateRotation == value) return; _updateRotation = value; UpdateRemote(); }
    }

    /// <summary>Gets or sets whether the target receives the source scale.</summary>
    /// <value>True by default.</value>
    public bool UpdateScale
    {
        get { ThrowIfDisposed(); return _updateScale; }
        set { EnsureMutable(); if (_updateScale == value) return; _updateScale = value; UpdateRemote(); }
    }

    /// <summary>Resolves <see cref="RemotePath"/> again without changing the target transform.</summary>
    /// <remarks>Use this after the target is added, moved, or replaced in the same tree.</remarks>
    public void ForceUpdateCache() { EnsureMutable(); UpdateCache(); }

    /// <inheritdoc />
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        if (string.IsNullOrWhiteSpace(_remotePath) || GetNodeOrNull(_remotePath) is not Entity target || !IsValidTarget(target))
            return [.. warnings, "RemotePath must point to an unrelated spatial node in this scene tree."];
        return warnings;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(RemoteProperties);

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(RemoteTransform)
        ? CreateDefaultNode : base.CreateSceneInstanceFactory();

    private static Node CreateDefaultNode() => new RemoteTransform();

    internal override void OnTreeMembershipChanged(bool entering)
    {
        if (!entering) _target = null;
        base.OnTreeMembershipChanged(entering);
        if (entering && !IsDisposed) UpdateCache();
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        if ((_useGlobalCoordinates && what == NotificationTransformChanged) ||
            (!_useGlobalCoordinates && what == NotificationLocalTransformChanged))
            UpdateRemote();
        base.OnNotification(what);
    }

    private void UpdateCache()
    {
        _target = null;
        if (!IsInsideTree || string.IsNullOrWhiteSpace(_remotePath)) return;
        if (GetNodeOrNull(_remotePath) is Entity target && IsValidTarget(target))
            _target = new WeakReference<Entity>(target);
    }

    private bool IsValidTarget(Entity target)
    {
        if (target.IsDisposed || ReferenceEquals(target, this)) return false;
        for (Node? node = Parent; node is not null; node = node.Parent)
            if (ReferenceEquals(node, target)) return false;
        for (Node? node = target.Parent; node is not null; node = node.Parent)
            if (ReferenceEquals(node, this)) return false;
        return true;
    }

    private void UpdateRemote()
    {
        if (_updating) { _updatePending = true; return; }
        _updating = true;
        try
        {
            for (var pass = 0; pass < 64; pass++)
            {
                _updatePending = false;
                UpdateRemoteOnce();
                if (!_updatePending) return;
            }
            throw new InvalidOperationException("Remote transform callbacks did not settle after 64 updates.");
        }
        finally { _updating = false; _updatePending = false; }
    }

    private void UpdateRemoteOnce()
    {
        if (!IsInsideTree || (!_updatePosition && !_updateRotation && !_updateScale) ||
            _target is null || !_target.TryGetTarget(out var target) || target.IsDisposed ||
            !ReferenceEquals(target.Tree, Tree) || !IsValidTarget(target) || HasRemoteCycle(target)) return;

        var source = _useGlobalCoordinates ? GlobalTransform : Transform;
        var destination = _useGlobalCoordinates ? target.GlobalTransform : target.Transform;
        var result = _updateRotation ? source : destination;
        if (_updateRotation != _updatePosition)
            result.Origin = _updatePosition ? source.Origin : destination.Origin;
        if (_updateRotation != _updateScale)
        {
            var scale = _updateScale ? source.Scale : destination.Scale;
            result = new Transform(result.Rotation, scale, result.Skew, result.Origin);
        }
        if (_useGlobalCoordinates) target.GlobalTransform = result;
        else target.Transform = result;
    }

    private bool HasRemoteCycle(Entity target)
    {
        if (target is not RemoteTransform) return false;
        var visited = new HashSet<RemoteTransform>();
        while (target is RemoteTransform remote && remote._target?.TryGetTarget(out var next) == true)
        {
            if (!visited.Add(remote) || ReferenceEquals(next, this)) return true;
            target = next;
        }
        return false;
    }
}
