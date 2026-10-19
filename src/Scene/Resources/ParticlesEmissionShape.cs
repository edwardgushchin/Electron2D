namespace Electron2D;

/// <summary>Selects the emitter's two-dimensional distribution.</summary>
public enum ParticlesEmissionShape
{
    /// <summary>Emit at the origin.</summary>
    Point = 0,
    /// <summary>Emit within a circular area using the owning emitter distribution.</summary>
    Sphere = 1,
    /// <summary>Emit the projected surface of a sphere into the disk.</summary>
    SphereSurface = 2,
    /// <summary>Emit inside a rectangle.</summary>
    Rectangle = 3,
    /// <summary>Emit at selected points with optional per-point colors.</summary>
    Points = 4,
    /// <summary>Emit at selected points with corresponding normal bases.</summary>
    DirectedPoints = 5,
    /// <summary>Emit uniformly by area between two radii.</summary>
    Ring = 6,
    /// <summary>Nonselectable bound.</summary>
    Max = 7
}
