# IP

Last updated: 2026-10-14

**Namespace:** `Electron2D`. **Declaration:** `public sealed partial class IP : ElectronObject`.

**Source:** [resolver](../../src/Core/Networking/IP.cs), [static API](../../src/Core/Networking/IP.API.cs). **Inherits:** [ElectronObject](ElectronObject.md). **Component:** [Address resolution](../components/address-resolution.md).

## Description

One retained process-wide service provides synchronous DNS, a bounded asynchronous hostname queue, successful-result caching and typed interface discovery. Static methods delegate to that object; Engine.GetSingleton("IP") preserves its identity and rejects disposal/unregistration. There is no public constructor or Instance accessor. Calls are lock-serialized and thread safe; worker completion publishes resolver data without touching a scene owner.

Cache keys retain exact host spelling and IPType. None and Any use the host's unspecified-family policy; IPv4/IPv6 filter matching results. Duplicate addresses are removed while host ordering and IPv6 scope remain. Successful entries persist until explicit ClearCache; failure is not cached. TCP/UDP/ENet hostname helpers and HTTP asynchronous establishment share the same backend/cache. Transport operations still require an address and keep their typed failures.

Queue IDs address 256 prepared slots. None → Waiting → Done/Error; cached entries can enter Done immediately. Done/Error still occupies a slot. Erasure retires a generation before cancellation, so a late old response cannot publish under a recycled ID/hostname. Cache clearing preserves pending queue results but prevents older lookups from repopulating removed cache entries. Array/adapter snapshots belong to the caller. Cold DNS, tasks, interface enumeration, snapshots and errors may allocate; prepared cached queue/status/first-string/erase cycles allocate no managed owner bytes after warmup.

## Example

```csharp
int id = IP.ResolveHostnameQueueItem("localhost", IPType.IPv4);
if (id != IP.ResolverInvalidID)
{
    // Poll on later frames; do not busy-wait on a scene owner.
    var status = IP.GetResolveItemStatus(id);
    if (status == ResolverStatus.Done)
    {
        string address = IP.GetResolveItemAddress(id);
    }
    if (status is ResolverStatus.Done or ResolverStatus.Error)
    {
        IP.EraseResolveItem(id);
        id = IP.ResolverInvalidID;
    }
}
```

The owning application must erase the ID after Done/Error or when abandoning a request. The snippet illustrates one polling step; retain the ID between frames.

## API summary

| Complete signature | Contract |
| --- | --- |
| `public static string ResolveHostname(string host, IPType type = IPType.Any)` | Returns the first normalized host address. |
| `public static string[] ResolveHostnameAddresses(string host, IPType type = IPType.Any)` | Returns distinct addresses in host order as a caller-owned array. |
| `public static int ResolveHostnameQueueItem(string host, IPType type = IPType.Any)` | Reserves the smallest free slot in [0,255]. |
| `public static ResolverStatus GetResolveItemStatus(int id)` | Reads phase without waiting. |
| `public static string GetResolveItemAddress(int id)` | Returns the first retained string only in Done, otherwise empty. |
| `public static string[] GetResolveItemAddresses(int id)` | Copies completed strings into a caller-owned array; other phases return empty. |
| `public static void EraseResolveItem(int id)` | Retires slot identity and result before canceling pending host work. |
| `public static void ClearCache(string host = "")` | Empty clears all cache entries; a nonempty exact spelling clears all four family entries for that host. |
| `public static IPInterfaceInfo[] GetLocalInterfaces()` | Enumerates current host adapters through the native . |
| `public static string[] GetLocalAddresses()` | Flattens a fresh adapter snapshot into caller-owned address strings. |

## Method descriptions

### ResolveHostname

`public static string ResolveHostname(string host, IPType type = IPType.Any)`. Returns the first normalized host address. An exact hostname/family cache hit returns its retained string without allocation. Uncached DNS blocks as explicit cold work. Native DNS failure or no matching address returns empty; invalid/empty/null host or undefined family rejects, and unavailable native lookup throws PlatformNotSupportedException.

### ResolveHostnameAddresses

