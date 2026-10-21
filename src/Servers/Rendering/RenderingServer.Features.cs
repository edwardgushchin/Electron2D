namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Reports a native compressed-image feature of the active rendering backend.</summary>
    /// <param name="feature">Feature tag such as s3tc, rgtc or bptc; case-insensitive.</param>
    /// <returns>Whether all formats in the named native ordinary-image upload family are available.</returns>
    /// <remarks>CPU decoding does not imply native upload support. Queries use an immutable session snapshot,
    /// may run on any thread and do not create a rendering device. Unknown features return false.</remarks>
    /// <exception cref="ArgumentNullException">The feature is null.</exception>
    /// <exception cref="InvalidOperationException">The rendering service is not active.</exception>
    public static bool HasOSFeature(string feature)
    {
        ArgumentNullException.ThrowIfNull(feature); return RequireService()._backend.HasOSFeature(feature);
    }
    internal static bool QueryOSFeature(string feature) => Service?._backend.HasOSFeature(feature) ?? false;
}
