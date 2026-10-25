namespace Electron2D;

/// <summary>Writes a graph-declared varying from the canvas vertex stage.</summary>
/// <remarks>This side-effect operation executes without an output connection. Named fragment writes require a separate light-graph producer.</remarks>
public sealed class VisualShaderNodeVaryingSetter : VisualShaderNodeVarying
{
    /// <summary>Creates an unselected floating-point setter with zero input.</summary>
    public VisualShaderNodeVaryingSetter() => InitializeDefault(0, 0f);
    internal override PortType[] Inputs => [ValueType];
    internal override PortType[] Outputs => [];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("A varying setter has no output value.");
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVaryingSetter();
}
