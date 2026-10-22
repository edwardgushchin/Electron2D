namespace Electron2D;

/// <summary>Declares a named ordinary image sampler in an imported visual graph.</summary>
/// <remarks>The image is supplied by ShaderMaterial, the shader default or the global registry.
/// Unassigned images use the retained render-only fallback; graph edits require reimport.</remarks>
public sealed class VisualShaderNodeImageParameter : VisualShaderNodeTextureParameter
{
    /// <summary>Creates a data sampler with shader-default filtering and wrapping.</summary>
    public VisualShaderNodeImageParameter() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeImageParameter();
}
