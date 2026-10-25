namespace Electron2D;

/// <summary>The numeric value transported between canvas shader stages.</summary>
/// <remarks>Integer and Boolean values use flat interpolation. Floating values and numeric matrices interpolate per component.</remarks>
public enum VisualShaderVaryingType
{
    /// <summary>A single-precision scalar.</summary>
    Float = 0,
    /// <summary>A signed 32-bit scalar.</summary>
    Int = 1,
    /// <summary>An unsigned 32-bit scalar.</summary>
    UInt = 2,
    /// <summary>Two floating components.</summary>
    Vector2 = 3,
    /// <summary>Three floating components.</summary>
    Vector3 = 4,
    /// <summary>Four floating components.</summary>
    Vector4 = 5,
    /// <summary>A Boolean value.</summary>
    Boolean = 6,
    /// <summary>A four-by-four numeric matrix.</summary>
    Transform = 7,
    /// <summary>The nonselectable upper bound.</summary>
    Max = 8,
}
