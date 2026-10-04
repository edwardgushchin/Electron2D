# IGDStatus

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public enum Electron2D.IGDStatus`. **Source:** [IGDStatus.cs](../../src/Core/Networking/IGDStatus.cs).

## Description

Identifies a discovered Internet Gateway Device assessment.



[UPNP gateway control](../components/upnp.md) defines discovery, endpoint assessment, borrowed device lifetime, input validation, synchronous network deadlines and verification limits.


## Enumeration summary

| Name | Value | Contract |
| --- | ---: | --- |
| `AllocationError` | 8 | Memory allocation error. |
| `Disconnected` | 5 | Disconnected. |
| `HTTPEmpty` | 2 | Empty HTTP response. |
| `HTTPError` | 1 | HTTP error. |
| `InvalidControl` | 7 | Invalid control. |
| `NoIGD` | 4 | Not a valid IGD. |
| `NoUrls` | 3 | Returned response contained no URLs. |
| `OK` | 0 | OK. |
| `UnknownDevice` | 6 | Unknown device. |
| `UnknownError` | 9 | Unknown error. |

## Enumeration Descriptions

<a id="member-45d0bd41c03c"></a>
### AllocationError

`AllocationError = 8`

Memory allocation error.

<a id="member-4c3df6461245"></a>
### Disconnected

`Disconnected = 5`

Disconnected.

<a id="member-432e4d614aa1"></a>
### HTTPEmpty

`HTTPEmpty = 2`

Empty HTTP response.

<a id="member-9b4aed2dbbdf"></a>
### HTTPError

`HTTPError = 1`

HTTP error.

<a id="member-57779d9ad53c"></a>
### InvalidControl

`InvalidControl = 7`

Invalid control.

<a id="member-4c0d3d135fd4"></a>
### NoIGD

`NoIGD = 4`

Not a valid IGD.

<a id="member-74fa0c6414f1"></a>
### NoUrls

`NoUrls = 3`

Returned response contained no URLs.

<a id="member-235264796802"></a>
### OK

`OK = 0`

OK.

<a id="member-ea82fbe554c5"></a>
### UnknownDevice

`UnknownDevice = 6`

Unknown device.

<a id="member-c6e1bfbece76"></a>
### UnknownError

`UnknownError = 9`

Unknown error.


## Lifecycle, verification and limits

Constructing-thread queries/mutation/disposal reject a foreign thread before changing state. Disposed objects reject further access. Borrowed device membership does not transfer disposal; discovered devices remain usable through retained managed references after a collection is cleared. Discovery and commands are synchronous allocating cold work; repeated metadata access does not allocate. Configuration strings describe caller-authored endpoints and are validated when executing network commands.

[UPNPTests](../../tests/Electron2D.Tests/UPNPTests.cs) verifies independent native SSDP and IPv4/IPv6 SOAP, escaped port-mapping fields, typed gateway fault codes, description/schema/budget failures, borrowed lifetime/ordering/threading and Node/SceneTree use. A live IPv4 gateway passed read-only discovery/status/external-address querying. Real gateway mapping changes, IPv6 routed multicast, other platforms, native allocations and human/rendered/editor acceptance remain separate gates. See [platform verification](../platform-verification.md).
