namespace Electron2D;

public sealed partial class IP
{
    /// <summary>Resolves the first host address synchronously.</summary>
    /// <param name="host">A hostname or address literal; ordinal cache keys preserve the supplied spelling.</param><param name="type">Any by default; None also selects the platform unspecified-family policy.</param>
    /// <returns>A normalized address string, or empty when DNS fails or no matching address exists.</returns>
    /// <exception cref="ArgumentException">The host is null or empty.</exception><exception cref="ArgumentOutOfRangeException">The family is undefined.</exception><exception cref="PlatformNotSupportedException">Native DNS is unavailable on this target.</exception>
    public static string ResolveHostname(string host, IPType type = IPType.Any) => SharedInstance.ResolveHostnameCore(host, type);
    /// <summary>Resolves all distinct matching addresses synchronously.</summary>
    /// <param name="host">A hostname or literal.</param><param name="type">The requested family, Any initially.</param><returns>A caller-owned array; empty on DNS failure or no matching addresses. Host ordering is retained.</returns>
    public static string[] ResolveHostnameAddresses(string host, IPType type = IPType.Any) => SharedInstance.ResolveHostnameAddressesCore(host, type);
    /// <summary>Reserves a queue item and starts or reuses a lookup.</summary>
    /// <param name="host">A hostname or literal.</param><param name="type">The requested family, Any initially.</param><returns>A slot in [0,255], or ResolverInvalidID when all slots remain occupied.</returns>
    /// <remarks>Completion can occur before return. Call EraseResolveItem after use; completion alone does not free a slot.</remarks>
    public static int ResolveHostnameQueueItem(string host, IPType type = IPType.Any) => SharedInstance.ResolveHostnameQueueItemCore(host, type);
    /// <summary>Reads a queued lookup's phase without waiting.</summary><param name="id">An in-range resolver slot ID.</param><returns>None for an unused/erased slot.</returns>
    public static ResolverStatus GetResolveItemStatus(int id) => SharedInstance.GetResolveItemStatusCore(id);
    /// <summary>Reads the first completed queue address without waiting.</summary><param name="id">An in-range slot ID.</param><returns>A cached string, or empty unless Done.</returns>
    public static string GetResolveItemAddress(int id) => SharedInstance.GetResolveItemAddressCore(id);
    /// <summary>Copies all completed queue addresses.</summary><param name="id">An in-range slot ID.</param><returns>A caller-owned array, or empty unless Done.</returns>
    public static string[] GetResolveItemAddresses(int id) => SharedInstance.GetResolveItemAddressesCore(id);
    /// <summary>Erases a slot and cancels its pending lookup.</summary><param name="id">An in-range slot ID; an already erased slot is a no-op.</param>
    /// <remarks>Late completion cannot publish into a reused slot or its cache entry. Existing caller snapshots stay valid.</remarks>
    public static void EraseResolveItem(int id) => SharedInstance.EraseResolveItemCore(id);
    /// <summary>Removes cached host results without erasing queue items.</summary><param name="host">Empty clears all; otherwise all family entries for this exact spelling are removed.</param>
    /// <remarks>Older in-flight lookups cannot repopulate cleared cache entries; their queue results still complete normally.</remarks>
    public static void ClearCache(string host = "") => SharedInstance.ClearCacheCore(host);
    /// <summary>Snapshots the host's network adapters and current unicast addresses.</summary><returns>A caller-owned typed array with adapter identifiers, names, string indexes and address arrays.</returns>
    /// <exception cref="PlatformNotSupportedException">Native interfaces are unavailable on this target.</exception>
    public static IPInterfaceInfo[] GetLocalInterfaces() => SharedInstance.GetLocalInterfacesCore();
    /// <summary>Snapshots all current local IPv4/IPv6 unicast addresses.</summary><returns>A caller-owned array; interface ordering is not a routing preference.</returns>
    public static string[] GetLocalAddresses() => SharedInstance.GetLocalInterfacesCore().SelectMany(adapter => adapter.Addresses).ToArray();
}
