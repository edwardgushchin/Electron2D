namespace Electron2D;

/// <summary>The numeric or Boolean value selected or compared by a graph operation.</summary>
/// <remarks>This selection domain excludes sampled images. PortType remains the broader logical port table.</remarks>
public enum VisualShaderValueType
{
    /// <summary>A floating scalar.</summary>
    Float = 0,
    /// <summary>A signed integer scalar.</summary>
    Int = 1,
    /// <summary>An unsigned integer scalar.</summary>
    UInt = 2,
    /// <summary>Two floating components.</summary>
    Vector2 = 3,
    /// <summary>Three floating components.</summary>
    Vector3 = 4,
    /// <summary>Four floating components.</summary>
    Vector4 = 5,
    /// <summary>A Boolean scalar.</summary>
    Boolean = 6,
    /// <summary>The nonselectable upper bound of the logical selection table.</summary>
    Max = 8,
}
