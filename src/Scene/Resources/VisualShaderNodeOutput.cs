namespace Electron2D;

/// <summary>The reserved canvas or writable-texture output operation owned by a visual shader stage.</summary>
/// <remarks>Retrieve it as node zero. Its input indices follow the containing stage. Canvas vertex and fragment roots retain their
/// renderer roles. Light accepts RGB, alpha and RGB shadow modulation at indices zero through two,
/// replacing the current light's automatic contribution. TextureBlit accepts four numeric RGBA outputs.
/// Fragment ports two through six author the normal, normal map, map depth, light position and shadow position.
/// The normal map overrides the direct normal and blends from the flat normal by its unclamped depth.
/// Particle roots use VisualShaderNodeParticleOutput. Unsupported point outputs reject during import.</remarks>
public class VisualShaderNodeOutput : VisualShaderNode
{
    internal VisualShader.Type Stage;
    /// <summary>Creates an unbound fragment output definition.</summary>
    public VisualShaderNodeOutput() { Stage = VisualShader.Type.Fragment; }
    internal override PortType[] Inputs => Stage == VisualShader.Type.TextureBlit ? [PortType.Vector4, PortType.Vector4, PortType.Vector4, PortType.Vector4] : Stage == VisualShader.Type.Light ? [PortType.Vector3, PortType.Scalar, PortType.Vector3] : Stage == VisualShader.Type.Vertex
        ? [PortType.Vector2, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Scalar]
        : [PortType.Vector3, PortType.Scalar, PortType.Vector3, PortType.Vector3, PortType.Scalar, PortType.Vector3, PortType.Vector2];
    internal override PortType[] Outputs => [];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Output nodes have no output ports.");
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeOutput, VisualShader.Type>("Stage", n => n.Stage,
            (n, v) => { if (n is VisualShaderNodeParticleOutput ? v is < VisualShader.Type.Start or > VisualShader.Type.ProcessCustom : v is not (VisualShader.Type.Vertex or VisualShader.Type.Fragment or VisualShader.Type.Light or VisualShader.Type.TextureBlit)) throw new InvalidDataException("Invalid output stage."); n.Stage = v; }, stored: true) }.Concat(base.GetPropertyDescriptors());
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeOutput { Stage = Stage };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeOutput)target).Stage = Stage;
}
