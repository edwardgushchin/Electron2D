# AudioDefaultPlaybackType

Last updated: 2026-10-03

**Declaration:** `public enum Electron2D.AudioDefaultPlaybackType` · **Source:** [AudioDefaultPlaybackType.cs](../../src/Scene/Resources/AudioDefaultPlaybackType.cs).

## Description

Typed domain of `ProjectSettings.AudioGeneralDefaultPlaybackType` (`audio/general/default_playback_type`). The valid values are Stream=0 and Sample=1. It is distinct from AudioServer.PlaybackType, whose Default=0/Stream=1/Sample=2 selects or delegates transport per request; a recursive Default is not valid for the project default.

## API summary

| Member | Value | Meaning |
| --- | --- | --- |
| `Stream` | 0 | Use the existing stream mixer. Initial project default. |
| `Sample` | 1 | Use native samples for finite sample-capable streams; otherwise stream. |

## Enumeration Descriptions

### Stream

The default project setting resolves AudioServer.PlaybackType.Default to managed stream blocks.

### Sample

Default requests select complete native buffers for WAV/MP3/Vorbis. Microphone/generator/composite streams fall back to the stream path.

## Verification

[AudioSampleTests](../../tests/Electron2D.Tests/AudioSampleTests.cs) exercises default resolution and nonsampleable fallback. [ADR 0051](../decisions/product.md#adr-0051) preserves the distinct semantic value set.
