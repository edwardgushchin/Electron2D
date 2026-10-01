# AudioStreamOggVorbis

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioStreamOggVorbis` · **Namespace:** `Electron2D` · **Source:** [AudioStreamOggVorbis.cs](../../src/Scene/Resources/AudioStreamOggVorbis.cs).

**Inherits:** [AudioStream](AudioStream.md). No production subclass.

## Description

Borrowed OggPacketSequence publishes validated mono/stereo PCM and copied lowercase import comments. PacketSequence replacement validates before commit; null clears playable data. Previously created playback retains its captured sequence and PCM; mutation of that sequence invalidates it. New playback redecodes a changed sequence before capturing it. Tags setters/getters copy dictionaries; explicit authored keys remain literal. Musical metadata, EOF/beat loops, fades, overrides and resampling follow AudioStreamMP3. BarBeats requires at least two (MP3 accepts zero). LoadFromBuffer/File owns imported packet sequences and retains retired imported sequences until stream disposal so old playback stays valid across cache Replace. Directly assigned sequences are borrowed. Duplicate shares a sequence shallowly; DuplicateDeep(All) copies it independently through the standard Resource graph. Imported Ogg pages check headers, CRC, page order, continued packet state, beginning/end markers and final granules. The first Vorbis logical stream is selected; other streams are ignored. Pinned decoder multichannel submap PCM failed the native oracle, so more than two channels reject explicitly; this is a Partial coverage dependency, not an exclusion.

## Example

Requires the indicated resource file when loading; this snippet mixes through the public CPU API.

```csharp
using var stream = AudioStreamOggVorbis.LoadFromFile("res://audio/theme.ogg");
stream.Loop = true;
using var playback = stream.InstantiatePlayback();
playback.LoopingOverride = false;
playback.Start(.1);
Vector2[] frames = playback.MixAudio(1, 256);
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public AudioStreamOggVorbis()` | Creates empty data, disabled looping, zero tempo/beat count and four beats per bar. |

## Constructors descriptions

### .ctor

`public AudioStreamOggVorbis()`

Summary: Creates empty data, disabled looping, zero tempo/beat count and four beats per bar.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Double BPM { get; set; }` | Gets or sets BPM metadata. |
| `public System.Int32 BarBeats { get; set; }` | Gets or sets BarBeats metadata. |
| `public System.Int32 BeatCount { get; set; }` | Gets or sets BeatCount metadata. |
| `public System.Boolean Loop { get; set; }` | Gets or sets Loop metadata. |
| `public System.Double LoopOffset { get; set; }` | Gets or sets LoopOffset metadata. |
| `public Electron2D.OggPacketSequence? PacketSequence { get; set; }` | Gets or sets the borrowed encoded packet sequence. |
| `public System.Collections.Generic.Dictionary<System.String, System.String> Tags { get; set; }` | Gets or sets copied textual Vorbis comments. |

## Properties descriptions

### BPM

`public System.Double BPM { get; set; }`

Summary: Gets or sets BPM metadata.

Value: Nonnegative finite beats per minute.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### BarBeats

`public System.Int32 BarBeats { get; set; }`

Summary: Gets or sets BarBeats metadata.

Value: At least two beats per bar; four initially.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### BeatCount

`public System.Int32 BeatCount { get; set; }`

Summary: Gets or sets BeatCount metadata.

Value: Nonnegative total musical beats.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### Loop

`public System.Boolean Loop { get; set; }`

Summary: Gets or sets Loop metadata.

Value: False initially; enabled EOF or beat loops restart at LoopOffset.

System.ObjectDisposedException: The resource is disposed.


### LoopOffset

`public System.Double LoopOffset { get; set; }`

Summary: Gets or sets LoopOffset metadata.

Value: Finite seconds; zero initially.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### PacketSequence

`public Electron2D.OggPacketSequence PacketSequence { get; set; }`

Summary: Gets or sets the borrowed encoded packet sequence.

