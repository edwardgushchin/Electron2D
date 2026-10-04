# UPNP

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.UPNP`. **Source:** [UPNP.cs](../../src/Core/Networking/UPNP.cs).

## Description

Discovers local Internet Gateway Devices and delegates synchronous control commands.

Calls/disposal require the constructing thread. Discovery clears membership after parameter validation and borrows returned devices; clearing/disposal does not dispose them. Network operations allocate and block explicitly; use a dedicated caller thread when gameplay cannot wait.

**Inherits:** [ElectronObject](ElectronObject.md).

[UPNP gateway control](../components/upnp.md) defines discovery, endpoint assessment, borrowed device lifetime, input validation, synchronous network deadlines and verification limits.

## Example

Public API excerpt; discovery requires a reachable native network and allocates cold state. The collection example compiles/runs independently; UPNPTests exercises actual native SOAP and public scene use.

```csharp
using var upnp = new UPNP();
UPNPResult result = upnp.Discover();
UPNPDevice? gateway = upnp.GetGateway();
string externalAddress = upnp.QueryExternalAddress();
// Network commands block this constructing thread. Devices are borrowed.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public UPNP()` | Creates an empty discovery/control collection. |

## Constructor Descriptions

<a id="member-b433a8f6fd67"></a>
### .ctor

`public UPNP()`

