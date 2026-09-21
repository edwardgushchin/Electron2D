namespace Electron2D;

/// <summary>Describes the version embedded in the Electron2D assembly.</summary>
public sealed class EngineVersionInfo
{
    internal EngineVersionInfo(Version assemblyVersion, string informationalVersion)
    {
        AssemblyVersion = assemblyVersion;
        InformationalVersion = informationalVersion;
    }

    /// <summary>Gets the numeric assembly version.</summary>
    /// <value>The immutable version reported by the loaded Electron2D assembly.</value>
    public Version AssemblyVersion { get; }

    /// <summary>Gets the complete product version string.</summary>
    /// <value>
    /// The assembly informational version when one is present; otherwise the numeric
    /// <see cref="AssemblyVersion"/> converted to a string.
    /// </value>
    public string InformationalVersion { get; }

    /// <summary>Returns the complete product version string.</summary>
    /// <returns><see cref="InformationalVersion"/>.</returns>
    public override string ToString() => InformationalVersion;
}
