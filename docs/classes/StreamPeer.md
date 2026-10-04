# StreamPeer

Last updated: 2026-10-04

**Namespace:** `Electron2D`. **Declaration:** `public abstract class Electron2D.StreamPeer`.

**Inherits:** [ElectronObject](ElectronObject.md). **Inherited by:** [StreamPeerBuffer](StreamPeerBuffer.md), [StreamPeerSocket](StreamPeerSocket.md). **Source:** [StreamPeer.cs](../../src/Core/Networking/StreamPeer.cs).

## Description

Reads and writes ordered bytes, endian-aware numbers and length-prefixed strings.

Instances belong to their constructing thread. Full operations consume the requested byte count or throw; partial operations report progress without waiting. Returned arrays are caller-owned. Numeric and caller-span operations do not allocate managed buffers. Dynamic values have no wire representation here.

The [networking component](../components/networking.md) records ownership, native/private boundaries, typed failures, allocation scopes and remaining protocol prerequisites. [ADR 0094](../decisions/networking.md#adr-0094) defines the accepted contract.

## Example

Public API excerpt; identifiers supplied by the caller are context, and connection/readiness polling remains required where shown. NetworkingTests exercises complete public workflows.

```csharp
using var storage = new StreamPeerBuffer();
StreamPeer stream = storage;
stream.PutU32(42);
storage.Seek(0);
uint value = stream.GetU32();
```

## Constructor summary

| Complete C# signature | Contract |
| --- | --- |
| `protected StreamPeer()` | Creates an ordered byte stream owned by the current thread. |

## Constructor Descriptions

<a id="member-a25d0afed2c3"></a>
### .ctor

`protected StreamPeer()`

Creates an ordered byte stream owned by the current thread.

## Property summary

| Complete C# signature | Contract |
| --- | --- |
| `public System.Boolean BigEndian { get; set; }` | Gets or sets number and string-length byte order. |
| `public System.Int32 MaxStringBytes { get; set; }` | Gets or sets the allocation limit for incoming string bytes. |

## Property Descriptions

<a id="member-263ad213c99e"></a>
### BigEndian

`public System.Boolean BigEndian { get; set; }`

Gets or sets number and string-length byte order.

Value: False uses little endian initially.

<a id="member-a4c5576e89f4"></a>
### MaxStringBytes

`public System.Int32 MaxStringBytes { get; set; }`

Gets or sets the allocation limit for incoming string bytes.

Value: 16 MiB initially; nonnegative.

System.ArgumentOutOfRangeException: The limit is negative.

## Method summary

| Complete C# signature | Contract |
| --- | --- |
| `protected System.Void CheckStream()` | Checks lifetime and constructing-thread ownership. |
| `public System.Int16 Get16()` | Reads one 16-bit signed value in BigEndian order. |
| `public System.Int32 Get32()` | Reads one 32-bit signed value in BigEndian order. |
| `public System.Int64 Get64()` | Reads one 64-bit signed value in BigEndian order. |
| `public System.SByte Get8()` | Reads one 8-bit signed value in BigEndian order. |
| `public abstract System.Int32 GetAvailableBytes()` | Reports immediately readable bytes without consuming them. |
| `public System.Byte[] GetData(System.Int32 bytes)` | Reads exactly a requested number of bytes into a new array. |
| `public System.Void GetData(System.Span<System.Byte> destination)` | Reads exactly the requested bytes into a caller-owned span. |
| `public System.Double GetDouble()` | Reads one 64-bit IEEE value in BigEndian order. |
| `public System.Single GetFloat()` | Reads one 32-bit IEEE value in BigEndian order. |
| `public System.Single GetHalf()` | Reads one 16-bit IEEE value in BigEndian order. |
| `public System.Byte[] GetPartialData(System.Int32 bytes)` | Reads up to a requested number of immediately available bytes. |
| `public System.Int32 GetPartialData(System.Span<System.Byte> destination)` | Reads an available prefix without waiting. |
| `protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()` | Returns the typed properties exposed to tooling before validation. |
| `public System.String GetString(System.Int32 bytes = -1)` | Reads Latin-1 text, terminating at the first NUL. |
| `public System.UInt16 GetU16()` | Reads one 16-bit unsigned value in BigEndian order. |
| `public System.UInt32 GetU32()` | Reads one 32-bit unsigned value in BigEndian order. |
| `public System.UInt64 GetU64()` | Reads one 64-bit unsigned value in BigEndian order. |
| `public System.Byte GetU8()` | Reads one 8-bit unsigned value in BigEndian order. |
| `public System.String GetUTF8String(System.Int32 bytes = -1)` | Reads UTF-8 text, skipping a leading BOM, stopping at NUL and replacing malformed bytes. |
| `public System.Void Put16(System.Int16 value)` | Writes one 16-bit value in BigEndian order. |
| `public System.Void Put32(System.Int32 value)` | Writes one 32-bit value in BigEndian order. |
| `public System.Void Put64(System.Int64 value)` | Writes one 64-bit value in BigEndian order. |
| `public System.Void Put8(System.SByte value)` | Writes one 8-bit value in BigEndian order. |
| `public System.Void PutData(System.ReadOnlySpan<System.Byte> data)` | Writes every supplied byte, waiting for progress when required. |
| `public System.Void PutDouble(System.Double value)` | Writes one 64-bit value in BigEndian order. |
| `public System.Void PutFloat(System.Single value)` | Writes one 32-bit value in BigEndian order. |
| `public System.Void PutHalf(System.Single value)` | Writes one 16-bit value in BigEndian order. |
| `public System.Int32 PutPartialData(System.ReadOnlySpan<System.Byte> data)` | Writes an available prefix without waiting. |
| `public System.Void PutString(System.String value)` | Writes ASCII bytes prefixed by their unsigned 32-bit length. |
| `public System.Void PutU16(System.UInt16 value)` | Writes one 16-bit value in BigEndian order. |
| `public System.Void PutU32(System.UInt32 value)` | Writes one 32-bit value in BigEndian order. |
| `public System.Void PutU64(System.UInt64 value)` | Writes one 64-bit value in BigEndian order. |
| `public System.Void PutU8(System.Byte value)` | Writes one 8-bit value in BigEndian order. |
| `public System.Void PutUTF8String(System.String value)` | Writes UTF-8 bytes prefixed by their unsigned 32-bit length. |
| `protected abstract System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)` | Reads a prefix for the concrete transport. |
| `protected override System.Void ValidateDisposal()` | Validates caller-specific disposal preconditions before this caller attempts the disposal transition. |
| `protected abstract System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)` | Writes a prefix for the concrete transport. |

## Method Descriptions

<a id="member-dd8a867f1151"></a>
### CheckStream

`protected System.Void CheckStream()`

Checks lifetime and constructing-thread ownership.

System.ObjectDisposedException: The stream is disposed.

System.InvalidOperationException: The caller is not the stream owner.

<a id="member-6a2b0cceddc2"></a>
### Get16

`public System.Int16 Get16()`

Reads one 16-bit signed value in BigEndian order.

Returns: The decoded value.

<a id="member-8bbab82e8537"></a>
### Get32

`public System.Int32 Get32()`

Reads one 32-bit signed value in BigEndian order.

Returns: The decoded value.

<a id="member-266e876710b4"></a>
### Get64

`public System.Int64 Get64()`

Reads one 64-bit signed value in BigEndian order.

Returns: The decoded value.

<a id="member-dce4faffb8ef"></a>
### Get8

`public System.SByte Get8()`

Reads one 8-bit signed value in BigEndian order.

Returns: The decoded value.

<a id="member-9db693f962d0"></a>
### GetAvailableBytes

`public abstract System.Int32 GetAvailableBytes()`

Reports immediately readable bytes without consuming them.

Returns: A nonnegative count.

<a id="member-8ebc3834a268"></a>
### GetData

`public System.Byte[] GetData(System.Int32 bytes)`

Reads exactly a requested number of bytes into a new array.

bytes: Nonnegative byte count.

Returns: A caller-owned array.

<a id="member-37cd04d70beb"></a>
### GetData

`public System.Void GetData(System.Span<System.Byte> destination)`

Reads exactly the requested bytes into a caller-owned span.

destination: The destination.

System.IO.EndOfStreamException: The stream ends before filling it; a prefix may have been consumed.

<a id="member-60dfb3aff2ca"></a>
### GetDouble

`public System.Double GetDouble()`

Reads one 64-bit IEEE value in BigEndian order.

Returns: The decoded value.

<a id="member-d41dd88356fd"></a>
### GetFloat

`public System.Single GetFloat()`

Reads one 32-bit IEEE value in BigEndian order.

Returns: The decoded value.

<a id="member-b02e46966915"></a>
### GetHalf

`public System.Single GetHalf()`

Reads one 16-bit IEEE value in BigEndian order.

Returns: The decoded value.

<a id="member-f306fd48d752"></a>
### GetPartialData

`public System.Byte[] GetPartialData(System.Int32 bytes)`

Reads up to a requested number of immediately available bytes.

bytes: Nonnegative maximum.

Returns: A caller-owned array containing the received prefix.

<a id="member-5f64db81bac9"></a>
### GetPartialData

`public System.Int32 GetPartialData(System.Span<System.Byte> destination)`

Reads an available prefix without waiting.

destination: The caller-owned destination.

Returns: Bytes read; zero when no progress is possible.

<a id="member-e08cc0f7ad38"></a>
### GetPropertyDescriptors

`protected override System.Collections.Generic.IEnumerable<Electron2D.PropertyDescriptor> GetPropertyDescriptors()`

Returns the typed properties exposed to tooling before validation.

Returns: The descriptor sequence. The base sequence exposes identity, lifetime, and translation state.

Remarks: Overrides append or replace descriptors; they must not yield null entries.

<a id="member-d2ee90c7ba15"></a>
### GetString

`public System.String GetString(System.Int32 bytes = -1)`

Reads Latin-1 text, terminating at the first NUL.

bytes: Explicit byte count; negative reads a signed 32-bit length prefix.

Returns: The decoded string.

System.IO.InvalidDataException: The encoded length is negative or exceeds MaxStringBytes.

<a id="member-1be3fb3f638b"></a>
### GetU16

`public System.UInt16 GetU16()`

Reads one 16-bit unsigned value in BigEndian order.

Returns: The decoded value.

<a id="member-30bdda3274a4"></a>
### GetU32

`public System.UInt32 GetU32()`

Reads one 32-bit unsigned value in BigEndian order.

Returns: The decoded value.

<a id="member-db8cc8a5c42c"></a>
### GetU64

`public System.UInt64 GetU64()`

Reads one 64-bit unsigned value in BigEndian order.

Returns: The decoded value.

<a id="member-5264d6835bf4"></a>
### GetU8

`public System.Byte GetU8()`

Reads one 8-bit unsigned value in BigEndian order.

Returns: The decoded value.

<a id="member-e56aebf96576"></a>
### GetUTF8String

`public System.String GetUTF8String(System.Int32 bytes = -1)`

Reads UTF-8 text, skipping a leading BOM, stopping at NUL and replacing malformed bytes.

bytes: Explicit byte count; negative reads a signed 32-bit length prefix.

Returns: The decoded string.

System.IO.InvalidDataException: The encoded length is negative or exceeds MaxStringBytes.

<a id="member-761e5d508842"></a>
### Put16

`public System.Void Put16(System.Int16 value)`

Writes one 16-bit value in BigEndian order.

value: The value to encode.

<a id="member-1593c156114f"></a>
### Put32

`public System.Void Put32(System.Int32 value)`

Writes one 32-bit value in BigEndian order.

value: The value to encode.

<a id="member-d4cfd4620bb1"></a>
### Put64

`public System.Void Put64(System.Int64 value)`

Writes one 64-bit value in BigEndian order.

value: The value to encode.

<a id="member-90408cd9211f"></a>
### Put8

`public System.Void Put8(System.SByte value)`

Writes one 8-bit value in BigEndian order.

value: The value to encode.

<a id="member-66192186e5d2"></a>
### PutData

`public System.Void PutData(System.ReadOnlySpan<System.Byte> data)`

Writes every supplied byte, waiting for progress when required.

data: The bytes to send; never retained.

<a id="member-64d8482da7a3"></a>
### PutDouble

`public System.Void PutDouble(System.Double value)`

Writes one 64-bit value in BigEndian order.

value: The value to encode.

<a id="member-aebc0583a709"></a>
### PutFloat

`public System.Void PutFloat(System.Single value)`

Writes one 32-bit value in BigEndian order.

value: The value to encode.

<a id="member-6f59990b1d00"></a>
### PutHalf

`public System.Void PutHalf(System.Single value)`

Writes one 16-bit value in BigEndian order.

value: The value to encode.

<a id="member-92dd3666ebc5"></a>
### PutPartialData

`public System.Int32 PutPartialData(System.ReadOnlySpan<System.Byte> data)`

Writes an available prefix without waiting.

data: The bytes to send; never retained.

Returns: The byte count written.

<a id="member-03584186825b"></a>
### PutString

`public System.Void PutString(System.String value)`

Writes ASCII bytes prefixed by their unsigned 32-bit length.

value: Text; unrepresentable scalars become spaces.

<a id="member-c878aa2f252b"></a>
### PutU16

`public System.Void PutU16(System.UInt16 value)`

Writes one 16-bit value in BigEndian order.

value: The value to encode.

<a id="member-4490639e4733"></a>
### PutU32

`public System.Void PutU32(System.UInt32 value)`

Writes one 32-bit value in BigEndian order.

value: The value to encode.

<a id="member-4aa0172b79e0"></a>
### PutU64

`public System.Void PutU64(System.UInt64 value)`

Writes one 64-bit value in BigEndian order.

value: The value to encode.

<a id="member-333cc95196c2"></a>
### PutU8

`public System.Void PutU8(System.Byte value)`

Writes one 8-bit value in BigEndian order.

value: The value to encode.

<a id="member-ee555b9c8441"></a>
### PutUTF8String

`public System.Void PutUTF8String(System.String value)`

Writes UTF-8 bytes prefixed by their unsigned 32-bit length.

value: Text to encode.

<a id="member-e4abb73f4853"></a>
### ReadCore

`protected abstract System.Int32 ReadCore(System.Span<System.Byte> destination, System.Boolean block)`

Reads a prefix for the concrete transport.

destination: The destination.

block: Whether waiting is permitted.

Returns: The received byte count.

<a id="member-a97d2277de90"></a>
### ValidateDisposal

`protected override System.Void ValidateDisposal()`

Validates caller-specific disposal preconditions before this caller attempts the disposal transition.

Remarks: This method can run concurrently in multiple callers and can race with another caller starting disposal. Overrides must therefore be side-effect-free and tolerate repeated execution.

<a id="member-4459fe9f196a"></a>
### WriteCore

`protected abstract System.Int32 WriteCore(System.ReadOnlySpan<System.Byte> data, System.Boolean block)`

Writes a prefix for the concrete transport.

data: The source.

block: Whether waiting is permitted.

Returns: The sent byte count.

## Verification and limits

[NetworkingTests](../../tests/Electron2D.Tests/NetworkingTests.cs) verifies the exercised native Linux x64 IPv4/IPv6 loopback and UDS paths, binary/framing/queue edges, lifecycle, owner checks, scene request/reply and warmed allocation boundaries. Native allocator totals, other hosts/browser transport, real routed traffic, network throughput and owner acceptance remain unverified. Dynamic-value wire encoding is excluded under ADR 0001; raw typed bytes are executable. See [coverage](../coverage/classes/StreamPeer.md).
