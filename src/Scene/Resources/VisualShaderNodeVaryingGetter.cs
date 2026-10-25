namespace Electron2D;

/// <summary>Reads a graph-declared canvas varying.</summary>
/// <remarks>Vertex-to-fragment/light reads receive the transported vertex value. Fragment-to-light reads receive
/// private values from the same fragment. Producer-stage reads observe their evaluation point; a name alone creates
/// no dependency. Unwritten values start at typed zero or matrix identity.</remarks>
public sealed class VisualShaderNodeVaryingGetter : VisualShaderNodeVarying
{
    /// <summary>Creates an unselected floating-point getter.</summary>
    public VisualShaderNodeVaryingGetter() { }
    internal override PortType[] Inputs => [];
    internal override PortType[] Outputs => [ValueType];
    internal override string GenerateExpression(int output, IReadOnlyList<string> inputs) => throw new InvalidOperationException("A varying read requires its graph declaration.");
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new VisualShaderNodeVaryingGetter();
}
