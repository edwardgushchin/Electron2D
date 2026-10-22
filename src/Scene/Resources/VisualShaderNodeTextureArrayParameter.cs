namespace Electron2D;

/// <summary>Declares a named independent image array sampler in an imported visual graph.</summary>
/// <remarks>The image is supplied by ShaderMaterial, the shader default or the global registry.
/// Unassigned images use the retained render-only fallback; graph edits require reimport.</remarks>
public sealed class VisualShaderNodeTextureArrayParameter : VisualShaderNodeTextureParameter
{
    /// <summary>Creates a data sampler with shader-default filtering and wrapping.</summary>
    public VisualShaderNodeTextureArrayParameter() { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeTextureArrayParameter();
}
