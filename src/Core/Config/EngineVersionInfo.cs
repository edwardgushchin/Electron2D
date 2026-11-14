using System.Globalization;
using System.Reflection;

namespace Electron2D;

/// <summary>Describes the version embedded in the Electron2D assembly.</summary>
public sealed class EngineVersionInfo
{
    private EngineVersionInfo(Version assemblyVersion, string informationalVersion, string? productVersion,
        string? build, string? sourceRevision, string? sourceTimestamp)
    {
        AssemblyVersion = assemblyVersion;
        InformationalVersion = informationalVersion;
        var versionAndStatus = productVersion?.Split('+', 2)[0].Split('-', 2);
        if (versionAndStatus is not null && Version.TryParse(versionAndStatus[0], out var numericVersion))
        {
            Major = numericVersion.Major;
            Minor = numericVersion.Minor;
            Patch = Math.Max(0, numericVersion.Build);
            Status = versionAndStatus.Length == 2 && versionAndStatus[1].Length > 0 ? versionAndStatus[1] : "stable";
        }
        else
        {
            Major = assemblyVersion.Major;
            Minor = assemblyVersion.Minor;
            Patch = Math.Max(0, assemblyVersion.Build);
            Status = "unknown";
        }
        Build = string.IsNullOrWhiteSpace(build) ? "unknown" : build;
        SourceRevision = string.IsNullOrWhiteSpace(sourceRevision) ? "unknown" : sourceRevision;
        if (SourceRevision != "unknown" &&
            long.TryParse(sourceTimestamp, NumberStyles.None, CultureInfo.InvariantCulture, out long timestamp))
            SourceTimestamp = timestamp;
    }

    internal static EngineVersionInfo FromAssembly(Assembly assembly)
    {
        var assemblyVersion = assembly.GetName().Version ?? new Version(0, 0);
        var informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
        var build = assembly.GetCustomAttribute<AssemblyConfigurationAttribute>()?.Configuration;
        string? productVersion = null, sourceRevision = null, sourceTimestamp = null;
        foreach (var metadata in assembly.GetCustomAttributes<AssemblyMetadataAttribute>())
        {
            switch (metadata.Key)
            {
                case "Electron2D.ProductVersion":
                    productVersion = metadata.Value;
                    break;
                case "Electron2D.SourceRevision":
                    sourceRevision = metadata.Value;
                    break;
                case "Electron2D.SourceTimestamp":
                    sourceTimestamp = metadata.Value;
                    break;
            }
        }
        return new EngineVersionInfo(assemblyVersion, informationalVersion ?? assemblyVersion.ToString(),
            productVersion, build, sourceRevision, sourceTimestamp);
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

    /// <summary>Gets the product major version, falling back to the assembly major version.</summary>
    public int Major { get; }

    /// <summary>Gets the product minor version, falling back to the assembly minor version.</summary>
    public int Minor { get; }

    /// <summary>Gets the product patch version, falling back to the assembly build component or zero.</summary>
    public int Patch { get; }

    /// <summary>Gets the numeric product version encoded as major times 65536 plus minor times 256 plus patch.</summary>
    public long Hex => (Major * 65536L) + (Minor * 256L) + Patch;

    /// <summary>Gets the product prerelease label, <c>stable</c> for a release version, or <c>unknown</c> when product metadata is unavailable.</summary>
    public string Status { get; }

    /// <summary>Gets the recorded assembly build configuration or <c>unknown</c>.</summary>
    public string Build { get; }

    /// <summary>Gets the recorded source revision identifier or <c>unknown</c>.</summary>
    public string SourceRevision { get; }

    /// <summary>Gets the recorded source revision's Unix timestamp in seconds, or zero when unavailable or invalid.</summary>
    public long SourceTimestamp { get; }

    /// <summary>Returns the complete product version string.</summary>
    /// <returns><see cref="InformationalVersion"/>.</returns>
    public override string ToString() => InformationalVersion;
}
