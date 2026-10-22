namespace Electron2D;

/// <summary>Contributes shared GLSL declarations to both imported canvas stages.</summary>
/// <remarks>Every global definition in either canvas graph participates once per graph entry, ordered by
/// stage and node ID, even without output connections. Use this source for functions, constants or
/// declarations that satisfy the existing shader ABI. Compiler diagnostics preserve native GLSL stage
/// restrictions. ELECTRON2D_SHADER_STAGE is zero for Vertex and one for Fragment during preprocessing.
/// Local statements and graph port values belong to VisualShaderNodeExpression instead.</remarks>
public sealed class VisualShaderNodeGlobalExpression : VisualShaderNodeExpression
{
    /// <summary>Creates an empty global source definition.</summary>
    public VisualShaderNodeGlobalExpression() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeGlobalExpression();
}
