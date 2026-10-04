# PacketPeerStream

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.PacketPeerStream`.

**Inherits:** [PacketPeer](PacketPeer.md). **Inherited by:** —. **Source:** [PacketPeerStream.cs](../../src/Core/Networking/PacketPeerStream.cs).

## Description

Preserves packet boundaries over a borrowed ordered byte stream.

Wire packets use a four-byte little-endian length independent of StreamPeer.BigEndian. Zero-byte sends are ignored; a received zero length remains a valid packet. Input resizing rejects queued or partial data. Oversized headers fail explicitly rather than stalling a full buffer indefinitely.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var bytes = new StreamPeerBuffer();
using var packets = new PacketPeerStream { StreamPeer = bytes };
packets.PutPacket(new byte[] { 1, 2, 3 });
bytes.Seek(0);
byte[] received = packets.GetPacket();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public PacketPeerStream()` | Creates a detached packet wrapper with 65532-byte payload limits. |

## Constructor Descriptions

<a id="member-ef199998e9ea"></a>
### .ctor

`public PacketPeerStream()`

Creates a detached packet wrapper with 65532-byte payload limits.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Int32 InputBufferMaxSize { get; set; }` | Gets or sets the rounded input payload limit. |
| `public System.Int32 OutputBufferMaxSize { get; set; }` | Gets or sets the rounded output payload limit. |
| `public Electron2D.StreamPeer StreamPeer { get; set; }` | Gets or replaces the borrowed stream. Different identities discard queued input. |

## Property Descriptions

<a id="member-5e58e822ff77"></a>
### InputBufferMaxSize

`public System.Int32 InputBufferMaxSize { get; set; }`

Gets or sets the rounded input payload limit.

Value: 65532 initially; allocation is the next power of two of value plus four, at most 64 MiB.

System.InvalidOperationException: Input bytes are queued and would be lost.

<a id="member-125dd76dd161"></a>
### OutputBufferMaxSize

`public System.Int32 OutputBufferMaxSize { get; set; }`

Gets or sets the rounded output payload limit.

Value: 65532 initially; allocation is the next power of two of value plus four, at most 64 MiB.

<a id="member-2d4150dca78f"></a>
### StreamPeer

`public Electron2D.StreamPeer StreamPeer { get; set; }`

Gets or replaces the borrowed stream. Different identities discard queued input.

Value: Null initially. The wrapper never disposes its stream.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public override System.Int32 GetAvailablePacketCount()` | Returns the number of complete immediately available packets. |
| `public override System.Int32 GetMaxPacketSize()` | Reports the maximum outgoing raw payload supported by this transport. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `protected override System.Int32 NextPacketSize()` | Reports the next packet size without consuming it. |
| `public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)` | Sends exactly one packet. |
| `protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)` | Consumes the next complete packet into a validated destination. |

## Method Descriptions

<a id="member-be8577de1825"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-f986f930cd42"></a>
### GetAvailablePacketCount

`public override System.Int32 GetAvailablePacketCount()`

Returns the number of complete immediately available packets.

Returns: Nonnegative packet count.

<a id="member-be5649ac80e2"></a>
### GetMaxPacketSize

`public override System.Int32 GetMaxPacketSize()`

Reports the maximum outgoing raw payload supported by this transport.

Returns: A nonnegative byte count; operating-system limits may be narrower.

<a id="member-f51e527a86c1"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-12a83df77273"></a>
### NextPacketSize

`protected override System.Int32 NextPacketSize()`

Reports the next packet size without consuming it.

Returns: Length, or minus one when absent.

<a id="member-3ca5cdc05672"></a>
### PutPacket

`public override System.Void PutPacket(System.ReadOnlySpan<System.Byte> data)`

Sends exactly one packet.

data: Packet bytes, borrowed only for this call.

<a id="member-b52be5f42f63"></a>
### ReadPacketCore

`protected override System.Void ReadPacketCore(System.Span<System.Byte> destination)`

Consumes the next complete packet into a validated destination.

destination: An exact-sized destination.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/PacketPeerStream.md).
