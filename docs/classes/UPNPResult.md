# UPNPResult

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.UPNPResult`. **Source:** [UPNPResult.cs](../../src/Core/Networking/UPNPResult.cs).

## Description

Identifies UPNP discovery/control outcomes.



[UPNP gateway control](../components/upnp.md) defines discovery, endpoint assessment, borrowed device lifetime, input validation, synchronous network deadlines and verification limits.


## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `ActionFailed` | 5 | The action failed. |
| `ConflictWithOtherMapping` | 13 | Conflict with an existing port mapping. |
| `ConflictWithOtherMechanism` | 12 | Conflict with other mechanism. May be returned instead of ConflictWithOtherMapping if a port mapping conflicts with an existing one. |
| `ExternalPortMustBeWildcard` | 10 | The external port value must be a wildcard. |
| `ExternalPortWildcardNotPermitted` | 7 | The UPNP device does not allow wildcard values for the external port. |
| `HTTPError` | 23 | HTTP error. |
| `InconsistentParameters` | 3 | Inconsistent parameters. |
| `InternalPortWildcardNotPermitted` | 8 | The UPNP device does not allow wildcard values for the internal port. |
| `InvalidArguments` | 20 | Invalid arguments. |
| `InvalidDuration` | 19 | Invalid duration. |
| `InvalidGateway` | 16 | Invalid gateway. |
| `InvalidParameter` | 22 | Invalid parameter. |
| `InvalidPort` | 17 | Invalid port. |
| `InvalidProtocol` | 18 | Invalid protocol. |
| `InvalidResponse` | 21 | Invalid response. |
| `MemoryAllocationError` | 25 | Error allocating memory. |
| `NoDevices` | 27 | No devices available. You may need to call Discover first, or discovery didn't detect any valid UPNP devices. |
| `NoGateway` | 26 | No gateway available. You may need to call Discover first, or discovery didn't detect any valid IGDs (InternetGatewayDevices). |
| `NoPortMapsAvailable` | 11 | No port maps are available. May also be returned if port mapping functionality is not available. |
| `NoSuchEntryInArray` | 4 | No such entry in array. May be returned if a given port, protocol combination is not found on a UPNP device. |
| `NotAuthorized` | 1 | Not authorized to use the command on the UPNP device. May be returned when the user disabled UPNP on their router. |
| `OnlyPermanentLeaseSupported` | 15 | Only permanent leases are supported. Do not use the duration parameter when adding port mappings. |
| `PortMappingNotFound` | 2 | No port mapping was found for the given port, protocol combination on the given UPNP device. |
| `RemoteHostMustBeWildcard` | 9 | The remote host value must be a wildcard. |
| `SamePortValuesRequired` | 14 | External and internal port values must be the same. |
| `SocketError` | 24 | Socket error. |
| `SourceIPWildcardNotPermitted` | 6 | The UPNP device does not allow wildcard values for the source IP address. |
| `Success` | 0 | UPNP command or discovery was successful. |
| `UnknownError` | 28 | Unknown error. |

## Enumeration Descriptions

<a id="member-07162f775d99"></a>
### ActionFailed

`ActionFailed = 5`

The action failed.

<a id="member-b1622bab1b2f"></a>
### ConflictWithOtherMapping

`ConflictWithOtherMapping = 13`

Conflict with an existing port mapping.

<a id="member-ef9e54a1fe05"></a>
### ConflictWithOtherMechanism

`ConflictWithOtherMechanism = 12`

Conflict with other mechanism. May be returned instead of ConflictWithOtherMapping if a port mapping conflicts with an existing one.

<a id="member-9e3cd8d4d0b7"></a>
### ExternalPortMustBeWildcard

`ExternalPortMustBeWildcard = 10`

The external port value must be a wildcard.

<a id="member-508c9f9a1dc2"></a>
### ExternalPortWildcardNotPermitted

`ExternalPortWildcardNotPermitted = 7`

The UPNP device does not allow wildcard values for the external port.

<a id="member-6bde014a5868"></a>
### HTTPError

`HTTPError = 23`

HTTP error.

<a id="member-cd75db3c6317"></a>
### InconsistentParameters

`InconsistentParameters = 3`

Inconsistent parameters.

<a id="member-2ff0d2bde495"></a>
### InternalPortWildcardNotPermitted

`InternalPortWildcardNotPermitted = 8`

The UPNP device does not allow wildcard values for the internal port.

<a id="member-00b789b542e5"></a>
### InvalidArguments

`InvalidArguments = 20`

Invalid arguments.

<a id="member-d435d6903205"></a>
### InvalidDuration

`InvalidDuration = 19`

Invalid duration.

<a id="member-62a8eb1b9d50"></a>
### InvalidGateway

`InvalidGateway = 16`

Invalid gateway.

<a id="member-056fdcd542f7"></a>
### InvalidParameter

`InvalidParameter = 22`

Invalid parameter.

<a id="member-6dc6ca29b60e"></a>
### InvalidPort

`InvalidPort = 17`

Invalid port.

<a id="member-4e0206a5c37f"></a>
### InvalidProtocol

`InvalidProtocol = 18`

Invalid protocol.

<a id="member-0852392aa76c"></a>
### InvalidResponse

`InvalidResponse = 21`

Invalid response.

<a id="member-c8af3a159d09"></a>
### MemoryAllocationError

`MemoryAllocationError = 25`

Error allocating memory.

<a id="member-d5522016c799"></a>
### NoDevices

`NoDevices = 27`

No devices available. You may need to call Discover first, or discovery didn't detect any valid UPNP devices.

<a id="member-b695e0e9337e"></a>
### NoGateway

`NoGateway = 26`

No gateway available. You may need to call Discover first, or discovery didn't detect any valid IGDs (InternetGatewayDevices).

<a id="member-e6f30471af7d"></a>
### NoPortMapsAvailable

`NoPortMapsAvailable = 11`

No port maps are available. May also be returned if port mapping functionality is not available.

<a id="member-3c4b89c5d5f8"></a>
### NoSuchEntryInArray

`NoSuchEntryInArray = 4`

No such entry in array. May be returned if a given port, protocol combination is not found on a UPNP device.

<a id="member-9191f8054261"></a>
### NotAuthorized

`NotAuthorized = 1`

Not authorized to use the command on the UPNP device. May be returned when the user disabled UPNP on their router.

<a id="member-86d09520ee84"></a>
### OnlyPermanentLeaseSupported

`OnlyPermanentLeaseSupported = 15`

Only permanent leases are supported. Do not use the duration parameter when adding port mappings.

<a id="member-42c1a3c86e39"></a>
### PortMappingNotFound

`PortMappingNotFound = 2`

No port mapping was found for the given port, protocol combination on the given UPNP device.

<a id="member-1fac1e324ecb"></a>
### RemoteHostMustBeWildcard

`RemoteHostMustBeWildcard = 9`

The remote host value must be a wildcard.

<a id="member-1a0054cfcd2a"></a>
### SamePortValuesRequired

`SamePortValuesRequired = 14`

External and internal port values must be the same.

<a id="member-d10c92f845f1"></a>
### SocketError

`SocketError = 24`

Socket error.

<a id="member-2b200a2ea372"></a>
### SourceIPWildcardNotPermitted

`SourceIPWildcardNotPermitted = 6`

The UPNP device does not allow wildcard values for the source IP address.

<a id="member-e52435e9a9b0"></a>
### Success

`Success = 0`

UPNP command or discovery was successful.

<a id="member-8423eb6756cf"></a>
### UnknownError

`UnknownError = 28`

Unknown error.


## Lifecycle, verification and limits

Constructing-thread queries/mutation/disposal reject a foreign thread before changing state. Disposed objects reject further access. Borrowed device membership does not transfer disposal; discovered devices remain usable through retained managed references after a collection is cleared. Discovery and commands are synchronous allocating cold work; repeated metadata access does not allocate. Configuration strings describe caller-authored endpoints and are validated when executing network commands.

[UPNPTests](../../tests/Electron2D.Tests/UPNPTests.cs) verifies independent native SSDP and IPv4/IPv6 SOAP, escaped port-mapping fields, typed gateway fault codes, description/schema/budget failures, borrowed lifetime/ordering/threading and Node/SceneTree use. A live IPv4 gateway passed read-only discovery/status/external-address querying. Real gateway mapping changes, IPv6 routed multicast, other platforms, native allocations and human/rendered/editor acceptance remain separate gates. See [platform verification](../platform-verification.md).
