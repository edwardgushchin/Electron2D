namespace Electron2D;

/// <summary>Computes a four-by-four outer product of two (x,y,z,0) numeric vectors.</summary>
/// <remarks>Inputs and results are numeric shader data. Editing requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeOuterProduct : VisualShaderNode
{
    /// <summary>Creates the definition with two explicit zero vector defaults.</summary>
    public VisualShaderNodeOuterProduct() { InitializeDefault(0, Vector3.Zero); InitializeDefault(1, Vector3.Zero); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Matrix];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"outerProduct(vec4({inputs[0]}, 0.0), vec4({inputs[1]}, 0.0))";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeOuterProduct();
}
