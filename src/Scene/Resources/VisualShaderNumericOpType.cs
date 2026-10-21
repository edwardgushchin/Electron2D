namespace Electron2D;

/// <summary>The floating component layout shared by range, threshold and interpolation graph operations.</summary>
/// <remarks>Scalar-control layouts keep the operation's control inputs scalar while its data inputs and output use the selected vector width.</remarks>
public enum VisualShaderNumericOpType
{
    /// <summary>Scalar data and scalar controls.</summary>
    Scalar = 0,
    /// <summary>Two-component data and controls.</summary>
    Vector2 = 1,
    /// <summary>Two-component data with scalar controls.</summary>
    Vector2Scalar = 2,
    /// <summary>Three-component data and controls.</summary>
    Vector3 = 3,
    /// <summary>Three-component data with scalar controls.</summary>
    Vector3Scalar = 4,
    /// <summary>Four-component data and controls.</summary>
    Vector4 = 5,
    /// <summary>Four-component data with scalar controls.</summary>
    Vector4Scalar = 6,
    /// <summary>The nonselectable layout-count sentinel.</summary>
    Max = 7,
}
