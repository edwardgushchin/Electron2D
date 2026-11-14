namespace Electron2D;

/// <summary>Defines the ordering of a surface's indexed or sequential vertices.</summary>
public enum MeshPrimitiveType
{
    /// <summary>Independent points, one framebuffer pixel unless an authored vertex output changes their diameter.</summary>
    Points = 0,
    /// <summary>Independent pairs of one-framebuffer-pixel line vertices.</summary>
    Lines = 1,
    /// <summary>Connected one-framebuffer-pixel line segments.</summary>
    LineStrip = 2,
    /// <summary>Independent triangles.</summary>
    Triangles = 3,
    /// <summary>Connected triangles with alternating winding.</summary>
    TriangleStrip = 4,
    /// <summary>Exclusive upper bound; cannot be selected for a surface.</summary>
    Max = 5
}