Creates an empty discovery/control collection.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean DiscoverIPV6 { get; set; }` | Gets or sets whether discovery uses IPv6. |
| `public System.Int32 DiscoverLocalPort { get; set; }` | Gets or sets the discovery source port. |
| `public System.String DiscoverMulticastIF { get; set; }` | Gets or sets the discovery interface. |

## Property Descriptions

<a id="member-dfce6cc7b307"></a>
### DiscoverIPV6

`public System.Boolean DiscoverIPV6 { get; set; }`

Gets or sets whether discovery uses IPv6.

Value: False initially. IPv6 sends site-local and link-local SSDP on the selected native interface.

<a id="member-d46a99e274dc"></a>
### DiscoverLocalPort

`public System.Int32 DiscoverLocalPort { get; set; }`

Gets or sets the discovery source port.

Value: Zero selects an ephemeral port; one selects 1900; otherwise zero through 65535.

<a id="member-7b676c4c9360"></a>
### DiscoverMulticastIF

`public System.String DiscoverMulticastIF { get; set; }`

Gets or sets the discovery interface.

Value: Empty selects the system default; a native interface name/ID or IP literal selects an interface.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void AddDevice(Electron2D.UPNPDevice device)` | Appends a borrowed live device. |
| `public Electron2D.UPNPResult AddPortMapping(System.Int32 port, System.Int32 portInternal = 0, System.String description = "", System.String protocol = "UDP", System.Int32 duration = 0)` | Requests a port mapping through the first valid gateway. |
| `public System.Void ClearDevices()` | Clears memberships while preserving caller-held device lifetimes. |
| `public Electron2D.UPNPResult DeletePortMapping(System.Int32 port, System.String protocol = "UDP")` | Deletes a mapping through the first valid gateway. |
| `public Electron2D.UPNPResult Discover(System.Int32 timeout = 2000, System.Int32 ttl = 2, System.String deviceFilter = "InternetGatewayDevice")` | Replaces memberships by synchronous SSDP discovery and gateway assessment. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public Electron2D.UPNPDevice GetDevice(System.Int32 index)` | Returns a borrowed device at an index. |
| `public System.Int32 GetDeviceCount()` | Returns the ordered membership count. |
| `public Electron2D.UPNPDevice GetGateway()` | Finds the first live device assessed as a valid gateway. |
| `public System.String QueryExternalAddress()` | Queries the first valid gateway synchronously. |
| `public System.Void RemoveDevice(System.Int32 index)` | Removes one membership without disposing the device. |
| `public System.Void SetDevice(System.Int32 index, Electron2D.UPNPDevice device)` | Replaces one membership without disposing either device. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-e240ab2d1c32"></a>
### AddDevice

`public System.Void AddDevice(Electron2D.UPNPDevice device)`

Appends a borrowed live device.

device: Device belonging to the same owner thread; duplicates are permitted.

<a id="member-4101eddeee85"></a>
### AddPortMapping

`public Electron2D.UPNPResult AddPortMapping(System.Int32 port, System.Int32 portInternal = 0, System.String description = "", System.String protocol = "UDP", System.Int32 duration = 0)`

Requests a port mapping through the first valid gateway.

port: External port.

portInternal: Zero uses external port.

description: Label; empty selects Electron2D.

protocol: UDP or TCP.

duration: Nonnegative seconds, zero permanent.

Returns: NoGateway or the device's typed outcome.

<a id="member-a3d8d6fff986"></a>
### ClearDevices

`public System.Void ClearDevices()`

Clears memberships while preserving caller-held device lifetimes.

<a id="member-a5d5744e32b4"></a>
### DeletePortMapping

`public Electron2D.UPNPResult DeletePortMapping(System.Int32 port, System.String protocol = "UDP")`

Deletes a mapping through the first valid gateway.

port: External port.

protocol: UDP or TCP.

Returns: NoGateway or the device's typed outcome.

<a id="member-714fa84e2c7c"></a>
### Discover

`public Electron2D.UPNPResult Discover(System.Int32 timeout = 2000, System.Int32 ttl = 2, System.String deviceFilter = "InternetGatewayDevice")`

Replaces memberships by synchronous SSDP discovery and gateway assessment.

timeout: Nonnegative response wait in milliseconds, 2000 initially.

ttl: Multicast TTL/hops, zero through 255.

deviceFilter: Case-sensitive search-target substring; empty retains all discovered targets.

Returns: Discovery result; Success may contain no matching devices or only invalid gateways.

Remarks: Invalid parameters preserve memberships. Description/status requests have independent five-second deadlines; the wait does not bound the whole operation. Native responses are deduplicated by location and target, up to 256 identities; each description/SOAP body is at most one MiB. Browser raw sockets are unavailable.

<a id="member-d8cb36f3d249"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-3d47605d67b3"></a>
### GetDevice

`public Electron2D.UPNPDevice GetDevice(System.Int32 index)`

Returns a borrowed device at an index.

index: Zero-based existing index.

Returns: The live device.

System.ArgumentOutOfRangeException: Index is absent.

<a id="member-cebb96bd2342"></a>
### GetDeviceCount

`public System.Int32 GetDeviceCount()`

Returns the ordered membership count.

Returns: Device count, including explicitly added entries.

<a id="member-5edc5e30fcbd"></a>
### GetGateway

`public Electron2D.UPNPDevice GetGateway()`

Finds the first live device assessed as a valid gateway.

Returns: A borrowed device or null; performs no network operation.

<a id="member-551dc8ea5c07"></a>
### QueryExternalAddress

`public System.String QueryExternalAddress()`

Queries the first valid gateway synchronously.

Returns: A normalized external address or empty on failure/no gateway.

<a id="member-7555795a7b93"></a>
### RemoveDevice

`public System.Void RemoveDevice(System.Int32 index)`

Removes one membership without disposing the device.

index: Existing zero-based index.

<a id="member-60c02775b0f7"></a>
### SetDevice

`public System.Void SetDevice(System.Int32 index, Electron2D.UPNPDevice device)`

Replaces one membership without disposing either device.

index: Existing zero-based index.

device: Borrowed live replacement on this thread.

<a id="member-05ce18e9ec97"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, verification and limits

Constructing-thread queries/mutation/disposal reject a foreign thread before changing state. Disposed objects reject further access. Borrowed device membership does not transfer disposal; discovered devices remain usable through retained managed references after a collection is cleared. Discovery and commands are synchronous allocating cold work; repeated metadata access does not allocate. Configuration strings describe caller-authored endpoints and are validated when executing network commands.

[UPNPTests](../../tests/Electron2D.Tests/UPNPTests.cs) verifies independent native SSDP and IPv4/IPv6 SOAP, escaped port-mapping fields, typed gateway fault codes, description/schema/budget failures, borrowed lifetime/ordering/threading and Node/SceneTree use. A live IPv4 gateway passed read-only discovery/status/external-address querying. Real gateway mapping changes, IPv6 routed multicast, other platforms, native allocations and human/rendered/editor acceptance remain separate gates. See [platform verification](../platform-verification.md).
