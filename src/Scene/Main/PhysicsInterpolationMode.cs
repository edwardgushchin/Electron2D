namespace Electron2D;

/// <summary>Controls whether a node and its descendants interpolate canvas transforms between physics ticks.</summary>
public enum PhysicsInterpolationMode
{
    /// <summary>Uses the nearest ancestor policy; a root defaults to on.</summary>
    Inherit = 0,
    /// <summary>Enables interpolation when the scene tree enables it.</summary>
    On = 1,
    /// <summary>Disables interpolation for this node and inheriting descendants.</summary>
    Off = 2,
}
