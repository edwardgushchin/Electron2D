namespace Electron2D;

/// <summary>Composes three numeric columns and one final column into an affine four-by-four matrix.</summary>
/// <remarks>Inputs and results are numeric shader data. Editing requires reimport before drawing changes.</remarks>
public sealed class VisualShaderNodeMatrixCompose : VisualShaderNode
{
    /// <summary>Creates the definition with four explicit zero vector defaults.</summary>
    public VisualShaderNodeMatrixCompose() { for (var port = 0; port < 4; port++) InitializeDefault(port, Vector3.Zero); }
    internal override PortType[] Inputs => [PortType.Vector3, PortType.Vector3, PortType.Vector3, PortType.Vector3];
    internal override PortType[] Outputs => [PortType.Matrix];
    internal override string Expression(int output, IReadOnlyList<string> inputs) => $"mat4(vec4({inputs[0]}, 0.0), vec4({inputs[1]}, 0.0), vec4({inputs[2]}, 0.0), vec4({inputs[3]}, 1.0))";
    internal override void CopyDefinitionTo(VisualShaderNode target) { }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeMatrixCompose();
}
