# AudioStreamPolyphonic

Last updated: 2026-10-03

**Declaration:** `public sealed class Electron2D.AudioStreamPolyphonic` · **Source:** [AudioStreamPolyphonic.cs](../../src/Scene/Resources/AudioStreamPolyphonic.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStream](AudioStream.md). No production subclass.

## Description

A monophonic meta resource producing independent [AudioStreamPlaybackPolyphonic](AudioStreamPlaybackPolyphonic.md) mixers. It contains capacity configuration, never transient child resources or native handles. Length, tempo, beats and loop metadata use the base zero/false defaults; IsMetaStream=true, IsMonophonic=true, CanBeSampled=false, parameter list empty. Scene players create one fresh mixer per Play; dynamic child calls select Stream/Sample/Default independently. Resource copies retain capacity and inherited Resource state.

## Example

Requires an attached player and a live child stream:

```csharp
using var resource = new AudioStreamPolyphonic { Polyphony = 32 };
player.Stream = resource;
player.Play();
var mixer = (AudioStreamPlaybackPolyphonic)player.GetStreamPlayback();
long id = mixer.PlayStream(sound, volumeDB: -6);
mixer.SetStreamPitchScale(id, 1.5f);
mixer.StopStream(id);
```

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioStreamPolyphonic()` | Thirty-two slots by default. |
| `public int Polyphony { get; set; }` | Zero through 128; captured by later playbacks. |
| `public override bool IsMetaStream()` | True for a live resource. |
| `protected override AudioStreamPlayback OnInstantiatePlayback()` | New independent mixer with captured capacity. |
| `protected override string OnGetStreamName()` | AudioStreamPolyphonic. |
| `protected override Resource CreateDuplicateInstance()` | Same concrete resource type. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies capacity; no runtime children enter copies. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Stored typed Polyphony plus inherited authoring. |
| `protected override void ValidateDisposal()` | Rejects child factory/mix reentry before disposal. |

## Constructor Descriptions

### AudioStreamPolyphonic

Creates a detached resource with Polyphony=32 and no transient child state.

## Property Descriptions

### Polyphony

Zero is supported, matching the runtime bound despite a typical authoring minimum of one. Existing mixers retain creation-time capacity. Invalid values throw ArgumentOutOfRangeException before mutation. Assignment does not emit Changed or PropertyListChanged; reads/assignments reject a disposed resource. Scalar publication is atomic.

## Method Descriptions

### IsMetaStream

Returns true; native Sample selection of this resource falls back to its stream mixer. Child PlayStream calls can still select native sample transport.

### OnInstantiatePlayback

Allocates a fixed voice array and a prepared 128-frame stereo scratch. Subsequent Polyphony edits do not resize it. Returned ownership belongs to caller or scene player.

### OnGetStreamName

Returns the concrete resource's descriptive name.

### CreateDuplicateInstance

Creates the same resource type for the standard Resource copy session.

### CopyCustomStateTo

Copies current capacity regardless of depth. Inherited Resource metadata uses the normal copy contract; no active IDs, child playbacks or output state are serialized.

### GetPropertyDescriptors

Exposes the typed stored capacity; scene packing borrows the resource through the existing player/emitter property.

### ValidateDisposal

A child factory or mix callback cannot dispose its source while the operation is in progress. Normal source disposal invalidates later playback consumption; owner Stop/Dispose still releases child state.

## Verification and limits

[AudioPolyphonicTests](../../tests/Electron2D.Tests/AudioPolyphonicTests.cs) verifies defaults, zero/128 bounds, captured capacity, typed Resource copies/PackedScene and runtime behavior. [Dynamic polyphony](../components/audio-playback.md#dynamic-polyphony) records native, allocation and host limits. [ADR 0047](../decisions/audio.md#adr-0047) owns transport; usage-tag/editor/file-authoring dependencies remain on declaring coverage pages.
