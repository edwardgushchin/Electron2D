namespace Electron2D;

/// <summary>Owns compiled compute-stage bytecode and its compiler diagnostic.</summary>
public sealed class RDShaderSPIRV : Resource
{
    private byte[] _compute = [];
    private string _error = "";
    /// <summary>Creates an empty compiled shader resource.</summary>
    public RDShaderSPIRV() { }
    /// <summary>Gets or replaces copied compute-stage bytecode.</summary>
    /// <value>An independent SPIR-V byte array; empty initially.</value>
    public byte[] BytecodeCompute { get { ThrowIfDisposed(); return (byte[])_compute.Clone(); } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _compute = (byte[])value.Clone(); EmitChanged(); } }
    /// <summary>Gets or sets the compute compiler diagnostic.</summary>
    /// <value>An empty string means no reported compiler error.</value>
    public string CompileErrorCompute { get { ThrowIfDisposed(); return _error; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _error = value; EmitChanged(); } }
    /// <summary>Returns copied bytecode for a supported stage.</summary>
    /// <param name="stage">Compute; other pipeline domains are not integrated.</param>
    /// <returns>A copy of the compute module.</returns>
    public byte[] GetStageBytecode(RenderingDevice.ShaderStage stage) { Check(stage); return BytecodeCompute; }
    /// <summary>Replaces copied bytecode for a supported stage.</summary>
    /// <param name="stage">Compute.</param>
    /// <param name="bytecode">Compiled module bytes.</param>
    public void SetStageBytecode(RenderingDevice.ShaderStage stage, byte[] bytecode) { Check(stage); BytecodeCompute = bytecode; }
    /// <summary>Returns the compiler diagnostic for a supported stage.</summary>
    /// <param name="stage">Compute.</param>
    /// <returns>The diagnostic, or an empty string.</returns>
    public string GetStageCompileError(RenderingDevice.ShaderStage stage) { Check(stage); return CompileErrorCompute; }
    /// <summary>Replaces the compiler diagnostic for a supported stage.</summary>
    /// <param name="stage">Compute.</param>
    /// <param name="compileError">The diagnostic, or an empty string.</param>
    public void SetStageCompileError(RenderingDevice.ShaderStage stage, string compileError) { Check(stage); CompileErrorCompute = compileError; }
    private static void Check(RenderingDevice.ShaderStage stage) { if (stage != RenderingDevice.ShaderStage.Compute) throw new NotSupportedException("Only compute stages are integrated into local devices."); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
        [new PropertyDescriptor<RDShaderSPIRV, byte[]>(nameof(BytecodeCompute), p => p.BytecodeCompute, (p, v) => p.BytecodeCompute = v, _ => [], stored: true),
         new PropertyDescriptor<RDShaderSPIRV, string>(nameof(CompileErrorCompute), p => p.CompileErrorCompute, (p, v) => p.CompileErrorCompute = v, _ => "", stored: true)]);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new RDShaderSPIRV();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    { var copy = (RDShaderSPIRV)target; copy._compute = (byte[])_compute.Clone(); copy._error = _error; }
}
