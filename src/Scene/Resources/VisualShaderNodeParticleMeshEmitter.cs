namespace Electron2D;

/// <summary>Samples aligned vertex channels from selected surfaces of a borrowed planar mesh.</summary>
/// <remarks>Import bakes only connected channels into owned numeric images. Missing channels and empty meshes
/// produce zero. Surface order and vertex order are retained; index buffers do not weight vertex selection.</remarks>
public sealed class VisualShaderNodeParticleMeshEmitter : VisualShaderNodeParticleEmitter
{
    private Mesh? _mesh;
    private int _surfaceIndex;
    private bool _useAllSurfaces = true;
    private readonly Action<Resource> _meshChanged;
    /// <summary>Creates an empty emitter selecting all surfaces.</summary>
    public VisualShaderNodeParticleMeshEmitter() { _meshChanged = _ => EmitChanged(); }
    /// <summary>Gets or sets the borrowed mesh defining the emission channels.</summary>
    /// <value>Null initially; the caller retains ownership.</value>
    /// <exception cref="ObjectDisposedException">The node or supplied mesh is disposed.</exception>
    public Mesh? Mesh
    {
        get { ThrowIfDisposed(); return _mesh; }
        set
        {
            ThrowIfDisposed(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            if (ReferenceEquals(_mesh, value)) return;
            if (_mesh is not null) _mesh.Changed -= _meshChanged;
            _mesh = value; if (_mesh is not null) _mesh.Changed += _meshChanged;
            EmitChanged();
        }
    }
    /// <summary>Gets or sets the selected surface when UseAllSurfaces is false.</summary>
    /// <value>Zero initially; an absent surface produces an empty emitter.</value>
    /// <exception cref="ArgumentOutOfRangeException">The index is negative.</exception>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public int SurfaceIndex { get { ThrowIfDisposed(); return _surfaceIndex; } set { ThrowIfDisposed(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_surfaceIndex == value) return; _surfaceIndex = value; EmitChanged(); } }
    /// <summary>Gets or sets whether every surface contributes vertices in source order.</summary>
    /// <value>True initially; SurfaceIndex is retained while inactive.</value>
    /// <exception cref="ObjectDisposedException">The node is disposed.</exception>
    public bool UseAllSurfaces { get { ThrowIfDisposed(); return _useAllSurfaces; } set { ThrowIfDisposed(); if (_useAllSurfaces == value) return; _useAllSurfaces = value; EmitChanged(); } }
    internal override PortType[] Inputs => [];
    internal override PortType[] Outputs => [PortType.Vector2, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Vector2, PortType.Vector2];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Mesh emission requires its shared sampled vertex index.");
    internal override void CopyDefinitionTo(VisualShaderNode target)
    {
        var node = (VisualShaderNodeParticleMeshEmitter)target;
        node.Mesh = _mesh; node._surfaceIndex = _surfaceIndex; node._useAllSurfaces = _useAllSurfaces;
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); ((VisualShaderNodeParticleMeshEmitter)target).Mesh = (Mesh?)duplicateSubresource(_mesh); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeParticleMeshEmitter();
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    { if (disposing) { if (_mesh is not null) _mesh.Changed -= _meshChanged; _mesh = null; } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<VisualShaderNodeParticleMeshEmitter, Mesh?>(nameof(Mesh), n => n.Mesh, (n, v) => n.Mesh = v, stored: true),
        new PropertyDescriptor<VisualShaderNodeParticleMeshEmitter, int>(nameof(SurfaceIndex), n => n.SurfaceIndex, (n, v) => n.SurfaceIndex = v, stored: true),
        new PropertyDescriptor<VisualShaderNodeParticleMeshEmitter, bool>(nameof(UseAllSurfaces), n => n.UseAllSurfaces, (n, v) => n.UseAllSurfaces = v, stored: true),
    ]);
}