`public static string[] ResolveHostnameAddresses(string host, IPType type = IPType.Any)`. Returns distinct addresses in host order as a caller-owned array. A zero-address DNS result/failure is empty and not negatively cached. Modifying the array cannot change retained cache state. Snapshot copying is allocating cold work; validation and host capability follow ResolveHostname.

### ResolveHostnameQueueItem

`public static int ResolveHostnameQueueItem(string host, IPType type = IPType.Any)`. Reserves the smallest free slot in [0,255]. Cached success can be Done before return; otherwise native lookup starts as cold asynchronous work. Completion retains the slot until EraseResolveItem. Capacity exhaustion returns ResolverInvalidID. Argument validation precedes reservation; queued lookup failure/no matching address produces Error.

### GetResolveItemStatus

`public static ResolverStatus GetResolveItemStatus(int id)`. Reads phase without waiting. None describes an in-range unused or erased slot. IDs outside [0,255] throw ArgumentOutOfRangeException. Successful warmed status reads allocate no owner bytes.

### GetResolveItemAddress

`public static string GetResolveItemAddress(int id)`. Returns the first retained string only in Done, otherwise empty. Does not wait or copy the string. An out-of-range ID rejects.

### GetResolveItemAddresses

`public static string[] GetResolveItemAddresses(int id)`. Copies completed strings into a caller-owned array; other phases return empty. An out-of-range ID rejects. Copying is allocating snapshot work.

### EraseResolveItem

`public static void EraseResolveItem(int id)`. Retires slot identity and result before canceling pending host work. The slot can immediately be reused; an old completion cannot replace a newer result or cache entry. An in-range already erased slot is a no-op; other IDs reject. Old caller snapshots remain valid.

### ClearCache

`public static void ClearCache(string host = "")`. Empty clears all cache entries; a nonempty exact spelling clears all four family entries for that host. Null rejects. Existing unrelated cached entries and retained queue results survive. Earlier in-flight results may still complete their queue but cannot repopulate cleared cache entries. No TTL/automatic expiry is added.

### GetLocalInterfaces

`public static IPInterfaceInfo[] GetLocalInterfaces()`. Enumerates current host adapters through the native .NET backend. Name is the adapter identifier, Friendly its display name, Index a decimal string and Addresses a new array of IPv4/IPv6 unicast strings. Host IPv6 scopes are preserved. Enumeration order does not imply routing priority. Unavailable native interface enumeration throws PlatformNotSupportedException; host enumeration failures retain typed exceptions.

### GetLocalAddresses

`public static string[] GetLocalAddresses()`. Flattens a fresh adapter snapshot into caller-owned address strings. Interface order is not a routing preference. It is a cold native enumeration/snapshot operation with the same capability limits as GetLocalInterfaces.

## Constants

| Declaration | Contract |
| --- | --- |
| `public const int ResolverInvalidID = -1` | Queue capacity reservation failed. |
| `public const int ResolverMaxQueries = 256` | Retained slot capacity, including completed queries. |

### ResolverInvalidID

The caller must check -1 before querying a returned ID; ordinary ID getters reject an out-of-range value.

### ResolverMaxQueries

All 256 slots remain addressable. Erasing any one allows immediate reuse; source queue values are not expanded by a hidden buffer-growth policy.

## Protected lifetime hooks

`protected override void ValidateDisposal()` rejects disposal of the permanent service before mutation. `protected override void Dispose(bool disposing)` retires isolated internal fixture queries and cancels host work before inherited cleanup. These overrides preserve ElectronObject lifetime behavior; the public type is sealed.

## Verification and limits

[IPTests](../../tests/Electron2D.Tests/IPTests.cs) verifies cache family/order/isolation, 256 occupied slots, erasure/reuse and late completion, cache-clear races, lookup failures, permanent identity, native IPv4/IPv6 localhost/interfaces and 128 prepared cache/queue/status/string/erase cycles at zero managed owner bytes. Its public Node/SceneTree consumer polls a real lookup and completes a native TCP wire request. NetworkingTests and HTTPTests verify connected native transports, protocol/scene/worker paths and unchanged warm span budgets.

Linux native execution is distinct from foreign host, browser-native capability, routed DNS, DNS server changes and native allocator acceptance. This component produces no rendered output. ADRs 0094, 0095, 0001, 0014, 0021 and 0051 govern typed API, service identity and verification.
