# OggPacketSequence

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.OggPacketSequence` · **Namespace:** `Electron2D` · **Source:** [OggPacketSequence.cs](../../src/Scene/Resources/OggPacketSequence.cs).

**Inherits:** [Resource](Resource.md). No production subclass.

## Description

Copied jagged byte[][][] storage represents pages containing complete Vorbis packet byte arrays; signed long[] stores terminal granules. Every setter/getter copies all mutable layers. Authored incomplete/negative/zero-rate state is retained literally; the importer/consumer validates coherent headers, granules and finite PCM before execution. Packet/granule writes increment the version even for equal values and invalidate existing readers. SamplingRate changes independently and GetLength divides the final nonnegative granule by that literal float; absent/negative final granules return zero, positive granules at zero rate return infinity. Resource operations use a private gate. InstantiatePlayback supplies independent actual decoder cursor state without exposing native packet pointers.

## Example

Requires the indicated resource file when loading; this snippet mixes through the public CPU API.

```csharp
using var sequence = new OggPacketSequence
{
    GranulePositions = [48000],
    SamplingRate = 48000,
};
double seconds = sequence.GetLength(); // 1; packets must still be supplied to decode.
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public OggPacketSequence()` | Creates empty packet/granule arrays and zero sampling-rate metadata. |

## Constructors descriptions

### .ctor

`public OggPacketSequence()`

Summary: Creates empty packet/granule arrays and zero sampling-rate metadata.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Int64[] GranulePositions { get; set; }` | Gets or sets copied per-page signed terminal granules. |
| `public System.Byte[][][] PacketData { get; set; }` | Gets or sets copied pages of copied encoded packet bytes. |
| `public System.Single SamplingRate { get; set; }` | Gets or sets literal sampling-rate metadata. |

## Properties descriptions

### GranulePositions

`public System.Int64[] GranulePositions { get; set; }`

Summary: Gets or sets copied per-page signed terminal granules.

Value: Empty initially; setters retain literal positions and invalidate captured readers.

System.ArgumentNullException: The array is null.

System.ObjectDisposedException: The resource is disposed.


### PacketData

`public System.Byte[][][] PacketData { get; set; }`

Summary: Gets or sets copied pages of copied encoded packet bytes.

Value: Empty initially. Every array layer is independent of caller storage.

System.ArgumentNullException: An array layer is null.

System.ObjectDisposedException: The resource is disposed.


### SamplingRate

`public System.Single SamplingRate { get; set; }`

Summary: Gets or sets literal sampling-rate metadata.

Value: Zero initially; changing only this metadata does not invalidate packet readers.

System.ObjectDisposedException: The resource is disposed.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Implements the inherited Resource contract; see the description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Implements the inherited Resource contract; see the description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited Resource contract; see the description. |
| `public System.Double GetLength()` | Divides the nonnegative final granule by the stored sampling rate. |
| `public Electron2D.OggPacketSequencePlayback InstantiatePlayback()` | Creates independent caller-owned packet reading state. |

## Methods and protected extension points descriptions

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Concrete implementation of [Resource](Resource.md); lifecycle, effects and failures follow the description above.


### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Concrete implementation of [Resource](Resource.md); lifecycle, effects and failures follow the description above.


### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete implementation of [Resource](Resource.md); lifecycle, effects and failures follow the description above.


### GetLength

`public System.Double GetLength()`

Summary: Divides the nonnegative final granule by the stored sampling rate.

Returns: Zero for absent/negative final granules; otherwise literal floating-point division, including zero-rate infinity.

System.ObjectDisposedException: The resource is disposed.


### InstantiatePlayback

`public Electron2D.OggPacketSequencePlayback InstantiatePlayback()`

Summary: Creates independent caller-owned packet reading state.

Returns: A version-bound decoder cursor borrowing this sequence.

System.ObjectDisposedException: The sequence is disposed.

## Dependencies and verification

[ADR 0047](../decisions/audio.md#adr-0047), [Resource lifetime](../decisions/resources.md#adr-0014) and [audio component](../components/audio-playback.md) own the boundaries. [AudioCompressedTests](../../tests/Electron2D.Tests/AudioCompressedTests.cs) verifies copied state, independent PCM, seek/loops, lifetime, mutation, corruption, typed parameters, cache replacement and warmed active/idle CPU mixing. Actual FAudio PCM is a separate native test.

[Coverage](../coverage/classes/OggPacketSequence.md) records inherited dependencies and multichannel/platform limits; physical listening and native allocation totals are unverified.
