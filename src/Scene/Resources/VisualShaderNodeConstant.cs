namespace Electron2D;

/// <summary>A visual shader node producing immutable values for an imported program.</summary>
/// <remarks>Changing a constant edits graph source. Reimport the graph to replace its compiled shader.</remarks>
public abstract class VisualShaderNodeConstant : VisualShaderNode
{
    /// <summary>Initializes a constant graph operation.</summary>
    protected VisualShaderNodeConstant() { }
    internal override PortType[] Inputs => [];
}
