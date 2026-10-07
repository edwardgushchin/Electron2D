namespace Electron2D;

public sealed partial class Shader
{
    private RID _renderingRID;
    internal void BindRenderingRID(RID rid) { lock (_codeGate) _renderingRID = rid; }
    private void ReleaseRenderingRID() { if (_renderingRID.IsValid()) RenderingProgramRegistry<Shader>.Remove(_renderingRID); _renderingRID = default; }
    /// <summary>Returns the stable borrowed logical program identity, independent of native startup.</summary>
    /// <returns>A resource-owned RID; server mutation and FreeRID reject borrowed ownership.</returns>
    /// <exception cref="ObjectDisposedException">The shader is disposed.</exception>
    public override RID GetRID() { lock (_codeGate) { ThrowIfDisposed(); return _renderingRID.IsValid() ? _renderingRID : _renderingRID = RenderingProgramRegistry<Shader>.Register(this); } }
}

public abstract partial class Material
{
    private readonly object _renderingRIDGate = new();
    private RID _renderingRID;
    internal void BindRenderingRID(RID rid) { lock (_renderingRIDGate) _renderingRID = rid; }
    /// <summary>Returns the stable borrowed canvas-material identity, independent of native startup.</summary>
    /// <returns>A resource-owned RID for programmable or built-in material consumers.</returns>
    /// <exception cref="ObjectDisposedException">The material is disposed.</exception>
    public override RID GetRID() { lock (_renderingRIDGate) { ThrowIfDisposed(); return _renderingRID.IsValid() ? _renderingRID : _renderingRID = RenderingProgramRegistry<Material>.Register(this); } }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) lock (_renderingRIDGate) { if (_renderingRID.IsValid()) RenderingProgramRegistry<Material>.Remove(_renderingRID); _renderingRID = default; } base.Dispose(disposing); }
}
