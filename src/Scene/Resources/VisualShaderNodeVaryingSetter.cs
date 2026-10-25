namespace Electron2D;

/// <summary>Writes a graph-declared varying from its canvas vertex or fragment producer.</summary>
/// <remarks>This side-effect operation executes without an output connection. The declaration selects its producer;
/// named writes in any other stage reject during import.</remarks>
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
