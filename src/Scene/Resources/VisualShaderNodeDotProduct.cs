namespace Electron2D;

/// <summary>Computes the dot product of two three-component numeric graph inputs.</summary>
/// <remarks>Arithmetic executes in the imported shader; source edits require reimport. Numeric components do not introduce spatial scene types.</remarks>
public sealed class VisualShaderNodeDotProduct : VisualShaderNode
{
    /// <summary>Creates the definition with zero numeric input defaults.</summary>
    public VisualShaderNodeDotProduct()
    {
        InitializeDefault(0, Vector3.Zero);
        InitializeDefault(1, Vector3.Zero);
    }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"dot({inputs[0]}, {inputs[1]})";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeDotProduct();
}
