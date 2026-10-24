namespace Electron2D;

/// <summary>Rotates three numeric components about a normalized axis and also exposes the numeric rotation matrix.</summary>
/// <remarks>Inputs are vector, angle in radians and axis. Output zero multiplies the input as a row vector;
/// output one is the same matrix extended to four components with a homogeneous identity row/column.
/// A zero axis retains native normalization semantics and has no portable finite-result guarantee.
/// This is numeric shader data; it introduces no spatial scene transform.</remarks>
public sealed class VisualShaderNodeRotationByAxis : VisualShaderNode
{
    /// <summary>Creates zero vector, angle and axis defaults.</summary>
    public VisualShaderNodeRotationByAxis()
    { InitializeDefault(0, Vector3.Zero); InitializeDefault(1, 0f); InitializeDefault(2, Vector3.Zero); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Scalar, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Vector3, PortType.Matrix];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs)
    {
        var matrix = $"e2d_axis_rotation({inputs[2]}, {inputs[1]})";
        return output == 0 ? $"({inputs[0]} * mat3({matrix}))" : matrix;
    }
    internal static string RotationDefinition(string name) => "mat4 " + name + "(vec3 axis, float angle) { vec3 a=normalize(axis); float c=cos(angle), s=sin(angle); return mat4(mat3(\n" +
        "vec3(c+a.x*a.x*(1.0-c), a.x*a.y*(1.0-c)-a.z*s, a.x*a.z*(1.0-c)+a.y*s),\n" +
        "vec3(a.y*a.x*(1.0-c)+a.z*s, c+a.y*a.y*(1.0-c), a.y*a.z*(1.0-c)-a.x*s),\n" +
        "vec3(a.z*a.x*(1.0-c)-a.y*s, a.z*a.y*(1.0-c)+a.x*s, c+a.z*a.z*(1.0-c)))); }\n";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeRotationByAxis();
}
