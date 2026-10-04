# UPNPDevice

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.UPNPDevice`. **Source:** [UPNPDevice.cs](../../src/Core/Networking/UPNPDevice.cs).

## Description

Stores a discovered device and executes synchronous Internet Gateway SOAP commands.

Calls and disposal require the constructing thread. Network commands are allocating cold work, bounded to five seconds and one MiB per HTTP exchange. Collection membership borrows this object. Configuration is caller-authored; a discovered OK status follows actual description/connection validation.

**Inherits:** [ElectronObject](ElectronObject.md).

[UPNP gateway control](../components/upnp.md) defines discovery, endpoint assessment, borrowed device lifetime, input validation, synchronous network deadlines and verification limits.

## Example

Public API excerpt; discovery requires a reachable native network and allocates cold state. The collection example compiles/runs independently; UPNPTests exercises actual native SOAP and public scene use.

```csharp
using var collection = new UPNP();
using var device = new UPNPDevice();
collection.AddDevice(device);
UPNPDevice borrowed = collection.GetDevice(0);
collection.ClearDevices(); // Device stays live; explicit endpoint metadata also permits SOAP commands.
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public UPNPDevice()` | Creates an unassessed device with empty endpoint metadata. |

## Constructor Descriptions

<a id="member-ec33e424ec13"></a>
### .ctor

`public UPNPDevice()`

Creates an unassessed device with empty endpoint metadata.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.String DescriptionURL { get; set; }` | Gets or sets the description URL. |
| `public System.String IGDControlURL { get; set; }` | Gets or sets the absolute SOAP control URL. |
| `public System.String IGDOurAddress { get; set; }` | Gets or sets the local address routed to the description endpoint. |
| `public System.String IGDServiceType { get; set; }` | Gets or sets the WANIPConnection or WANPPPConnection service identity. |
| `public Electron2D.IGDStatus IGDStatus { get; set; }` | Gets or sets the gateway assessment. |
| `public System.String ServiceType { get; set; }` | Gets or sets the SSDP search-target identity. |

## Property Descriptions

<a id="member-ce2aea1b3e77"></a>
### DescriptionURL

`public System.String DescriptionURL { get; set; }`

Gets or sets the description URL.

Value: Empty initially; discovered devices retain the advertised URL.

<a id="member-4e90f7d390be"></a>
### IGDControlURL

`public System.String IGDControlURL { get; set; }`

Gets or sets the absolute SOAP control URL.

Value: Empty initially; commands require an HTTP URL.

<a id="member-8969eb5ee232"></a>
### IGDOurAddress

`public System.String IGDOurAddress { get; set; }`

Gets or sets the local address routed to the description endpoint.

Value: Empty initially; used as the internal client of a port mapping.

<a id="member-1f63a1184d3f"></a>
### IGDServiceType

`public System.String IGDServiceType { get; set; }`

Gets or sets the WANIPConnection or WANPPPConnection service identity.

Value: Empty initially; used in SOAPAction and the request namespace.

<a id="member-af8fb47810cb"></a>
### IGDStatus

`public Electron2D.IGDStatus IGDStatus { get; set; }`

Gets or sets the gateway assessment.

Value: UnknownError initially; OK marks a usable gateway.

<a id="member-190f6b244573"></a>
### ServiceType

`public System.String ServiceType { get; set; }`

Gets or sets the SSDP search-target identity.

Value: Empty initially.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public Electron2D.UPNPResult AddPortMapping(System.Int32 port, System.Int32 portInternal = 0, System.String description = "", System.String protocol = "UDP", System.Int32 duration = 0)` | Requests a mapping to this device's configured local client. |
| `public Electron2D.UPNPResult DeletePortMapping(System.Int32 port, System.String protocol = "UDP")` | Requests deletion of an external port/protocol mapping. |
| `public System.Boolean IsValidGateway()` | Reports whether this device's assessment is OK. |
| `public System.String QueryExternalAddress()` | Queries the external IP address synchronously. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |

## Method Descriptions

<a id="member-95f4a62b724d"></a>
### AddPortMapping

`public Electron2D.UPNPResult AddPortMapping(System.Int32 port, System.Int32 portInternal = 0, System.String description = "", System.String protocol = "UDP", System.Int32 duration = 0)`

Requests a mapping to this device's configured local client.

port: External port, one through 65535.

portInternal: Internal port, zero uses the external port.

description: XML-escaped label; empty uses Electron2D.

protocol: Exactly UDP or TCP.

duration: Nonnegative lease seconds; zero requests a permanent lease.

Returns: Typed local validation or gateway outcome.

Remarks: Gateways may reject conflicts or leases. No automatic renewal/removal is performed.

<a id="member-0d26a14cacaa"></a>
### DeletePortMapping

`public Electron2D.UPNPResult DeletePortMapping(System.Int32 port, System.String protocol = "UDP")`

Requests deletion of an external port/protocol mapping.

port: One through 65535.

protocol: Exactly UDP or TCP.

Returns: Typed validation or gateway outcome.

Remarks: The explicit control metadata may be used even when status is not OK.

<a id="member-ce64c5cc9218"></a>
### IsValidGateway

`public System.Boolean IsValidGateway()`

Reports whether this device's assessment is OK.

Returns: The current status projection; does not perform network work.

<a id="member-ce0e7d4a7702"></a>
### QueryExternalAddress

`public System.String QueryExternalAddress()`

Queries the external IP address synchronously.

Returns: A normalized IP literal, or empty on gateway/control/network failure.

<a id="member-43246a27ef0f"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

## Lifecycle, verification and limits

Constructing-thread queries/mutation/disposal reject a foreign thread before changing state. Disposed objects reject further access. Borrowed device membership does not transfer disposal; discovered devices remain usable through retained managed references after a collection is cleared. Discovery and commands are synchronous allocating cold work; repeated metadata access does not allocate. Configuration strings describe caller-authored endpoints and are validated when executing network commands.

[UPNPTests](../../tests/Electron2D.Tests/UPNPTests.cs) verifies independent native SSDP and IPv4/IPv6 SOAP, escaped port-mapping fields, typed gateway fault codes, description/schema/budget failures, borrowed lifetime/ordering/threading and Node/SceneTree use. A live IPv4 gateway passed read-only discovery/status/external-address querying. Real gateway mapping changes, IPv6 routed multicast, other platforms, native allocations and human/rendered/editor acceptance remain separate gates. See [platform verification](../platform-verification.md).
