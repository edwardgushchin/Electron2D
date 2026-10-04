# StreamPeerBuffer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public class Electron2D.StreamPeerBuffer`.

**Inherits:** [StreamPeer](StreamPeer.md). **Inherited by:** —. **Source:** [StreamPeerBuffer.cs](../../src/Core/Networking/StreamPeerBuffer.cs).

## Description

Stores a seekable, growable byte stream with copied array boundaries.

Clear retains capacity; Duplicate copies bytes and resets position and endian configuration. Resize clamps a cursor beyond the new end, avoiding negative reads. No native socket is required.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var buffer = new StreamPeerBuffer { BigEndian = true };
buffer.PutUTF8String("hello");
buffer.Seek(0);
string value = buffer.GetUTF8String();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `public StreamPeerBuffer()` | Creates an empty buffer. |

## Constructor Descriptions

<a id="member-6622dcf5f73f"></a>
### .ctor

`public StreamPeerBuffer()`

Creates an empty buffer.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Byte[] DataArray { get; set; }` | Gets a caller-owned byte copy, or replaces bytes with a copied array and resets position. |

## Property Descriptions

<a id="member-fbf56a8bf59b"></a>
### DataArray

`public System.Byte[] DataArray { get; set; }`

Gets a caller-owned byte copy, or replaces bytes with a copied array and resets position.

Value: Empty initially.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Void Clear()` | Clears bytes and cursor while retaining prepared capacity. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Deterministically releases resources owned by this object. |
| `public Electron2D.StreamPeerBuffer Duplicate()` | Copies bytes into a new buffer with default byte order and cursor zero. |
| `public override System.Int32 GetAvailableBytes()` | Reports immediately readable bytes without consuming them. |
| `public System.Int32 GetPosition()` | Returns the cursor byte offset. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.Int32 GetSize()` | Returns the logical byte length. |
| `protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)` | Reads a prefix for the concrete transport. |
| `public System.Void Resize(System.Int32 size)` | Changes logical length, zero-filling growth and clamping the cursor when shrinking. |
| `public System.Void Seek(System.Int32 position)` | Moves within the inclusive interval from zero to GetSize. |
| `protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)` | Writes a prefix for the concrete transport. |

## Method Descriptions

<a id="member-aaeb0791bd8d"></a>
### Clear

`public System.Void Clear()`

Clears bytes and cursor while retaining prepared capacity.

<a id="member-4827de4b1263"></a>
### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Deterministically releases resources owned by this object.

Remarks: Disposal is idempotent. The winning caller synchronously sends Electron2D.ElectronObject.NotificationPreDelete, invokes Electron2D.ElectronObject.Dispose(System.Boolean), publishes the final state, clears base event subscribers, and suppresses finalization. Callers that lose the atomic transition return without repeating cleanup, although caller-specific Electron2D.ElectronObject.ValidateDisposal may already have run and may throw before that transition. The disposing thread may access guarded state during pre-delete and cleanup callbacks; every other thread is rejected after disposal starts.

System.AggregateException: Both notification delivery and derived cleanup fail.

System.Exception: Disposal validation, a pre-delete callback, derived cleanup, or a Electron2D.ElectronObject.Disposed handler fails. Validation failure leaves this caller from starting disposal; a Electron2D.ElectronObject.Disposed handler failure occurs after the final disposed state has been published.

<a id="member-a676d821b7d5"></a>
### Duplicate

`public Electron2D.StreamPeerBuffer Duplicate()`

Copies bytes into a new buffer with default byte order and cursor zero.

Returns: A caller-owned buffer.

<a id="member-7755fce3dd4b"></a>
### GetAvailableBytes

`public override System.Int32 GetAvailableBytes()`

Reports immediately readable bytes without consuming them.

Returns: A nonnegative count.

<a id="member-eda39cee9fc1"></a>
### GetPosition

`public System.Int32 GetPosition()`

Returns the cursor byte offset.

Returns: Byte offset.

<a id="member-67f07d759eaf"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-61ae8ee44013"></a>
### GetSize

`public System.Int32 GetSize()`

Returns the logical byte length.

Returns: Byte count.

<a id="member-61c753ad6b06"></a>
### ReadCore

`protected override System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)`

Reads a prefix for the concrete transport.

destination: The destination.

block: Whether waiting is permitted.

Returns: The received byte count.

<a id="member-bee701dadb72"></a>
### Resize

`public System.Void Resize(System.Int32 size)`

Changes logical length, zero-filling growth and clamping the cursor when shrinking.

size: Nonnegative length.

<a id="member-1fe6ab32eb4b"></a>
### Seek

`public System.Void Seek(System.Int32 position)`

Moves within the inclusive interval from zero to GetSize.

position: Byte offset.

System.ArgumentOutOfRangeException: The position lies outside the buffer.

<a id="member-7792d67e3fa6"></a>
### WriteCore

`protected override System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)`

Writes a prefix for the concrete transport.

data: The source.

block: Whether waiting is permitted.

Returns: The sent byte count.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/StreamPeerBuffer.md).
