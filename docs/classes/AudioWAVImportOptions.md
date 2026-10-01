# AudioWAVImportOptions

Last updated: 2026-10-01

**Declaration:** `public sealed class Electron2D.AudioWAVImportOptions` · **Source:** [AudioWAVImportOptions.cs](../../src/Scene/Resources/AudioWAVImportOptions.cs) · **Component:** [Audio playback](../components/audio-playback.md).

`System.Object` (.NET).

## Description

Typed replacement for the WAV import options map. All boolean flags default false; MaxRate/loop points zero; Loop and Compression null. Null Loop detects RIFF metadata; Disabled discards it; other modes use explicit points, with negative points measured from the imported end. LimitRate requires positive MaxRate. Compression accepts only IMAADPCM/QOA; otherwise source-width PCM is retained or Force8Bit selects eight-bit. Rate reduction uses cubic interpolation, Normalize scales maximum absolute sample to one, Trim applies the source threshold/tail behavior only without detected looping, and ForceMono averages stereo. Edits allocate during import, outside the repeated audio path.

## API summary

| Full signature | Contract |
| --- | --- |
| `public AudioWAVImportOptions()` | Creates import edits with source-width PCM and detected loop metadata. |
| `public System.Nullable<Electron2D.AudioStreamWAV.Format> Compression { get; set; }` | Gets the internal compressed representation, or null for ordinary source-width PCM. Only IMAADPCM/QOA select compression. |
| `public System.Boolean Force8Bit { get; set; }` | Gets whether to force eight-bit internal PCM when compression is disabled. False by default. |
| `public System.Boolean ForceMono { get; set; }` | Gets whether to average stereo input into one channel. False by default. |
| `public System.Boolean LimitRate { get; set; }` | Gets whether to limit the imported sample rate. False by default. |
| `public System.Nullable<Electron2D.AudioStreamWAV.LoopMode> Loop { get; set; }` | Gets the import loop policy: null detects RIFF metadata; Disabled discards it. Null by default; detects source loop metadata. |
| `public System.Int32 LoopBegin { get; set; }` | Gets the explicit loop-begin frame; negative values count back from the imported end. Zero by default; interpreted only with an explicit enabled Loop. |
| `public System.Int32 LoopEnd { get; set; }` | Gets the explicit loop-end frame; negative values count back from the imported end. Zero by default; interpreted only with an explicit enabled Loop. |
| `public System.Int32 MaxRate { get; set; }` | Gets the requested maximum frequency in Hz. Zero by default; must be positive when LimitRate is true. |
| `public System.Boolean Normalize { get; set; }` | Gets whether to normalize the largest absolute sample to unity. False by default. |
| `public System.Boolean Trim { get; set; }` | Gets whether to trim silence and apply the source's 500-frame tail fade when not looping. False by default. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
