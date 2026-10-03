# AudioServer.PlaybackType

Last updated: 2026-10-03

**Declaration:** `public enum Electron2D.AudioServer.PlaybackType` · **Source:** [AudioServer.cs](../../src/Servers/Audio/AudioServer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Typed numeric selector.

## Description

Default resolves ProjectSettings.AudioGeneralDefaultPlaybackType. Stream uses the managed block mixer; Sample uses complete native finite buffers for sample-capable resources and otherwise falls back to streaming. Max remains nonselectable. The project default has its separate two-value AudioDefaultPlaybackType domain.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioServer.PlaybackType Default = 0` | Use the configured default. Numeric value: 0. |
| `public const Electron2D.AudioServer.PlaybackType Max = 3` | Exclusive upper selector bound. Numeric value: 3. |
| `public const Electron2D.AudioServer.PlaybackType Sample = 2` | Use prepared native samples where available. Numeric value: 2. |
| `public const Electron2D.AudioServer.PlaybackType Stream = 1` | Use the stream mixing path. Numeric value: 1. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
