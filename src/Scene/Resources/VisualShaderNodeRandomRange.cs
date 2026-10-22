namespace Electron2D;

/// <summary>Computes a repeatable scalar variation from a three-component numeric seed and two bounds.</summary>
/// <remarks>The shader hash produces a factor in [-1, 1), and the bounds are mixed without clamping.
/// Results may fall below the first bound; a zero seed yields twice the first bound minus the second.
/// Equal and reversed bounds retain this arithmetic. No mutable random state is consumed.
/// Floating-point results depend on shader precision. Source edits require reimport.</remarks>
public sealed class VisualShaderNodeRandomRange : VisualShaderNode
{
    /// <summary>Creates a definition with seed (1, 1, 1), minimum zero and maximum one.</summary>
    public VisualShaderNodeRandomRange()
    { InitializeDefault(0, Vector3.One); InitializeDefault(1, 0f); InitializeDefault(2, 1f); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Scalar, PortType.Scalar];
    internal override PortType[] Outputs => [PortType.Scalar];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) =>
        $"mix({inputs[1]}, {inputs[2]}, e2d_random_range({inputs[0]}).x)";
    internal static string HashDefinition(string name) =>
        $"vec3 {name}(vec3 p) {{ p *= mat3(vec3(127.1, 311.7, -53.7), vec3(269.5, 183.3, 77.1), vec3(-301.7, 27.3, 215.3)); return 2.0 * fract(fract(p) * 4375.55) - 1.0; }}\n";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeRandomRange();
}
