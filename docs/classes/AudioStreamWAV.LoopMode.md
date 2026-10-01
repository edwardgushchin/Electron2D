# AudioStreamWAV.LoopMode

Last updated: 2026-10-01

**Declaration:** `public enum Electron2D.AudioStreamWAV.LoopMode` · **Source:** [AudioStreamWAV.cs](../../src/Scene/Resources/AudioStreamWAV.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Typed numeric selector.

## Description

Retains all numeric selector identities. Unsupported execution prerequisites are recorded on the owning class and in coverage; an enum value alone does not prove an output path.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamWAV.LoopMode Backward = 3` | Traverse the loop backward. Numeric value: 3. |
| `public const Electron2D.AudioStreamWAV.LoopMode Disabled = 0` | Play once. Numeric value: 0. |
| `public const Electron2D.AudioStreamWAV.LoopMode Forward = 1` | Wrap forward at the loop boundary. Numeric value: 1. |
| `public const Electron2D.AudioStreamWAV.LoopMode PingPong = 2` | Reflect direction at both loop boundaries. Numeric value: 2. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
