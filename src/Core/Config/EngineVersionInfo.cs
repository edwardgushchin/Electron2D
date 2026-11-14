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

    /// <summary>Gets the major version of the loaded product.</summary>
    /// <value>The product major component, or <see cref="Version.Major"/> from <see cref="AssemblyVersion"/> when the product version is missing or its numeric part is invalid.</value>
    public int Major { get; }

    /// <summary>Gets the minor version of the loaded product.</summary>
    /// <value>The product minor component, or <see cref="Version.Minor"/> from <see cref="AssemblyVersion"/> when the product version is missing or its numeric part is invalid.</value>
    public int Minor { get; }

    /// <summary>Gets the patch version of the loaded product.</summary>
    /// <value>The product patch component, or <see cref="Version.Build"/> from <see cref="AssemblyVersion"/> when the product version is missing or its numeric part is invalid. An unspecified component is zero.</value>
    public int Patch { get; }

    /// <summary>Gets the combined numeric version.</summary>
    /// <value>
    /// The arithmetic sum <c>Major * 65536 + Minor * 256 + Patch</c> as a signed 64-bit integer,
    /// using the full component values, including values of 256 or greater.
    /// </value>
    public long Hex => (Major * 65536L) + (Minor * 256L) + Patch;

    /// <summary>Gets the release status of the loaded product.</summary>
    /// <value>The full prerelease label, <c>stable</c> if the parsed product version has no nonempty label, or <c>unknown</c> if the product version is missing or its numeric part is invalid.</value>
    public string Status { get; }

    /// <summary>Gets the recorded build configuration.</summary>
    /// <value>The assembly configuration string, or <c>unknown</c> if it is missing, empty, or whitespace.</value>
    public string Build { get; }

    /// <summary>Gets the recorded source revision identifier.</summary>
    /// <value>The source revision embedded by the build, or <c>unknown</c> if it is missing, empty, or whitespace.</value>
    public string SourceRevision { get; }

    /// <summary>Gets the recorded source revision timestamp.</summary>
    /// <value>Unix seconds as a nonnegative signed 64-bit integer. Zero indicates an unknown revision or a missing, invalid, negative, or out-of-range timestamp.</value>
    public long SourceTimestamp { get; }

    /// <summary>Returns the complete product version string.</summary>
    /// <returns><see cref="InformationalVersion"/>.</returns>
    public override string ToString() => InformationalVersion;
}
