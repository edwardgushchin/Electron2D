namespace Electron2D;

public sealed partial class RenderingDevice
{
    /// <summary>Selects native allocation memory for the device's shared physical payload.</summary>
    public enum MemoryType
    {
        /// <summary>Allocated texture regions, including alignment, cycling and pending native retirement.</summary>
        Textures = 0,
        /// <summary>Allocated buffer regions, including uniform and transfer storage.</summary>
        Buffers = 1,
        /// <summary>Committed native allocation blocks, including retained free space.</summary>
        Total = 2
    }

    /// <summary>Reads native allocation memory in bytes without submitting or waiting for GPU work.</summary>
    /// <param name="type">The native allocation category.</param>
    /// <returns>Bytes owned by the shared physical device, including other live renderer, compute and physics leases.</returns>
    /// <remarks>Uses the ordinary device owner and lifetime guards. Queries acquire the native allocator mutex;
    /// separate reads are nearby snapshots. Borrowing shares the source allocation; private image-support snapshots
    /// and uploads still contribute their actual native storage. Native release may be deferred until
    /// completion, and free allocation blocks can remain reserved. Total includes that reservation rather than
    /// summing texture and buffer usage. Driver-internal allocations outside the native allocator are excluded.
    /// Vulkan on pinned SDL 3.4.18 supplies the native adapter; other native drivers/releases reject explicitly.</remarks>
    /// <exception cref="NotSupportedException">The selected native driver or core has no compatible memory adapter.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The category is undefined.</exception>
    /// <exception cref="OverflowException">The native byte count exceeds Int64.</exception>
    public long GetMemoryUsage(MemoryType type)
    {
        Access();
        if ((uint)type > 2) throw new ArgumentOutOfRangeException(nameof(type));
        return GPUDeviceFactory.ReadMemory(_context.Handle).Select(type);
    }
}
