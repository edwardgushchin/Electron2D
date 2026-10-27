namespace Electron2D;

/// <summary>An immutable native host-memory snapshot in bytes, with -1 for each unavailable value.</summary>
/// <remarks>Values describe the host rather than the managed heap or a container's memory quota.
/// Availability is advisory and can change immediately after the query.</remarks>
public readonly struct OSMemoryInfo
{
    internal OSMemoryInfo(long physical, long free, long available, long stack)
    { Physical = physical; Free = free; Available = available; Stack = stack; }

    /// <summary>Gets the total usable physical memory, or -1 when unavailable.</summary>
    public long Physical { get; }
    /// <summary>Gets the currently free physical memory, or -1 when unavailable.</summary>
    public long Free { get; }
    /// <summary>Gets the native allocation budget including swap, or -1 when unavailable.</summary>
    /// <remarks>Linux/Android report free physical memory plus free swap; Apple reports free memory plus
    /// available swap (no swap on mobile/TV); Windows reports the system commit limit, including existing commitments.</remarks>
    public long Available { get; }
    /// <summary>Gets the native stack budget, or -1 when unavailable or unlimited.</summary>
    /// <remarks>Unix reports the process soft stack limit; Windows reports the calling thread's stack address range.</remarks>
    public long Stack { get; }
}
