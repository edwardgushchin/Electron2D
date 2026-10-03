namespace Electron2D;

/// <summary>Draws borrowed two-dimensional mesh instances through the retained canvas renderer.</summary>
/// <remarks>Mesh and texture resources remain caller-owned. Live mesh geometry is read during replay;
/// change notifications request redraw for captured scene state. Attached mutation uses the scene owner thread.</remarks>
public class MultiMeshInstance : Entity
{
    private MultiMesh? _multiMesh;
    private Texture? _texture;
    /// <summary>Creates a node without mesh or texture.</summary>
    public MultiMeshInstance() { }
    /// <summary>Gets or sets the borrowed instance resource.</summary>
    /// <value>Null initially. Equal assignments are silent; replacement detaches the previous change listener.</value>
    /// <exception cref="ObjectDisposedException">The node or assigned instance resource is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public MultiMesh? MultiMesh
    {
        get { ThrowIfDisposed(); return _multiMesh; }
        set
        {
            EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_multiMesh, value)) return;
            value?.GetRID(); EnsureMutable();
            if (_multiMesh is not null) _multiMesh.Changed -= MultiMeshChanged;
            _multiMesh = value; if (_multiMesh is not null) _multiMesh.Changed += MultiMeshChanged; RefreshInterpolation(); InvalidateCanvas(); UpdateConfigurationWarnings();
        }
    }
    /// <summary>Gets or sets the optional borrowed surface texture.</summary>
    /// <value>Null initially. Equal assignments are silent; replacement commits before TextureChanged.</value>
    /// <exception cref="ObjectDisposedException">The node or assigned texture is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public Texture? Texture
    {
        get { ThrowIfDisposed(); return _texture; }
        set { EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_texture, value)) return; _texture = value; InvalidateCanvas(); TextureChanged?.Invoke(); }
    }
    /// <summary>Occurs after replacing the borrowed texture and scheduling redraw.</summary>
    public event Action? TextureChanged;
    private void MultiMeshChanged(Resource _) { if (!IsDisposed) InvalidateCanvas(); }
    internal void RefreshInterpolation() { if (IsInsideTree && _multiMesh is { IsDisposed: false }) _multiMesh.SetInterpolationEnabled(IsPhysicsInterpolatedAndEnabled()); }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what is NotificationEnterTree or NotificationResetPhysicsInterpolation) RefreshInterpolation();
    }
    /// <summary>Reports a missing mesh along with inherited scene warnings.</summary>
    /// <returns>Independent warning strings; an empty mesh still counts as an assigned resource.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return _multiMesh is null ? [.. warnings, "MultiMeshInstance requires a MultiMesh to render anything."] : warnings;
    }
    /// <inheritdoc />
    protected override void OnDraw() { base.OnDraw(); if (_multiMesh is not null) DrawMultiMesh(_multiMesh, _texture); }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<MultiMeshInstance, MultiMesh?>(nameof(MultiMesh), p => p.MultiMesh, (p, v) => p.MultiMesh = v, _ => null, stored: true),
        new PropertyDescriptor<MultiMeshInstance, Texture?>(nameof(Texture), p => p.Texture, (p, v) => p.Texture = v, _ => null, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MultiMeshInstance) ? CreateMultiMeshInstance : base.CreateSceneInstanceFactory();
    private static Node CreateMultiMeshInstance() => new MultiMeshInstance();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_multiMesh is not null) _multiMesh.Changed -= MultiMeshChanged; _multiMesh = null; _texture = null; TextureChanged = null; }
        base.Dispose(disposing);
    }
}
