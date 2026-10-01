# AudioServer.SpeakerMode

Last updated: 2026-10-01

**Declaration:** `public enum Electron2D.AudioServer.SpeakerMode` · **Source:** [AudioServer.cs](../../src/Servers/Audio/AudioServer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Typed numeric selector.

## Description

Retains all numeric selector identities. Unsupported execution prerequisites are recorded on the owning class and in coverage; an enum value alone does not prove an output path.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioServer.SpeakerMode Stereo = 0` | Left and right output. Numeric value: 0. |
| `public const Electron2D.AudioServer.SpeakerMode Surround31 = 1` | Three full-range channels and low frequency. Numeric value: 1. |
| `public const Electron2D.AudioServer.SpeakerMode Surround51 = 2` | Five full-range channels and low frequency. Numeric value: 2. |
| `public const Electron2D.AudioServer.SpeakerMode Surround71 = 3` | Seven full-range channels and low frequency. Numeric value: 3. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
