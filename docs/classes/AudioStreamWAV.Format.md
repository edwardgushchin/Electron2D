# AudioStreamWAV.Format

Last updated: 2026-10-01

**Declaration:** `public enum Electron2D.AudioStreamWAV.Format` · **Source:** [AudioStreamWAV.cs](../../src/Scene/Resources/AudioStreamWAV.cs) · **Component:** [Audio playback](../components/audio-playback.md).

Typed numeric selector.

## Description

Retains all numeric selector identities. Unsupported execution prerequisites are recorded on the owning class and in coverage; an enum value alone does not prove an output path.

## API summary

| Full signature | Contract |
| --- | --- |
| `public const Electron2D.AudioStreamWAV.Format IMAADPCM = 2` | Interleaved channel-byte IMA ADPCM, low nibble first. Numeric value: 2. |
| `public const Electron2D.AudioStreamWAV.Format PCM16 = 1` | Little-endian signed sixteen-bit PCM. Numeric value: 1. |
| `public const Electron2D.AudioStreamWAV.Format PCM8 = 0` | Signed eight-bit PCM. Numeric value: 0. |
| `public const Electron2D.AudioStreamWAV.Format QOA = 3` | Quite OK Audio file data. Numeric value: 3. |

## Verification and limits

[Audio verification](../components/audio-playback.md#verification) distinguishes CPU behavior, actual native mixed PCM, public host lifecycle, packaging and physical listening. [ADR 0047](../decisions/audio.md#adr-0047) owns the backend/decoder boundary. Inherited members are documented on their declaring class.
