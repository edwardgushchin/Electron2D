# AudioStreamMP3

Last updated: 2026-10-02

**Declaration:** `public sealed class Electron2D.AudioStreamMP3` · **Namespace:** `Electron2D` · **Source:** [AudioStreamMP3.cs](../../src/Scene/Resources/AudioStreamMP3.cs).

**Inherits:** [AudioStream](AudioStream.md). No production subclass.

## Description

Copied MPEG Layer III bytes decode transactionally into immutable mono/stereo PCM. Existing playback retains its captured PCM after Data replacement; disposal invalidates all consumers. Resources are polyphonic. Tempo and beat/bar setters notify Changed after commit, even for equal assignments; a throwing listener leaves committed metadata. Loop and offset updates are silent and remain live for playback. Negative offsets clamp to zero; seeking at/beyond the captured duration restarts at zero. Active positions include 128-frame resampling prefetch. EOF loops count restarts; positive BPM and BeatCount enable beat boundaries with a 256-frame additive tail fade. Very short beat intervals clamp to one source frame; unplayable loop offsets reject instead of hanging. Nullable LoopingOverride follows the resource at null and forces true/false otherwise. Cold decoding prepares the entire finite PCM payload; import and replacement allocate, warmed mixing does not. MPEG Layer I/II, inconsistent rates/channels and declared-frame-count or gapless mismatches reject with FormatException. MP3 gapless LAME/Lavc/Lavf delay/padding includes decoder delay compensation.

## Example

Requires the indicated resource file when loading; this snippet mixes through the public CPU API.

```csharp
using var stream = AudioStreamMP3.LoadFromFile("res://audio/theme.mp3");
stream.Loop = true;
using var playback = stream.InstantiatePlayback();
playback.Start();
Vector2[] frames = playback.MixAudio(1, 256);
```

## Constructors

| Complete signature | Contract |
| --- | --- |
| `public AudioStreamMP3()` | Creates empty data, disabled looping, zero tempo/beat count and four beats per bar. |

## Constructors descriptions

### .ctor

`public AudioStreamMP3()`

Summary: Creates empty data, disabled looping, zero tempo/beat count and four beats per bar.


## Properties

| Complete signature | Contract |
| --- | --- |
| `public System.Double BPM { get; set; }` | Gets or sets BPM metadata. |
| `public System.Int32 BarBeats { get; set; }` | Gets or sets BarBeats metadata. |
| `public System.Int32 BeatCount { get; set; }` | Gets or sets BeatCount metadata. |
| `public System.Byte[] Data { get; set; }` | Gets or sets copied MPEG Layer III file bytes. |
| `public System.Boolean Loop { get; set; }` | Gets or sets Loop metadata. |
| `public System.Double LoopOffset { get; set; }` | Gets or sets LoopOffset metadata. |

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

Value: Nonnegative beats per bar; four initially.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### BeatCount

`public System.Int32 BeatCount { get; set; }`

Summary: Gets or sets BeatCount metadata.

Value: Nonnegative total musical beats.

System.ArgumentOutOfRangeException: The assigned numeric value is invalid.

System.ObjectDisposedException: The resource is disposed.


### Data

`public System.Byte[] Data { get; set; }`

Summary: Gets or sets copied MPEG Layer III file bytes.

Value: Empty initially; invalid replacement throws before changing resource state.

System.FormatException: The bytes are not supported finite mono/stereo MPEG audio.

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


## Methods and protected extension points

| Complete signature | Contract |
| --- | --- |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource, Electron2D.Resource> duplicateSubresource, System.Func<Electron2D.Resource, Electron2D.Resource> forceDuplicateSubresource)` | Implements the inherited AudioStream contract; see the description. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Implements the inherited AudioStream contract; see the description. |
| `public static Electron2D.AudioStreamMP3 LoadFromBuffer(System.ReadOnlySpan<System.Byte> streamData)` | Loads validated MPEG audio from borrowed encoded bytes. |
| `public static Electron2D.AudioStreamMP3 LoadFromFile(System.String path)` | Loads validated MPEG audio through engine file access. |
| `protected override System.Double OnGetBPM()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Int32 OnGetBarBeats()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Int32 OnGetBeatCount()` | Implements the inherited AudioStream contract; see the description. |
| `protected override System.Double OnGetLength()` | Implements the inherited AudioStream contract; see the description. |
| `protected override Electron2D.PropertyDescriptor[] OnGetParameterList()` | Implements the inherited AudioStream contract; see the description. |
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

`public static Electron2D.AudioStreamMP3 LoadFromBuffer(System.ReadOnlySpan<System.Byte> streamData)`

Summary: Loads validated MPEG audio from borrowed encoded bytes.

streamData: Borrowed MPEG file data.

Returns: A caller-owned independent audio resource.

System.FormatException: The encoded data is invalid.


### LoadFromFile

`public static Electron2D.AudioStreamMP3 LoadFromFile(System.String path)`

Summary: Loads validated MPEG audio through engine file access.

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

[Coverage](../coverage/classes/AudioStreamMP3.md) records inherited dependencies and multichannel/platform limits; physical listening and native allocation totals are unverified.
