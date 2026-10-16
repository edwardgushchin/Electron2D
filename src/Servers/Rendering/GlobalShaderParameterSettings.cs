using System.Text.Json.Serialization;

namespace Electron2D;

/// <summary>Stores one typed project-authored global shader parameter.</summary>
/// <typeparam name="T">The exact numeric carrier, float array for Mat3/Mat4, or resource path string for a sampled image.</typeparam>
/// <remarks>Register a ProjectSetting of this value type under shader_globals/name before opening the rendering session.
/// The renderer snapshots active feature overrides before scene readiness. Later setting edits apply to the next session.
/// Numeric matrices are copied through project serialization; image paths are loaded into session-owned resources.
/// Invalid schema roles, carriers and numeric values reject preparation. Sampled format and backend capabilities validate when a material submits.</remarks>
[JsonUnmappedMemberHandling(JsonUnmappedMemberHandling.Disallow)]
public sealed class GlobalShaderParameterSettings<T> where T : notnull
{
    /// <summary>Creates a version-one global parameter definition.</summary>
    public GlobalShaderParameterSettings() { }
    /// <summary>Gets the stored schema version.</summary>
    /// <value>One for the supported schema.</value>
    public int Version { get; init; } = 1;
    /// <summary>Gets the logical shader value role.</summary>
    /// <value>The role whose exact carrier must match T.</value>
    public RenderingServer.GlobalShaderParameterType Type { get; init; }
    /// <summary>Gets the value snapshotted by the project setting.</summary>
    /// <value>A finite numeric value, nine or sixteen matrix elements, or an image resource path. An empty image path leaves the base unassigned.</value>
    public T Value { get; init; } = default!;
}
