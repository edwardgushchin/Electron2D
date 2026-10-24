namespace Electron2D;

/// <summary>Sets the maximum anisotropy for canvas filters that request it.</summary>
public enum AnisotropicFiltering
{
    /// <summary>Disables anisotropy.</summary>
    Disabled = 0,
    /// <summary>At most two samples.</summary>
    Anisotropy2X = 1,
    /// <summary>At most four samples; the default.</summary>
    Anisotropy4X = 2,
    /// <summary>At most eight samples.</summary>
    Anisotropy8X = 3,
    /// <summary>At most sixteen samples.</summary>
    Anisotropy16X = 4,
    /// <summary>Sentinel; not a valid choice.</summary>
    Max = 5,
}
