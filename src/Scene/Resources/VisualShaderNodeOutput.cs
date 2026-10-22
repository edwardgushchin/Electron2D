namespace Electron2D;

/// <summary>The reserved canvas output operation owned by a visual shader stage.</summary>
/// <remarks>Retrieve it as node zero. Its input indices follow the canvas vertex or fragment port table.
/// The initial graph importer executes vertex position/UV/color/alpha and fragment color/alpha.</remarks>
public sealed class VisualShaderNodeOutput : VisualShaderNode
{
    internal VisualShader.Type Stage;
    /// <summary>Creates an unbound fragment output definition.</summary>
    public VisualShaderNodeOutput() { Stage = VisualShader.Type.Fragment; }
    internal override PortType[] Inputs => Stage == VisualShader.Type.Vertex
        ? [PortType.Vector2, PortType.Vector2, PortType.Vector3, PortType.Scalar, PortType.Scalar]
        : [PortType.Vector3, PortType.Scalar, PortType.Vector3, PortType.Vector3, PortType.Scalar, PortType.Vector3, PortType.Vector2];
    internal override PortType[] Outputs => [];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("Output nodes have no output ports.");
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        new PropertyDescriptor[] { new PropertyDescriptor<VisualShaderNodeOutput, VisualShader.Type>("Stage", n => n.Stage,
            (n, v) => { if (v is not (VisualShader.Type.Vertex or VisualShader.Type.Fragment)) throw new InvalidDataException("Invalid output stage."); n.Stage = v; }, stored: true) }.Concat(base.GetPropertyDescriptors());
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeOutput { Stage = Stage };
    internal override void CopyDefinitionTo(VisualShaderNode target) => ((VisualShaderNodeOutput)target).Stage = Stage;
}
