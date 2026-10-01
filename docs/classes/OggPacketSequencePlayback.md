# OggPacketSequencePlayback

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.OggPacketSequencePlayback` · **Namespace:** `Electron2D` · **Source:** [OggPacketSequencePlayback.cs](../../src/Scene/Resources/OggPacketSequencePlayback.cs).

**Inherits:** [ElectronObject](ElectronObject.md). No production subclass.

## Description

Factory-created caller-owned decoder cursor. The packet sequence is borrowed; disposal clears captured arrays without disposing it. Independent cursor state advances across pages and exposes terminal granules/EOS only to the internal decoder. There are no public native packet-pointer methods; inherited ElectronObject lifetime remains applicable. Packet/granule mutation or source disposal invalidates use. This is the packet-provider object actually consumed during AudioStreamOggVorbis decoding.

## Example

Requires the indicated resource file when loading; this snippet mixes through the public CPU API.

```csharp
using var stream = AudioStreamOggVorbis.LoadFromFile("res://audio/theme.ogg");
using var packets = stream.PacketSequence!.InstantiatePlayback();
// Independent packet cursor lifetime; packet iteration belongs to the internal decoder.
```

## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited ElectronObject contract; see the description. |

## Methods and protected extension points descriptions

### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete implementation of [ElectronObject](ElectronObject.md); lifecycle, effects and failures follow the description above.

## Dependencies and verification

[ADR 0047](../decisions/audio.md#adr-0047), [Resource lifetime](../decisions/resources.md#adr-0014) and [audio component](../components/audio-playback.md) own the boundaries. [AudioCompressedTests](../../tests/Electron2D.Tests/AudioCompressedTests.cs) verifies copied state, independent PCM, seek/loops, lifetime, mutation, corruption, typed parameters, cache replacement and warmed active/idle CPU mixing. Actual FAudio PCM is a separate native test.

[Coverage](../coverage/classes/OggPacketSequencePlayback.md) records inherited dependencies and multichannel/platform limits; physical listening and native allocation totals are unverified.
