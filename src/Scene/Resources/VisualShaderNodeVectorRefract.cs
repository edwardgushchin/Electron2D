namespace Electron2D;

/// <summary>Refracts an incident numeric vector using a normal and scalar index ratio.</summary>
/// <remarks>The normal and incident vectors follow native shader normalization/domain rules. Input two is a scalar ratio and retains its value or absence across width changes.</remarks>
public sealed class VisualShaderNodeVectorRefract : VisualShaderNodeVectorBase
{
    /// <summary>Creates the definition with zero numeric input defaults.</summary>
    public VisualShaderNodeVectorRefract()
    {
        InitializeDefault(0, Vector3.Zero);
        InitializeDefault(1, Vector3.Zero);
        InitializeDefault(2, 0f);
    }
    internal override PortType[] Inputs => [VectorKind, VectorKind, PortType.Scalar];
    internal override PortType[] Outputs => [VectorKind];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"refract({inputs[0]}, {inputs[1]}, {inputs[2]})";
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorRefract();
}
