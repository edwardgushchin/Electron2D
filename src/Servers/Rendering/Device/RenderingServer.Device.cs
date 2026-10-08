namespace Electron2D;

public sealed partial class RenderingServer
{
    /// <summary>Creates an independent owner-thread compute device.</summary>
    /// <returns>A caller-owned local device whose resources are private to it.</returns>
    /// <remarks>Does not require a running canvas renderer. Device and platform initialization failures propagate;
    /// the caller may separately select a CPU algorithm. Dispose the device after all submitted work is complete.</remarks>
    /// <exception cref="InvalidOperationException">The platform cannot create a compute-capable device.</exception>
    public static RenderingDevice CreateLocalRenderingDevice() => new();
}
