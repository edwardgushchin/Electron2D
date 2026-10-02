# AudioStreamGenerator

Last updated: 2026-10-02

**Namespace:** `Electron2D` · **Declaration:** `public sealed class AudioStreamGenerator : AudioStream` · **Source:** [AudioStreamGenerator.cs](../../src/Scene/Resources/AudioStreamGenerator.cs).

**Inherits:** [AudioStream](AudioStream.md) → [Resource](Resource.md).

## Description

Configuration for procedural stereo PCM. InstantiatePlayback returns a new caller-owned [AudioStreamGeneratorPlayback](AudioStreamGeneratorPlayback.md) with independent empty bounded storage; the playback borrows this resource. A player owns its returned handle, which remains borrowed by game code and becomes disposed on slot replacement/release. Length is unknown (`0`), playback is monophonic, and this is an ordinary producer rather than a meta stream. No musical/loop metadata or custom parameters are added.

Custom frequency, live output frequency and prepared recording-device frequency execute. Input mode uses AudioServer.GetInputMixRate and never substitutes Output. Mode selection/creation/start may prepare a paused native device on the audio owner; mixing only reads prepared frequency and never starts capture. Positive finite rate and duration are required. At creation, float rate times duration is truncated to source frames, storage uses the strictly larger power of two and reserves one slot. Exact powers therefore advance to the next power. Zero truncated frames yield one storage slot and zero usable frames. At most 2^24 storage frames (128 MiB stereo PCM) can be prepared; oversized requests reject before allocation.

Existing playback capacity remains fixed after BufferLength changes. MixRate/Mode changes are live sampling metadata. Generator setters commit under a resource lock, then synchronously emit Changed outside it; observer failure preserves the committed value. Resource duplication and scene-local copies preserve configuration, while active producer queues remain playback state outside Resource graphs. The source must remain live until its playback is no longer used.

## Example

Partial public scene snippet; attach the player to a live tree before Play. Reuse a prepared frame array when refilling from OnProcess or another producer thread.

```csharp
using var stream = new AudioStreamGenerator { MixRate = 22050, BufferLength = .1f };
var player = new AudioStreamPlayer { Stream = stream };
window.AddChild(player);
// In an attached scene callback:
player.Play();
var playback = (AudioStreamGeneratorPlayback)player.GetStreamPlayback();
Vector2[] frames = new Vector2[256];
for (int i = 0; i < frames.Length; i++)
    frames[i] = Vector2.One * (float)Math.Sin(Math.Tau * 440 * i / 22050);
playback.PushBuffer(frames);
```

[AudioGeneratorTests.RunHost](../../tests/Electron2D.Tests/AudioGeneratorTests.cs) compiles and exercises ongoing public generation with preserved phase and two sequential Engine.Run lifecycles on both current renderers.

## API summary

| Signature | Contract |
| --- | --- |
| `public AudioStreamGenerator()` | Custom 44100 Hz, requested 0.5-second buffer. |
| `public float MixRate { get; set; }` | Positive finite custom frequency in Hz. |
| `public float BufferLength { get; set; }` | Positive finite creation-time requested queue duration. |
| `public AudioStreamGeneratorMixRate MixRateMode { get; set; }` | Custom by default; Output and Input use the corresponding prepared device frequency. |
| `public enum AudioStreamGeneratorMixRate` | [Numeric selector](AudioStreamGenerator.AudioStreamGeneratorMixRate.md). |

## Member descriptions

### .ctor

Creates configuration only. No native audio/device ownership is claimed; empty queue/history storage is created by InstantiatePlayback.

### MixRate

44100 Hz initially. Finite positive values are accepted; invalid values throw ArgumentOutOfRangeException before commitment. Custom playback observes changes on subsequent source mixing. Output and Input ignore this property. Very large finite values can cause creation-size or resampling-cursor rejection; setting valid metadata does not resize existing storage.

### BufferLength

0.5 seconds initially. Controls each future queue's creation capacity using the current effective rate, truncation and strict power-of-two policy. Existing queues stay independent and fixed. Invalid values reject; excessive rate × duration fails during InstantiatePlayback. It is not a per-frame resizing request.

### MixRateMode

Custom (`2`) uses MixRate; Output (`0`) uses AudioServer.GetMixRate, including its documented 44100 Hz pre-native default. Input (`1`) prepares AudioServer.GetInputMixRate before committing selection; missing devices/permissions or off-owner preparation throw InvalidOperationException without changing the mode. Native input remains paused unless explicitly activated or used by a microphone. Max (`3`) and undefined values throw ArgumentOutOfRangeException. All reads/setters reject disposed resources.

### AudioStreamGeneratorMixRate

Nested enum values and the exact input dependency are documented on the linked selector page. Selector identities do not claim a working input backend.

## Protected overrides

| Signature | Contract |
| --- | --- |
| `protected override AudioStreamPlayback OnInstantiatePlayback()` | Validates/captures queue capacity and constructs an independent producer playback. |
| `protected override string OnGetStreamName()` | Returns `UserFeed`. |
| `protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()` | Inherited plus typed stored rate/duration/mode descriptors. |
| `protected override Resource CreateDuplicateInstance()` | New exact AudioStreamGenerator. |
| `protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)` | Copies a coherent configuration snapshot, with no queue data. |

### OnInstantiatePlayback

Captures effective rate and BufferLength under the resource lock, validates the bounded size and allocates independent queue storage. Throws ArgumentOutOfRangeException for an oversized or nonfinite product, ObjectDisposedException after resource disposal. It creates no native voice; the existing player path owns that later preparation.

### OnGetStreamName

Supplies the concrete producer diagnostic name, without changing stream metadata or state.

### GetPropertyDescriptors

Appends three typed stored descriptors to inherited Resource state. Configuration is available to normal typed scene/resource copying; runtime playback queues are not stored descriptors.

### CreateDuplicateInstance

Constructs an exact empty resource for inherited duplicate/local-scene operations.

### CopyCustomStateTo

Copies frequency, mode and duration from a locked source snapshot into the isolated target. Shallow/deep resource copies have identical producer configuration because this resource owns no child resources. Active queues/time/skip state do not enter the copy. Inherited Resource cleanup owns failure handling.

## Verification and limitations

[AudioGeneratorTests](../../tests/Electron2D.Tests/AudioGeneratorTests.cs) covers defaults, finite/cap/power-of-two boundaries, independent/local copies, invalid mode and post-commit notification failure. [AudioInputTests](../../tests/Electron2D.Tests/AudioInputTests.cs) also checks actual Input frequency, fixed creation capacity and live Custom-to-Input updates. Its playback/native/host cases verify actual procedural PCM and bounded lifecycle behavior. [Audio component](../components/audio-playback.md#procedural-generator) distinguishes CPU, FAudio and public host evidence.

[ADR 0047](../decisions/audio.md#adr-0047) owns the typed queue, validation, initial silence, continuous underrun and input-device frequency ownership. [Own coverage](../coverage/classes/AudioStreamGenerator.md) marks all own declarations implemented; inherited stream dependencies remain at their declaring layer. Physical listening, native allocation totals outside the FAudio instrumentation, other platforms and scene-file authoring remain unverified.