Value: Null initially. Non-null replacement validates/decodes headers and snapshots before committing.

System.FormatException: The packet sequence is not supported Vorbis data.

System.ObjectDisposedException: This resource or its sequence is disposed.


### Tags

`public System.Collections.Generic.Dictionary<System.String, System.String> Tags { get; set; }`

Summary: Gets or sets copied textual Vorbis comments.

Value: Empty initially. Imported names use lowercase keys; later authored names are retained literally.

System.ArgumentNullException: The dictionary or a key/value is null.

System.ObjectDisposedException: The resource is disposed.


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Implements the inherited AudioStream contract; see the description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited AudioStream contract; see the description. |
| `public static Electron2D.AudioStreamOggVorbis LoadFromBuffer(System.ReadOnlySpan<System.Byte> streamData)` | Loads validated Vorbis audio from borrowed encoded bytes. |
| `public static Electron2D.AudioStreamOggVorbis LoadFromFile(System.String path)` | Loads validated Vorbis audio through engine file access. |
| `protected override System.Double OnGetBPM()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Int32 OnGetBarBeats()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Int32 OnGetBeatCount()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Double OnGetLength()` | Implements the inherited AudioStream contract; see the description. |
| `protected override Electron2D.PropertyDescriptor[] OnGetParameterList()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Collections.Generic.Dictionary<System.String, System.String> OnGetTags()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Boolean OnHasLoop()` | Implements the inherited AudioStream contract; see the description. |
| `protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Boolean OnIsMonophonic()` | Implements the inherited AudioStream contract; see the description. |

## Methods and protected extension points descriptions

### CopyCustomStateTo

`protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### CreateDuplicateInstance

`protected override Electron2D.Resource CreateDuplicateInstance()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### Dispose

`protected override System.Void Dispose(System.Boolean disposing)`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### LoadFromBuffer

`public static Electron2D.AudioStreamOggVorbis LoadFromBuffer(System.ReadOnlySpan<System.Byte> streamData)`

Summary: Loads validated Vorbis audio from borrowed encoded bytes.

streamData: Borrowed Vorbis file data.

Returns: A caller-owned independent audio resource.

System.FormatException: The encoded data is invalid.


### LoadFromFile

`public static Electron2D.AudioStreamOggVorbis LoadFromFile(System.String path)`

Summary: Loads validated Vorbis audio through engine file access.

path: Filesystem, resource or user path.

Returns: A caller-owned independent audio resource.


### OnGetBPM

`protected override System.Double OnGetBPM()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnGetBarBeats

`protected override System.Int32 OnGetBarBeats()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnGetBeatCount

`protected override System.Int32 OnGetBeatCount()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnGetLength

`protected override System.Double OnGetLength()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnGetParameterList

`protected override Electron2D.PropertyDescriptor[] OnGetParameterList()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnGetTags

`protected override System.Collections.Generic.Dictionary<System.String, System.String> OnGetTags()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnHasLoop

`protected override System.Boolean OnHasLoop()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnInstantiatePlayback

`protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.


### OnIsMonophonic

`protected override System.Boolean OnIsMonophonic()`

Concrete implementation of [AudioStream](AudioStream.md); lifecycle, effects and failures follow the description above.

## Dependencies and verification

[ADR 0047](../decisions/audio.md#adr-0047), [Resource lifetime](../decisions/resources.md#adr-0014) and [audio component](../components/audio-playback.md) own the boundaries. [AudioCompressedTests](../../tests/Electron2D.Tests/AudioCompressedTests.cs) verifies copied state, independent PCM, seek/loops, lifetime, mutation, corruption, typed parameters, cache replacement and warmed active/idle CPU mixing. Actual FAudio PCM is a separate native test.

[Coverage](../coverage/classes/AudioStreamOggVorbis.md) records inherited dependencies and multichannel/platform limits; physical listening and native allocation totals are unverified.
