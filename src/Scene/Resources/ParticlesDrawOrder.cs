namespace Electron2D;

/// <summary>Selects planar particle draw ordering.</summary>
public enum ParticlesDrawOrder
{
    /// <summary>Original particle index.</summary>
    Index = 0,
    /// <summary>Oldest particles first.</summary>
    Lifetime = 1,
    /// <summary>Youngest particles first.</summary>
    ReverseLifetime = 2,
}
