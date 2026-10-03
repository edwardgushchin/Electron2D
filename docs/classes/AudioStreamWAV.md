# AudioStreamWAV

Last updated: 2026-10-01

**Declaration:** `public sealed class Electron2D.AudioStreamWAV` · **Source:** [AudioStreamWAV.cs](../../src/Scene/Resources/AudioStreamWAV.cs) · **Component:** [Audio playback](../components/audio-playback.md).

**Inherits:** [AudioStream](AudioStream.md).

## Description

Reusable copied encoded sample resource. Defaults: PCM8, 44100 Hz, mono, disabled loop, zero loop points, empty bytes and textual tags. Metadata setters retain signed nonzero sample rates and loop bounds; invalid execution rates/bounds fail when consumed. PCM8 stores signed bytes; PCM16 is little-endian signed; IMA stores low nibble first in channel-byte groups and ignores seek, with enabled loops forced forward; QOA uses the pinned managed codec. Decode prepares immutable float PCM outside warmed mixing, cached by resource version. Changes to previously prepared data decode on the setter thread, publishing a new snapshot; corrupt data caches a failure reported when consumed. Mono duplicates left into right. PCM playback normalizes by 32767; loops use the source inclusive batch boundary behavior, with forward/backward/ping-pong traversal. GetLoopCount retains zero for this concrete playback. Duration depends on literal data/metadata; malformed compressed data rejects on playback. WAV resources are polyphonic. Data and Tags getters/setters copy; Duplicate copies encoded state independently. Setters do not emit Resource.Changed, matching this resource contract. LoadFromBuffer/File accepts validated PCM/IEEE-float mono/stereo RIFF through SDL WAV decoding, then typed edits and optional IMA/QOA storage. INFO metadata and smpl loops are retained. SaveToWAV atomically writes ordinary PCM, appends .wav when needed, maps signed internal PCM8 to unsigned RIFF bytes and rejects compressed samples before touching an existing file. ResourceLoader generic audio registration and file scene serialization remain absent; static WAV load/save work directly.

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioStreamWAV()` | Creates empty mono eight-bit data at 44100 Hz with no loop. |
| `protected override System.Void CopyCustomStateTo(Electron2D.Resource target, System.Boolean deep, Electron2D.DeepDuplicateMode subresourceMode, System.Func<Electron2D.Resource?, Electron2D.Resource?> duplicateSubresource, System.Func<Electron2D.Resource?, Electron2D.Resource?> forceDuplicateSubresource)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override Electron2D.Resource CreateDuplicateInstance()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override System.Void Dispose(System.Boolean disposing)` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public static Electron2D.AudioStreamWAV LoadFromBuffer(System.ReadOnlySpan<System.Byte> streamData, Electron2D.AudioWAVImportOptions? options = null)` | Loads validated PCM/IEEE-float RIFF WAVE data with typed import options. A caller-owned audio resource. |
| streamData | Borrowed RIFF WAVE bytes. |
| options | Optional normalize/trim/rate/mono/loop/compression edits. |
| System.FormatException | Input chunks, codec, dimensions or samples are invalid. |
| System.ArgumentOutOfRangeException | Import options are invalid. |
| `public static Electron2D.AudioStreamWAV LoadFromFile(System.String path, Electron2D.AudioWAVImportOptions? options = null)` | Loads PCM/IEEE-float WAVE data through the engine file-access policy. A caller-owned stream. |
| path | Filesystem, resource or user path. |
| options | Optional typed import edits. |
| `protected override System.Double OnGetLength()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override System.Collections.Generic.Dictionary<System.String, System.String> OnGetTags()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override Electron2D.AudioStreamPlayback OnInstantiatePlayback()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `protected override System.Boolean OnIsMonophonic()` | Projects the inherited typed callback; see the concrete behavior above and base-class contract. |
| `public System.Void SaveToWAV(System.String path)` | Saves internal PCM as an ordinary RIFF WAVE file. |
| path | Engine-accessible output path; .wav is appended when absent. |
| System.NotSupportedException | Internal samples are compressed. |
| System.ObjectDisposedException | The stream is disposed. |
| `public System.Byte[] Data { get; set; }` | Gets or sets copied encoded audio bytes. Empty initially; setters/getters never share caller-owned array storage. |
| System.ArgumentNullException | The input is null. |
| System.ObjectDisposedException | The stream is disposed. |
| `public Electron2D.AudioLoopMode Loop { get; set; }` | Gets or sets Loop sample metadata. Disabled initially. Undefined modes reject. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public System.Int32 LoopBegin { get; set; }` | Gets or sets LoopBegin sample metadata. Zero initially; loop sample index. Validity is checked when consumed. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public System.Int32 LoopEnd { get; set; }` | Gets or sets LoopEnd sample metadata. Zero initially; loop boundary sample index. Validity is checked when consumed. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public System.Int32 MixRate { get; set; }` | Gets or sets MixRate sample metadata. 44100 initially. Zero rejects; other signed values retain their metadata identity. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public Electron2D.AudioStreamWAV.Format SampleFormat { get; set; }` | Gets or sets SampleFormat sample metadata. PCM8 initially. Undefined formats reject. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public System.Boolean Stereo { get; set; }` | Gets or sets Stereo sample metadata. False initially; true uses interleaved left/right data. |
| System.ArgumentOutOfRangeException | The assigned mode/rate is invalid. |
| System.ObjectDisposedException | The resource is disposed. |
| `public System.Collections.Generic.Dictionary<System.String, System.String> Tags { get; set; }` | Gets or sets copied textual RIFF metadata. An empty dictionary initially. Keys and values must be nonnull. |
| System.ArgumentNullException | The map or a key/value is null. |
| System.ObjectDisposedException | The stream is disposed. |

## Sampling overrides

| Full signature | Contract |
| --- | --- |
| `public override bool CanBeSampled()` | True for a live resource. |
| `public override AudioSample GenerateSample()` | Cold copied decoded mono/stereo PCM at its original rate. |

### CanBeSampled

Reports finite sample capability; corrupt encoded data can still fail when generated. Empty disabled-loop PCM is valid.

### GenerateSample

Returns caller-owned [AudioSample](AudioSample.md) from decoded PCM8/PCM16/IMA/QOA. Invalid rate/loop bounds or nonfinite PCM reject. WAV retains encoded resource metadata, including negative rates, but generation requires positive PCM rate. Native loop end is exclusive; streamed WAV retains its separate batch endpoint policy. Native sample rate/pitch limits apply during native preparation.

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.

[Own reference coverage](../coverage/classes/AudioStreamWAV.md) retains missing and Partial members separately.
