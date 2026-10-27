namespace Electron2D;

/// <summary>Owns copied vertex, fragment and compute bytecode with stage-specific compiler diagnostics.</summary>
/// <remarks>Native Half and Int16/UInt16 data require independently enabled arithmetic/storage features. Compact push reconstruction also requires the matching arithmetic feature.</remarks>
public sealed class RDShaderSPIRV : Resource
{
    /// <summary>Creates an empty compiled shader resource.</summary>
    public RDShaderSPIRV() { }
    private byte[] _vertex = [];
    private string _vertexError = "";
    /// <summary>Gets or replaces copied vertex-stage bytecode.</summary>
    /// <value>An independent SPIR-V byte array; empty initially.</value>
    public byte[] BytecodeVertex { get { ThrowIfDisposed(); return (byte[])_vertex.Clone(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _vertex = (byte[])value.Clone(); EmitChanged(); } }
    /// <summary>Gets or sets the vertex compiler diagnostic.</summary>
    /// <value>An empty string means no reported compiler error.</value>
    public string CompileErrorVertex { get { ThrowIfDisposed(); return _vertexError; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _vertexError = value; EmitChanged(); } }
    private byte[] _fragment = [];
    private string _fragmentError = "";
    /// <summary>Gets or replaces copied fragment-stage bytecode.</summary>
    /// <value>An independent SPIR-V byte array; empty initially.</value>
    public byte[] BytecodeFragment { get { ThrowIfDisposed(); return (byte[])_fragment.Clone(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _fragment = (byte[])value.Clone(); EmitChanged(); } }
    /// <summary>Gets or sets the fragment compiler diagnostic.</summary>
    /// <value>An empty string means no reported compiler error.</value>
    public string CompileErrorFragment { get { ThrowIfDisposed(); return _fragmentError; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _fragmentError = value; EmitChanged(); } }
    private byte[] _compute = [];
    private string _computeError = "";
    /// <summary>Gets or replaces copied compute-stage bytecode.</summary>
    /// <value>An independent SPIR-V byte array; empty initially.</value>
    public byte[] BytecodeCompute { get { ThrowIfDisposed(); return (byte[])_compute.Clone(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _compute = (byte[])value.Clone(); EmitChanged(); } }
    /// <summary>Gets or sets the compute compiler diagnostic.</summary>
    /// <value>An empty string means no reported compiler error.</value>
    public string CompileErrorCompute { get { ThrowIfDisposed(); return _computeError; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _computeError = value; EmitChanged(); } }
    /// <summary>Returns copied bytecode for an integrated stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <returns>An independent module copy.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is invalid.</exception>
    public byte[] GetStageBytecode(RenderingDevice.ShaderStage stage) => stage switch
    { RenderingDevice.ShaderStage.Vertex => BytecodeVertex, RenderingDevice.ShaderStage.Fragment => BytecodeFragment, RenderingDevice.ShaderStage.Compute => BytecodeCompute, _ => throw new ArgumentOutOfRangeException(nameof(stage)) };
    /// <summary>Replaces copied bytecode for an integrated stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <param name="bytecode">Compiled module bytes.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage is invalid.</exception>
    public void SetStageBytecode(RenderingDevice.ShaderStage stage, byte[] bytecode)
    { switch (stage) { case RenderingDevice.ShaderStage.Vertex: BytecodeVertex = bytecode; break; case RenderingDevice.ShaderStage.Fragment: BytecodeFragment = bytecode; break; case RenderingDevice.ShaderStage.Compute: BytecodeCompute = bytecode; break; default: throw new ArgumentOutOfRangeException(nameof(stage)); } }
    /// <summary>Returns the diagnostic for an integrated stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <returns>The compiler diagnostic, empty when absent.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The stage is invalid.</exception>
    public string GetStageCompileError(RenderingDevice.ShaderStage stage) => stage switch
    { RenderingDevice.ShaderStage.Vertex => CompileErrorVertex, RenderingDevice.ShaderStage.Fragment => CompileErrorFragment, RenderingDevice.ShaderStage.Compute => CompileErrorCompute, _ => throw new ArgumentOutOfRangeException(nameof(stage)) };
    /// <summary>Replaces the diagnostic for an integrated stage.</summary>
    /// <param name="stage">Vertex, fragment or compute.</param>
    /// <param name="compileError">The diagnostic, empty when absent.</param>
    /// <exception cref="ArgumentOutOfRangeException">The stage is invalid.</exception>
    public void SetStageCompileError(RenderingDevice.ShaderStage stage, string compileError)
    { switch (stage) { case RenderingDevice.ShaderStage.Vertex: CompileErrorVertex = compileError; break; case RenderingDevice.ShaderStage.Fragment: CompileErrorFragment = compileError; break; case RenderingDevice.ShaderStage.Compute: CompileErrorCompute = compileError; break; default: throw new ArgumentOutOfRangeException(nameof(stage)); } }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<RDShaderSPIRV, byte[]>(nameof(BytecodeVertex), p => p.BytecodeVertex, (p, v) => p.BytecodeVertex = v, _ => [], stored: true),
        new PropertyDescriptor<RDShaderSPIRV, string>(nameof(CompileErrorVertex), p => p.CompileErrorVertex, (p, v) => p.CompileErrorVertex = v, _ => "", stored: true),
        new PropertyDescriptor<RDShaderSPIRV, byte[]>(nameof(BytecodeFragment), p => p.BytecodeFragment, (p, v) => p.BytecodeFragment = v, _ => [], stored: true),
        new PropertyDescriptor<RDShaderSPIRV, string>(nameof(CompileErrorFragment), p => p.CompileErrorFragment, (p, v) => p.CompileErrorFragment = v, _ => "", stored: true),
        new PropertyDescriptor<RDShaderSPIRV, byte[]>(nameof(BytecodeCompute), p => p.BytecodeCompute, (p, v) => p.BytecodeCompute = v, _ => [], stored: true),
        new PropertyDescriptor<RDShaderSPIRV, string>(nameof(CompileErrorCompute), p => p.CompileErrorCompute, (p, v) => p.CompileErrorCompute = v, _ => "", stored: true),
    ]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RDShaderSPIRV();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (RDShaderSPIRV)target;
        copy._vertex = (byte[])_vertex.Clone(); copy._vertexError = _vertexError;
        copy._fragment = (byte[])_fragment.Clone(); copy._fragmentError = _fragmentError;
        copy._compute = (byte[])_compute.Clone(); copy._computeError = _computeError;
    }
}
