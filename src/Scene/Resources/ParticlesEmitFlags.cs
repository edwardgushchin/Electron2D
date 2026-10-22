namespace Electron2D;

/// <summary>Selects supplied fields for manual particle emission and graph subemission.</summary>
[Flags]
public enum ParticlesEmitFlags
{
    /// <summary>Copies the supplied position.</summary>
    Position = 1,
    /// <summary>Copies the supplied rotation and scale.</summary>
    RotationScale = 2,
    /// <summary>Copies the supplied velocity.</summary>
    Velocity = 4,
    /// <summary>Copies the supplied color.</summary>
    Color = 8,
    /// <summary>Copies the supplied four numeric custom channels.</summary>
    Custom = 16,
}
