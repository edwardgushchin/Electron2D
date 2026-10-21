namespace Electron2D;

/// <summary>Assembles two, three or four scalar graph inputs into a numeric vector.</summary>
/// <remarks>Default slots zero through three remain available while inactive, preserving authored
/// components across narrowing, duplication and archives. A width change recreates missing active
/// defaults as zero. Only the selected components accept graph connections.</remarks>
public sealed class VisualShaderNodeVectorCompose : VisualShaderNodeVectorBase
{
    /// <summary>Creates a three-component definition with three explicit zero scalar defaults.</summary>
    public VisualShaderNodeVectorCompose()
    { InitializeDefault(0, 0f); InitializeDefault(1, 0f); InitializeDefault(2, 0f); }
    internal override PortType[] Inputs => Enumerable.Repeat(PortType.Scalar, (int)OpType + 2).ToArray();
    internal override PortType[] DefaultInputs => [PortType.Scalar, PortType.Scalar, PortType.Scalar, PortType.Scalar];
    internal override PortType[] Outputs => [VectorKind];
    internal override string Expression(int output, IReadOnlyList<string> inputs) =>
        $"{VisualShaderValues.Name(VectorKind)}({string.Join(", ", inputs)})";
    internal override void ChangeDefaultWidth(PortType type)
    {
        var count = type switch { PortType.Vector2 => 2, PortType.Vector3 => 3, PortType.Vector4 => 4, _ => throw new ArgumentOutOfRangeException(nameof(type)) };
        for (var port = 0; port < count; port++) InitializeDefault(port, GetInputPortDefaultValue<float>(port) ?? 0f);
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVectorCompose();
}
