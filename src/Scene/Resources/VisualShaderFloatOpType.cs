namespace Electron2D;

/// <summary>The floating component layout shared by fused arithmetic and derivative graph operations.</summary>
public enum VisualShaderFloatOpType
{
    /// <summary>A floating scalar.</summary>
    Scalar = 0,
    /// <summary>Two floating components.</summary>
    Vector2 = 1,
    /// <summary>Three floating components.</summary>
    Vector3 = 2,
    /// <summary>Four floating components.</summary>
    Vector4 = 3,
    /// <summary>The nonselectable layout-count sentinel.</summary>
    Max = 4,
}
