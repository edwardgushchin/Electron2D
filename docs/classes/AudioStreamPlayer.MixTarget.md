# AudioStreamPlayer.MixTarget

Last updated: 2026-10-01

**Declaration:** `public enum Electron2D.AudioStreamPlayer.MixTarget` · **Source:** [AudioStreamPlayer.cs](../../src/Scene/Audio/AudioStreamPlayer.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Typed numeric selector.

## Description

Retains all numeric selector identities. Unsupported execution prerequisites are recorded on the owning class and in coverage; an enum value alone does not prove an output path.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamPlayer.MixTarget Center = 2` | Route the left input to center and right input to low-frequency output when present. Numeric value: 2. |
| `public const Electron2D.AudioStreamPlayer.MixTarget Stereo = 0` | Route to left/right. Numeric value: 0. |
| `public const Electron2D.AudioStreamPlayer.MixTarget Surround = 1` | Route front, center/low-frequency and supported rear stereo pairs. Numeric value: 1. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
