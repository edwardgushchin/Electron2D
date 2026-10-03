namespace Electron2D;

/// <summary>Draws a borrowed two-dimensional mesh through the retained canvas renderer.</summary>
/// <remarks>Mesh and texture resources remain caller-owned. Live mesh geometry is read during replay;
/// change notifications request redraw for captured scene state. Attached mutation uses the scene owner thread.</remarks>
public class MeshInstance : Entity
{
    private Mesh? _mesh;
    private Texture? _texture;
    /// <summary>Creates a node without mesh or texture.</summary>
    public MeshInstance() { }
    /// <summary>Gets or sets the borrowed mesh.</summary>
    /// <value>Null initially. Equal assignments are silent; replacement detaches the previous change listener.</value>
    /// <exception cref="ObjectDisposedException">The node or assigned mesh is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner or capture-owned.</exception>
    public Mesh? Mesh
    {
        get { ThrowIfDisposed(); return _mesh; }
        set
        {
            EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_mesh, value)) return;
            value?.GetRID(); EnsureMutable();
            if (_mesh is not null) _mesh.Changed -= MeshChanged;
            _mesh = value; if (_mesh is not null) _mesh.Changed += MeshChanged; InvalidateCanvas(); UpdateConfigurationWarnings();
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
    private void MeshChanged(Resource _) { if (!IsDisposed) InvalidateCanvas(); }
    /// <summary>Reports a missing mesh along with inherited scene warnings.</summary>
    /// <returns>Independent warning strings; an empty mesh still counts as an assigned resource.</returns>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return _mesh is null ? [.. warnings, "MeshInstance requires a Mesh to render anything."] : warnings;
    }
    /// <inheritdoc />
    protected override void OnDraw() { base.OnDraw(); if (_mesh is not null) DrawMesh(_mesh, _texture); }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<MeshInstance, Mesh?>(nameof(Mesh), p => p.Mesh, (p, v) => p.Mesh = v, _ => null, stored: true),
        new PropertyDescriptor<MeshInstance, Texture?>(nameof(Texture), p => p.Texture, (p, v) => p.Texture = v, _ => null, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(MeshInstance) ? CreateMeshInstance : base.CreateSceneInstanceFactory();
    private static Node CreateMeshInstance() => new MeshInstance();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) { if (_mesh is not null) _mesh.Changed -= MeshChanged; _mesh = null; _texture = null; TextureChanged = null; }
        base.Dispose(disposing);
    }
}
